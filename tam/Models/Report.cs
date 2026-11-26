namespace HouseRentalAPI.Models;

/// <summary>Báo cáo tin đăng vi phạm</summary>
public class Report : BaseEntity
{
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public int ReporterId { get; set; }
    public User Reporter { get; set; } = null!;

    public ReportReason Reason { get; set; }
    public string? Description { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public string? AdminNote { get; set; }
    public DateTime? HandledAt { get; set; }
}
