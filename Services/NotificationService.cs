using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Notifications;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Mappings;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;

namespace HouseRentalAPI.Services;

public class NotificationService(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    ICurrentUserService currentUser) : INotificationService
{
    public Task NotifyAsync(int userId, NotificationType type, string title, string message, string? link = null) =>
        notificationRepository.AddAsync(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            Link = link
        });

    public async Task NotifyAdminsAsync(NotificationType type, string title, string message, string? link = null)
    {
        foreach (var adminId in await userRepository.GetActiveAdminIdsAsync())
            await NotifyAsync(adminId, type, title, message, link);
    }

    public async Task<PagedResult<NotificationDto>> GetMineAsync(NotificationQueryDto query)
    {
        var result = await notificationRepository.GetByUserAsync(currentUser.GetRequiredUserId(), query);
        return result.Map(n => n.ToDto());
    }

    public Task<int> GetUnreadCountAsync() =>
        notificationRepository.CountUnreadAsync(currentUser.GetRequiredUserId());

    public async Task<NotificationDto> MarkAsReadAsync(int id)
    {
        var notification = await FindMineAsync(id);

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await notificationRepository.SaveChangesAsync();
        }

        return notification.ToDto();
    }

    public Task<int> MarkAllAsReadAsync() =>
        notificationRepository.MarkAllAsReadAsync(currentUser.GetRequiredUserId());

    public async Task DeleteAsync(int id)
    {
        var notification = await FindMineAsync(id);
        notificationRepository.Remove(notification);
        await notificationRepository.SaveChangesAsync();
    }

    private async Task<Notification> FindMineAsync(int id)
    {
        var notification = await notificationRepository.GetByIdAsync(id);

        // Không tiết lộ thông báo của người khác có tồn tại hay không
        if (notification is null || notification.UserId != currentUser.GetRequiredUserId())
            throw new NotFoundException($"Không tìm thấy thông báo có id {id}");

        return notification;
    }
}
