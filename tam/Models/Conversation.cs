namespace HouseRentalAPI.Models;

/// <summary>Cuộc trò chuyện giữa hai người dùng, có thể gắn với một căn nhà</summary>
public class Conversation : BaseEntity
{
    public int InitiatorId { get; set; }
    public User Initiator { get; set; } = null!;

    public int RecipientId { get; set; }
    public User Recipient { get; set; } = null!;

    public int? PropertyId { get; set; }
    public Property? Property { get; set; }

    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
