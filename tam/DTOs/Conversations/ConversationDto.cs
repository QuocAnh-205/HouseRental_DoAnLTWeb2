using System.ComponentModel.DataAnnotations;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.DTOs.Conversations;

public record ParticipantDto(
    int Id,
    string FullName,
    string? AvatarUrl,
    UserRole Role);

public record ConversationDto(
    int Id,
    ParticipantDto OtherParticipant,
    int? PropertyId,
    string? PropertyTitle,
    string? LastMessage,
    DateTime LastMessageAt,
    int UnreadCount);

public record MessageDto(
    int Id,
    int ConversationId,
    int SenderId,
    string SenderName,
    string Content,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);

public class StartConversationDto
{
    [Range(1, int.MaxValue, ErrorMessage = "RecipientId không hợp lệ")]
    public int RecipientId { get; set; }

    /// <summary>Căn nhà đang trao đổi (không bắt buộc)</summary>
    public int? PropertyId { get; set; }

    [Required(ErrorMessage = "Nội dung tin nhắn là bắt buộc")]
    [StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}

public class SendMessageDto
{
    [Required(ErrorMessage = "Nội dung tin nhắn là bắt buộc")]
    [StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}
