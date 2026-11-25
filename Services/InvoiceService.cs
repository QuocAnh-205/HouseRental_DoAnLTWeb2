using HouseRentalAPI.Constants;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Invoices;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Mappings;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;

namespace HouseRentalAPI.Services;

/// <summary>
/// Chủ nhà lập hóa đơn hàng tháng cho hợp đồng đang hiệu lực, người thuê thanh toán qua
/// POST /api/bookings/{id}/payments với InvoiceId. Khi chủ nhà xác nhận, số đã trả được cộng dồn.
/// </summary>
public class InvoiceService(
    IInvoiceRepository invoiceRepository,
    IBookingRepository bookingRepository,
    INotificationService notificationService,
    ICurrentUserService currentUser) : IInvoiceService
{
    public async Task<InvoiceDto> CreateAsync(int bookingId, CreateInvoiceDto dto)
    {
        var booking = await bookingRepository.GetDetailAsync(bookingId)
            ?? throw new NotFoundException($"Không tìm thấy hợp đồng có id {bookingId}");

        if (!currentUser.IsAdmin && booking.Property.LandlordId != currentUser.UserId)
            throw new ForbiddenException("Bạn không phải chủ của căn nhà này");

        if (booking.Status != BookingStatus.Approved)
            throw new BadRequestException("Chỉ lập hóa đơn cho hợp đồng đang hiệu lực");

        var periodStart = new DateOnly(dto.Year, dto.Month, 1);
        var periodEnd = periodStart.AddMonths(1);
        if (periodEnd <= booking.StartDate || periodStart >= booking.EndDate)
            throw new BadRequestException("Kỳ hóa đơn nằm ngoài thời hạn hợp đồng");

        if (await invoiceRepository.ExistsAsync(bookingId, dto.Year, dto.Month))
            throw new ConflictException($"Đã có hóa đơn tháng {dto.Month}/{dto.Year} cho hợp đồng này");

        // Bỏ trống chỉ số cũ (0) thì lấy chỉ số mới của hóa đơn gần nhất
        var latest = await invoiceRepository.GetLatestAsync(bookingId);
        var electricityOld = dto.ElectricityOldIndex == 0 && latest is not null ? latest.ElectricityNewIndex : dto.ElectricityOldIndex;
        var waterOld = dto.WaterOldIndex == 0 && latest is not null ? latest.WaterNewIndex : dto.WaterOldIndex;

        if (dto.ElectricityNewIndex < electricityOld)
            throw new BadRequestException($"Chỉ số điện mới không được nhỏ hơn chỉ số cũ ({electricityOld})");

        if (dto.WaterNewIndex < waterOld)
            throw new BadRequestException($"Chỉ số nước mới không được nhỏ hơn chỉ số cũ ({waterOld})");

        var invoice = new Invoice
        {
            BookingId = booking.Id,
            Month = dto.Month,
            Year = dto.Year,
            RentAmount = booking.MonthlyPrice,
            ElectricityOldIndex = electricityOld,
            ElectricityNewIndex = dto.ElectricityNewIndex,
            ElectricityUnitPrice = dto.ElectricityUnitPrice,
            ElectricityAmount = (dto.ElectricityNewIndex - electricityOld) * dto.ElectricityUnitPrice,
            WaterOldIndex = waterOld,
            WaterNewIndex = dto.WaterNewIndex,
            WaterUnitPrice = dto.WaterUnitPrice,
            WaterAmount = (dto.WaterNewIndex - waterOld) * dto.WaterUnitPrice,
            ServiceFee = dto.ServiceFee,
            OtherFee = dto.OtherFee,
            DueDate = dto.DueDate ?? periodEnd.AddDays(9),
            Note = dto.Note?.Trim()
        };
        invoice.TotalAmount = invoice.RentAmount + invoice.ElectricityAmount + invoice.WaterAmount + invoice.ServiceFee + invoice.OtherFee;

        await invoiceRepository.AddAsync(invoice);
        await invoiceRepository.SaveChangesAsync();

        await notificationService.NotifyAsync(booking.TenantId, NotificationType.Invoice,
            $"Hóa đơn tháng {invoice.Month}/{invoice.Year}",
            $"Hóa đơn \"{booking.Property.Title}\" tháng {invoice.Month}/{invoice.Year}: {invoice.TotalAmount:N0}đ, hạn thanh toán {invoice.DueDate:dd/MM/yyyy}.",
            Link(invoice.Id));
        await invoiceRepository.SaveChangesAsync();

        invoice.Booking = booking;
        return invoice.ToDto();
    }

    public async Task<InvoiceDto> GetByIdAsync(int id)
    {
        var invoice = await FindAsync(id);
        EnsureCanView(invoice.Booking);
        return invoice.ToDto();
    }

    public async Task<IReadOnlyList<InvoiceDto>> GetByBookingAsync(int bookingId)
    {
        var booking = await bookingRepository.GetDetailAsync(bookingId)
            ?? throw new NotFoundException($"Không tìm thấy hợp đồng có id {bookingId}");
        EnsureCanView(booking);

        var invoices = await invoiceRepository.GetByBookingAsync(bookingId);
        return invoices.Select(i => i.ToDto()).ToList();
    }

    public async Task<PagedResult<InvoiceDto>> GetMineAsync(InvoiceQueryDto query)
    {
        var result = await invoiceRepository.GetByTenantAsync(currentUser.GetRequiredUserId(), query);
        return result.Map(i => i.ToDto());
    }

    public async Task<PagedResult<InvoiceDto>> GetReceivedAsync(InvoiceQueryDto query)
    {
        var result = await invoiceRepository.GetByLandlordAsync(currentUser.GetRequiredUserId(), query);
        return result.Map(i => i.ToDto());
    }

    public async Task<InvoiceDto> CancelAsync(int id)
    {
        var invoice = await FindAsync(id);

        if (!currentUser.IsAdmin && invoice.Booking.Property.LandlordId != currentUser.UserId)
            throw new ForbiddenException("Bạn không phải chủ của căn nhà này");

        if (invoice.Status == InvoiceStatus.Cancelled)
            throw new BadRequestException("Hóa đơn đã bị hủy trước đó");

        if (invoice.PaidAmount > 0)
            throw new BadRequestException("Hóa đơn đã có thanh toán, không thể hủy");

        invoice.Status = InvoiceStatus.Cancelled;

        await notificationService.NotifyAsync(invoice.Booking.TenantId, NotificationType.Invoice,
            $"Hóa đơn tháng {invoice.Month}/{invoice.Year} đã bị hủy",
            $"Chủ nhà đã hủy hóa đơn tháng {invoice.Month}/{invoice.Year} của \"{invoice.Booking.Property.Title}\".",
            Link(invoice.Id));
        await invoiceRepository.SaveChangesAsync();

        return invoice.ToDto();
    }

    /// <summary>Tính lại trạng thái hóa đơn theo số tiền đã trả và hạn thanh toán</summary>
    public static InvoiceStatus ResolveStatus(Invoice invoice)
    {
        if (invoice.Status == InvoiceStatus.Cancelled)
            return InvoiceStatus.Cancelled;

        if (invoice.PaidAmount >= invoice.TotalAmount)
            return InvoiceStatus.Paid;

        if (invoice.DueDate < AppTime.Today)
            return InvoiceStatus.Overdue;

        return invoice.PaidAmount > 0 ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Unpaid;
    }

    private static string Link(int id) => $"/api/invoices/{id}";

    private async Task<Invoice> FindAsync(int id) =>
        await invoiceRepository.GetDetailAsync(id)
        ?? throw new NotFoundException($"Không tìm thấy hóa đơn có id {id}");

    private void EnsureCanView(Booking booking)
    {
        var userId = currentUser.UserId;
        if (!currentUser.IsAdmin && booking.TenantId != userId && booking.Property.LandlordId != userId)
            throw new ForbiddenException("Bạn không có quyền xem hóa đơn của hợp đồng này");
    }
}
