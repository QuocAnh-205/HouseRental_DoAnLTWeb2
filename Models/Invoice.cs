namespace HouseRentalAPI.Models;

/// <summary>
/// Hóa đơn hàng tháng của hợp đồng: tiền thuê + điện + nước + dịch vụ.
/// </summary>
public class Invoice : BaseEntity
{
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public int Month { get; set; }
    public int Year { get; set; }

    public decimal RentAmount { get; set; }

    public int ElectricityOldIndex { get; set; }
    public int ElectricityNewIndex { get; set; }
    public decimal ElectricityUnitPrice { get; set; }
    public decimal ElectricityAmount { get; set; }

    public int WaterOldIndex { get; set; }
    public int WaterNewIndex { get; set; }
    public decimal WaterUnitPrice { get; set; }
    public decimal WaterAmount { get; set; }

    public decimal ServiceFee { get; set; }
    public decimal OtherFee { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public DateOnly DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    public string? Note { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
