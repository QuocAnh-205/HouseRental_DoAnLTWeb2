using System.ComponentModel.DataAnnotations;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.DTOs.Payments;

public record PaymentDto(
    int Id,
    int BookingId,
    int? InvoiceId,
    string? PropertyTitle,
    decimal Amount,
    PaymentType Type,
    PaymentMethod Method,
    PaymentStatus Status,
    string TransactionCode,
    string? Note,
    DateTime? PaidAt,
    DateTime CreatedAt);

public class CreatePaymentDto
{
    [Range(typeof(decimal), "1000", "100000000000", ErrorMessage = "Số tiền không hợp lệ")]
    public decimal Amount { get; set; }

    [EnumDataType(typeof(PaymentType))]
    public PaymentType Type { get; set; } = PaymentType.MonthlyRent;

    [EnumDataType(typeof(PaymentMethod))]
    public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;

    /// <summary>Thanh toán cho hóa đơn tháng nào (không bắt buộc)</summary>
    public int? InvoiceId { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}

public class UpdatePaymentStatusDto
{
    /// <summary>Success, Failed (khi đang Pending) hoặc Refunded (khi đã Success)</summary>
    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    [EnumDataType(typeof(PaymentStatus))]
    public PaymentStatus? Status { get; set; }
}

public class PaymentQueryDto : PaginationQuery
{
    public PaymentStatus? Status { get; set; }
    public PaymentType? Type { get; set; }
}
