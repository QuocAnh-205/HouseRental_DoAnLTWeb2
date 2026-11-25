using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Notifications;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<PagedResult<Notification>> GetByUserAsync(int userId, NotificationQueryDto query);
    Task<int> CountUnreadAsync(int userId);
    Task<int> MarkAllAsReadAsync(int userId);
}
