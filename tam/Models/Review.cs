namespace HouseRentalAPI.Models;

public class Review : BaseEntity
{
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public int TenantId { get; set; }
    public User Tenant { get; set; } = null!;

    public int Rating { get; set; }
    public string? Comment { get; set; }

    // Phản hồi của chủ nhà
    public string? LandlordReply { get; set; }
    public DateTime? RepliedAt { get; set; }
}
