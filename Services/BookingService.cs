using HouseRentalAPI.Constants;
using HouseRentalAPI.DTOs.Bookings;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Mappings;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;

namespace HouseRentalAPI.Services;

/// <summary>
/// Luồng thuê nhà: Pending -> Approved -> Completed
///                 Pending -> Rejected | Cancelled
///                 Approved -> Cancelled (trước ngày bắt đầu)
/// </summary>
public class BookingService(
    IBookingRepository bookingRepository,
    IPropertyRepository propertyRepository,
    INotificationService notificationService,
    ICurrentUserService currentUser) : IBookingService
{
    public async Task<BookingDto> CreateAsync(CreateBookingDto dto)
    {
        var tenantId = currentUser.GetRequiredUserId();

        var property = await propertyRepository.GetByIdAsync(dto.PropertyId)
            ?? throw new NotFoundException($"Không tìm thấy nhà có id {dto.PropertyId}");

        if (property.Status != PropertyStatus.Available)
            throw new BadRequestException("Nhà này hiện không nhận yêu cầu thuê");

        if (property.LandlordId == tenantId)
            throw new BadRequestException("Bạn không thể thuê nhà của chính mình");

        var startDate = dto.StartDate!.Value;
        if (startDate < AppTime.Today)
            throw new BadRequestException("Ngày bắt đầu thuê không được ở quá khứ");

        var endDate = startDate.AddMonths(dto.DurationMonths);

        if (await bookingRepository.HasPendingRequestAsync(tenantId, property.Id))
            throw new ConflictException("Bạn đã gửi yêu cầu thuê nhà này và đang chờ chủ nhà duyệt");

        if (await bookingRepository.HasOverlapAsync(property.Id, startDate, endDate))
            throw new ConflictException("Nhà đã có người thuê trong khoảng thời gian này");

        var booking = new Booking
        {
            PropertyId = property.Id,
            TenantId = tenantId,
            StartDate = startDate,
            EndDate = endDate,
            DurationMonths = dto.DurationMonths,
            MonthlyPrice = property.PricePerMonth,
            Deposit = property.Deposit,
            TotalAmount = property.PricePerMonth * dto.DurationMonths,
            Note = dto.Note?.Trim(),
            Status = BookingStatus.Pending
        };

        await bookingRepository.AddAsync(booking);
        await bookingRepository.SaveChangesAsync();

        await notificationService.NotifyAsync(property.LandlordId, NotificationType.Booking,
            "Có yêu cầu thuê mới",
            $"Nhà \"{property.Title}\" nhận được yêu cầu thuê {dto.DurationMonths} tháng từ ngày {startDate:dd/MM/yyyy}.",
            BookingLink(booking.Id));
        await bookingRepository.SaveChangesAsync();

        return (await FindAsync(booking.Id)).ToDto();
    }

    public async Task<BookingDto> GetByIdAsync(int id)
    {
        var booking = await FindAsync(id);

        var userId = currentUser.UserId;
        if (!currentUser.IsAdmin && booking.TenantId != userId && booking.Property.LandlordId != userId)
            throw new ForbiddenException("Bạn không có quyền xem yêu cầu thuê này");

        return booking.ToDto();
    }

    public async Task<PagedResult<BookingDto>> GetMineAsync(BookingQueryDto query)
    {
        var result = await bookingRepository.GetByTenantAsync(currentUser.GetRequiredUserId(), query);
        return result.Map(b => b.ToDto());
    }

    public async Task<PagedResult<BookingDto>> GetReceivedAsync(BookingQueryDto query)
    {
        var result = await bookingRepository.GetByLandlordAsync(currentUser.GetRequiredUserId(), query);
        return result.Map(b => b.ToDto());
    }

    public async Task<PagedResult<BookingDto>> GetAllAsync(BookingQueryDto query)
    {
        var result = await bookingRepository.GetPagedAsync(query);
        return result.Map(b => b.ToDto());
    }

    public async Task<BookingDto> ApproveAsync(int id)
    {
        var booking = await FindForLandlordAsync(id);
        EnsureStatus(booking, BookingStatus.Pending, "Chỉ có thể duyệt yêu cầu đang chờ");

        // Tin đã bị ẩn (vd: Admin ẩn do báo cáo lừa đảo), đang chờ duyệt hoặc bị từ chối thì không được ký hợp đồng.
        // Nếu không chặn, chủ nhà Approve → nhà thành Rented → Complete → Available, vượt mặt lệnh ẩn của Admin.
        if (booking.Property.Status != PropertyStatus.Available)
            throw new BadRequestException(
                $"Tin đăng đang ở trạng thái {booking.Property.Status}, không thể duyệt hợp đồng. Hãy từ chối yêu cầu này.");

        if (booking.EndDate <= AppTime.Today)
            throw new BadRequestException("Thời gian thuê của yêu cầu này đã kết thúc, không thể duyệt. Hãy từ chối yêu cầu.");

        if (await bookingRepository.HasOverlapAsync(booking.PropertyId, booking.StartDate, booking.EndDate, booking.Id))
            throw new ConflictException("Nhà đã có hợp đồng khác trùng thời gian");

        booking.Status = BookingStatus.Approved;
        booking.Property.Status = PropertyStatus.Rented;

        // Tự động từ chối các yêu cầu đang chờ khác của cùng căn nhà
        var otherPending = await bookingRepository.GetPendingByPropertyAsync(booking.PropertyId, booking.Id);
        foreach (var other in otherPending)
        {
            other.Status = BookingStatus.Rejected;
            other.RejectReason = "Nhà đã được cho thuê cho khách khác";
            await notificationService.NotifyAsync(other.TenantId, NotificationType.Booking,
                "Yêu cầu thuê bị từ chối",
                $"Rất tiếc, nhà \"{booking.Property.Title}\" đã được cho thuê cho khách khác.",
                BookingLink(other.Id));
        }

        await notificationService.NotifyAsync(booking.TenantId, NotificationType.Booking,
            "Yêu cầu thuê đã được duyệt",
            $"Chủ nhà đã duyệt yêu cầu thuê \"{booking.Property.Title}\". Vui lòng thanh toán tiền cọc {booking.Deposit:N0}đ.",
            BookingLink(booking.Id));

        await bookingRepository.SaveChangesAsync();
        return booking.ToDto();
    }

    public async Task<BookingDto> RejectAsync(int id, string reason)
    {
        var booking = await FindForLandlordAsync(id);
        EnsureStatus(booking, BookingStatus.Pending, "Chỉ có thể từ chối yêu cầu đang chờ");

        booking.Status = BookingStatus.Rejected;
        booking.RejectReason = reason.Trim();

        await notificationService.NotifyAsync(booking.TenantId, NotificationType.Booking,
            "Yêu cầu thuê bị từ chối",
            $"Yêu cầu thuê \"{booking.Property.Title}\" bị từ chối. Lý do: {booking.RejectReason}",
            BookingLink(booking.Id));

        await bookingRepository.SaveChangesAsync();
        return booking.ToDto();
    }

    public async Task<BookingDto> CancelAsync(int id)
    {
        var booking = await FindAsync(id);

        if (!currentUser.IsAdmin && booking.TenantId != currentUser.UserId)
            throw new ForbiddenException("Bạn không có quyền hủy yêu cầu thuê này");

        var today = AppTime.Today;
        switch (booking.Status)
        {
            case BookingStatus.Pending:
                break;
            case BookingStatus.Approved when booking.StartDate > today:
                if (booking.Property.Status == PropertyStatus.Rented)
                    booking.Property.Status = PropertyStatus.Available;
                break;
            case BookingStatus.Approved:
                throw new BadRequestException("Hợp đồng đã bắt đầu, vui lòng liên hệ chủ nhà để kết thúc");
            default:
                throw new BadRequestException($"Không thể hủy yêu cầu ở trạng thái {booking.Status}");
        }

        booking.Status = BookingStatus.Cancelled;

        await notificationService.NotifyAsync(booking.Property.LandlordId, NotificationType.Booking,
            "Yêu cầu thuê đã bị hủy",
            $"Yêu cầu thuê \"{booking.Property.Title}\" của {booking.Tenant.FullName} đã bị hủy.",
            BookingLink(booking.Id));
        await bookingRepository.SaveChangesAsync();
        return booking.ToDto();
    }

    public async Task<BookingDto> CompleteAsync(int id)
    {
        var booking = await FindForLandlordAsync(id);
        EnsureStatus(booking, BookingStatus.Approved, "Chỉ có thể kết thúc hợp đồng đang hiệu lực");

        booking.Status = BookingStatus.Completed;
        if (booking.Property.Status == PropertyStatus.Rented)
            booking.Property.Status = PropertyStatus.Available;

        await notificationService.NotifyAsync(booking.TenantId, NotificationType.Booking,
            "Hợp đồng thuê đã kết thúc",
            $"Hợp đồng thuê \"{booking.Property.Title}\" đã kết thúc. Hãy để lại đánh giá giúp người thuê sau nhé!",
            BookingLink(booking.Id));
        await bookingRepository.SaveChangesAsync();
        return booking.ToDto();
    }

    private static string BookingLink(int id) => $"/api/bookings/{id}";

    private async Task<Booking> FindAsync(int id) =>
        await bookingRepository.GetDetailAsync(id)
        ?? throw new NotFoundException($"Không tìm thấy yêu cầu thuê có id {id}");

    private async Task<Booking> FindForLandlordAsync(int id)
    {
        var booking = await FindAsync(id);

        if (!currentUser.IsAdmin && booking.Property.LandlordId != currentUser.UserId)
            throw new ForbiddenException("Bạn không phải chủ của căn nhà này");

        return booking;
    }

    private static void EnsureStatus(Booking booking, BookingStatus expected, string message)
    {
        if (booking.Status != expected)
            throw new BadRequestException(message);
    }
}
