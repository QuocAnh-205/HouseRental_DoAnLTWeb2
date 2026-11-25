namespace HouseRentalAPI.Models;

/// <summary>
/// Yêu cầu thuê nhà. Khi được chủ nhà duyệt sẽ trở thành hợp đồng thuê.
/// </summary>
public class Booking : BaseEntity
{
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public int TenantId { get; set; }
    public User Tenant { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int DurationMonths { get; set; }

    // Chốt giá tại thời điểm đặt, không bị ảnh hưởng khi chủ nhà đổi giá
    public decimal MonthlyPrice { get; set; }
    public decimal Deposit { get; set; }
    public decimal TotalAmount { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public string? Note { get; set; }
    public string? RejectReason { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
