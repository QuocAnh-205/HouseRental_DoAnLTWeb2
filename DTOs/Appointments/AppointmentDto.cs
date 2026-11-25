using System.ComponentModel.DataAnnotations;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.DTOs.Appointments;

public record AppointmentDto(
    int Id,
    int PropertyId,
    string PropertyTitle,
    string PropertyAddress,
    int LandlordId,
    int TenantId,
    string TenantName,
    string? TenantPhone,
    DateTime ScheduledAt,
    AppointmentStatus Status,
    string? Note,
    string? LandlordNote,
    DateTime CreatedAt);

public class CreateAppointmentDto
{
    [Range(1, int.MaxValue, ErrorMessage = "PropertyId không hợp lệ")]
    public int PropertyId { get; set; }

    /// <summary>Thời điểm hẹn xem nhà (UTC)</summary>
    [Required(ErrorMessage = "Thời gian hẹn là bắt buộc")]
    public DateTime? ScheduledAt { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}

public class AppointmentResponseDto
{
    /// <summary>Ghi chú của chủ nhà (lý do từ chối, hướng dẫn...)</summary>
    [StringLength(500)]
    public string? Note { get; set; }
}

public class AppointmentQueryDto : PaginationQuery
{
    public AppointmentStatus? Status { get; set; }
}
