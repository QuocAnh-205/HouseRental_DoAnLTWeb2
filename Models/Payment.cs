namespace HouseRentalAPI.Models;

public class Payment : BaseEntity
{
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    // Thanh toán cho một hóa đơn tháng (nếu có)
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public decimal Amount { get; set; }
    public PaymentType Type { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string TransactionCode { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime? PaidAt { get; set; }
}
