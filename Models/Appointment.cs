namespace HouseRentalAPI.Models;

/// <summary>Lịch hẹn xem nhà trước khi thuê</summary>
public class Appointment : BaseEntity
{
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public int TenantId { get; set; }
    public User Tenant { get; set; } = null!;

    public DateTime ScheduledAt { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Note { get; set; }

    /// <summary>Phản hồi của chủ nhà (lý do từ chối, hướng dẫn đường đi...)</summary>
    public string? LandlordNote { get; set; }
}
