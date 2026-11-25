using System.ComponentModel.DataAnnotations;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.DTOs.Bookings;

public record BookingDto(
    int Id,
    int PropertyId,
    string PropertyTitle,
    string PropertyAddress,
    int LandlordId,
    int TenantId,
    string TenantName,
    string? TenantPhone,
    DateOnly StartDate,
    DateOnly EndDate,
    int DurationMonths,
    decimal MonthlyPrice,
    decimal Deposit,
    decimal TotalAmount,
    BookingStatus Status,
    string? Note,
    string? RejectReason,
    DateTime CreatedAt);

public class CreateBookingDto
{
    [Range(1, int.MaxValue, ErrorMessage = "PropertyId không hợp lệ")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "Ngày bắt đầu thuê là bắt buộc")]
    public DateOnly? StartDate { get; set; }

    [Range(1, 60, ErrorMessage = "Thời hạn thuê từ 1 đến 60 tháng")]
    public int DurationMonths { get; set; } = 6;

    [StringLength(1000)]
    public string? Note { get; set; }
}

public class RejectBookingDto
{
    [Required(ErrorMessage = "Lý do từ chối là bắt buộc")]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class BookingQueryDto : PaginationQuery
{
    /// <summary>Lọc theo trạng thái hợp đồng</summary>
    public BookingStatus? Status { get; set; }

    /// <summary>Tìm theo tiêu đề / địa chỉ nhà, tên / email / số điện thoại người thuê</summary>
    public string? Keyword { get; set; }

    /// <summary>newest (mặc định) | oldest | start_date | start_date_desc | price_asc | price_desc</summary>
    public string? SortBy { get; set; }
}
