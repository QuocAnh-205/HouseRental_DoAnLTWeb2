using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Payments;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Mappings;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;

namespace HouseRentalAPI.Services;

/// <summary>
/// Người thuê tạo phiếu thanh toán (Pending), chủ nhà xác nhận đã nhận tiền (Success) hoặc không (Failed).
/// Thanh toán đã Success có thể hoàn tiền (Refunded), ví dụ trả lại tiền cọc.
/// Có thể thay bằng callback từ cổng thanh toán (VnPay, Momo...) khi tích hợp thực tế.
/// </summary>
public class PaymentService(
    IPaymentRepository paymentRepository,
    IBookingRepository bookingRepository,
    IInvoiceRepository invoiceRepository,
    INotificationService notificationService,
    ICurrentUserService currentUser) : IPaymentService
{
    public async Task<PaymentDto> CreateAsync(int bookingId, CreatePaymentDto dto)
    {
        var booking = await FindBookingAsync(bookingId);

        if (booking.TenantId != currentUser.GetRequiredUserId())
            throw new ForbiddenException("Bạn chỉ có thể thanh toán cho hợp đồng của mình");

        if (booking.Status != BookingStatus.Approved)
            throw new BadRequestException("Chỉ có thể thanh toán cho hợp đồng đã được duyệt");

        if (dto.InvoiceId.HasValue)
        {
            var invoice = await invoiceRepository.GetByIdAsync(dto.InvoiceId.Value);
            if (invoice is null || invoice.BookingId != booking.Id)
                throw new NotFoundException($"Không tìm thấy hóa đơn có id {dto.InvoiceId} trong hợp đồng này");

            if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled)
                throw new BadRequestException("Hóa đơn đã thanh toán đủ hoặc đã bị hủy");

            if (dto.Amount > invoice.TotalAmount - invoice.PaidAmount)
                throw new BadRequestException($"Số tiền vượt quá số còn phải trả ({invoice.TotalAmount - invoice.PaidAmount:N0}đ)");
        }
        else if (dto.Type == PaymentType.Deposit)
        {
            // Mỗi hợp đồng chỉ có MỘT phiếu cọc đang chờ hoặc đã thành công → tránh khách bấm gửi 2 lần
            if (await paymentRepository.AnyAsync(p => p.BookingId == booking.Id && p.Type == PaymentType.Deposit && p.Status == PaymentStatus.Success))
                throw new ConflictException("Hợp đồng này đã thanh toán tiền cọc");

            if (await paymentRepository.AnyAsync(p => p.BookingId == booking.Id && p.Type == PaymentType.Deposit && p.Status == PaymentStatus.Pending))
                throw new ConflictException("Bạn đã gửi phiếu tiền cọc và đang chờ chủ nhà xác nhận");

            if (dto.Amount > booking.Deposit)
                throw new BadRequestException($"Số tiền cọc vượt quá mức cọc của hợp đồng ({booking.Deposit:N0}đ)");
        }

        var payment = new Payment
        {
            BookingId = booking.Id,
            InvoiceId = dto.InvoiceId,
            Amount = dto.Amount,
            Type = dto.InvoiceId.HasValue ? PaymentType.MonthlyRent : dto.Type,
            Method = dto.Method,
            Note = dto.Note?.Trim(),
            Status = PaymentStatus.Pending,
            TransactionCode = GenerateTransactionCode()
        };

        await paymentRepository.AddAsync(payment);
        await notificationService.NotifyAsync(booking.Property.LandlordId, NotificationType.Payment,
            "Có thanh toán chờ xác nhận",
            $"{booking.Tenant.FullName} vừa thanh toán {payment.Amount:N0}đ cho \"{booking.Property.Title}\" (mã {payment.TransactionCode}).",
            $"/api/bookings/{booking.Id}/payments");
        await paymentRepository.SaveChangesAsync();

        payment.Booking = booking;
        return payment.ToDto();
    }

    public async Task<IReadOnlyList<PaymentDto>> GetByBookingAsync(int bookingId)
    {
        var booking = await FindBookingAsync(bookingId);

        var userId = currentUser.UserId;
        if (!currentUser.IsAdmin && booking.TenantId != userId && booking.Property.LandlordId != userId)
            throw new ForbiddenException("Bạn không có quyền xem thanh toán của hợp đồng này");

        var payments = await paymentRepository.GetByBookingAsync(bookingId);
        return payments.Select(p => p.ToDto()).ToList();
    }

    public async Task<PagedResult<PaymentDto>> GetMineAsync(PaymentQueryDto query)
    {
        var result = await paymentRepository.GetByTenantAsync(currentUser.GetRequiredUserId(), query);
        return result.Map(p => p.ToDto());
    }

    public async Task<PagedResult<PaymentDto>> GetReceivedAsync(PaymentQueryDto query)
    {
        var result = await paymentRepository.GetByLandlordAsync(currentUser.GetRequiredUserId(), query);
        return result.Map(p => p.ToDto());
    }

    public async Task<PaymentDto> UpdateStatusAsync(int id, PaymentStatus status)
    {
        var payment = await paymentRepository.GetDetailAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy thanh toán có id {id}");

        if (!currentUser.IsAdmin && payment.Booking.Property.LandlordId != currentUser.UserId)
            throw new ForbiddenException("Chỉ chủ nhà mới được xác nhận thanh toán");

        var allowed = (payment.Status, status) is
            (PaymentStatus.Pending, PaymentStatus.Success) or
            (PaymentStatus.Pending, PaymentStatus.Failed) or
            (PaymentStatus.Success, PaymentStatus.Refunded);

        if (!allowed)
            throw new BadRequestException($"Không thể chuyển thanh toán từ {payment.Status} sang {status}");

        // Nhiều phiếu Pending cho cùng hóa đơn: không cho xác nhận vượt quá số còn phải trả
        if (status == PaymentStatus.Success && payment.Invoice is { } target &&
            payment.Amount > target.TotalAmount - target.PaidAmount)
            throw new BadRequestException(
                $"Hóa đơn chỉ còn {target.TotalAmount - target.PaidAmount:N0}đ chưa thanh toán, không thể xác nhận khoản {payment.Amount:N0}đ. Hãy đánh dấu Failed.");

        // Không cho xác nhận 2 phiếu cọc thành công cho cùng một hợp đồng (dữ liệu cũ / gửi đồng thời)
        if (status == PaymentStatus.Success && payment.Type == PaymentType.Deposit &&
            await paymentRepository.AnyAsync(p => p.BookingId == payment.BookingId && p.Id != payment.Id &&
                                                  p.Type == PaymentType.Deposit && p.Status == PaymentStatus.Success))
            throw new ConflictException("Hợp đồng này đã có phiếu cọc được xác nhận. Hãy đánh dấu phiếu này là Failed.");

        payment.Status = status;
        if (status == PaymentStatus.Success)
            payment.PaidAt = DateTime.UtcNow;

        if (payment.Invoice is { } invoice && status is PaymentStatus.Success or PaymentStatus.Refunded)
        {
            invoice.PaidAmount += status == PaymentStatus.Success ? payment.Amount : -payment.Amount;
            invoice.Status = InvoiceService.ResolveStatus(invoice);
        }

        var (title, message) = status switch
        {
            PaymentStatus.Success => ("Thanh toán đã được xác nhận", $"Chủ nhà đã xác nhận nhận {payment.Amount:N0}đ (mã {payment.TransactionCode})."),
            PaymentStatus.Failed => ("Thanh toán không thành công", $"Chủ nhà chưa nhận được khoản {payment.Amount:N0}đ (mã {payment.TransactionCode}). Vui lòng kiểm tra lại."),
            _ => ("Đã hoàn tiền", $"Khoản {payment.Amount:N0}đ (mã {payment.TransactionCode}) đã được hoàn lại.")
        };
        await notificationService.NotifyAsync(payment.Booking.TenantId, NotificationType.Payment, title, message,
            $"/api/bookings/{payment.BookingId}/payments");

        await paymentRepository.SaveChangesAsync();
        return payment.ToDto();
    }

    private async Task<Booking> FindBookingAsync(int bookingId) =>
        await bookingRepository.GetDetailAsync(bookingId)
        ?? throw new NotFoundException($"Không tìm thấy hợp đồng có id {bookingId}");

    private static string GenerateTransactionCode() =>
        $"PAY{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
}
