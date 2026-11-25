using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.DTOs.Notifications;

public record NotificationDto(
    int Id,
    NotificationType Type,
    string Title,
    string Message,
    string? Link,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);

public class NotificationQueryDto : PaginationQuery
{
    public bool? IsRead { get; set; }
    public NotificationType? Type { get; set; }
}

public record UnreadCountDto(int UnreadCount);
