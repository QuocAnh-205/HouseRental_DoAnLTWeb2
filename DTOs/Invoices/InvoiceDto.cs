using System.ComponentModel.DataAnnotations;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.DTOs.Invoices;

public record InvoiceDto(
    int Id,
    int BookingId,
    int PropertyId,
    string PropertyTitle,
    int TenantId,
    string TenantName,
    int Month,
    int Year,
    decimal RentAmount,
    int ElectricityOldIndex,
    int ElectricityNewIndex,
    decimal ElectricityUnitPrice,
    decimal ElectricityAmount,
    int WaterOldIndex,
    int WaterNewIndex,
    decimal WaterUnitPrice,
    decimal WaterAmount,
    decimal ServiceFee,
    decimal OtherFee,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    DateOnly DueDate,
    InvoiceStatus Status,
    string? Note,
    DateTime CreatedAt);

public class CreateInvoiceDto
{
    [Range(1, 12, ErrorMessage = "Tháng từ 1 đến 12")]
    public int Month { get; set; }

    [Range(2000, 2100, ErrorMessage = "Năm không hợp lệ")]
    public int Year { get; set; }

    [Range(0, int.MaxValue)]
    public int ElectricityOldIndex { get; set; }

    [Range(0, int.MaxValue)]
    public int ElectricityNewIndex { get; set; }

    /// <summary>Giá điện mỗi kWh</summary>
    [Range(typeof(decimal), "0", "1000000")]
    public decimal ElectricityUnitPrice { get; set; } = 3_500;

    [Range(0, int.MaxValue)]
    public int WaterOldIndex { get; set; }

    [Range(0, int.MaxValue)]
    public int WaterNewIndex { get; set; }

    /// <summary>Giá nước mỗi m³</summary>
    [Range(typeof(decimal), "0", "1000000")]
    public decimal WaterUnitPrice { get; set; } = 15_000;

    /// <summary>Phí dịch vụ (wifi, rác, gửi xe...)</summary>
    [Range(typeof(decimal), "0", "1000000000")]
    public decimal ServiceFee { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal OtherFee { get; set; }

    /// <summary>Hạn thanh toán, mặc định ngày 10 của tháng kế tiếp</summary>
    public DateOnly? DueDate { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}

public class InvoiceQueryDto : PaginationQuery
{
    public InvoiceStatus? Status { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
}
