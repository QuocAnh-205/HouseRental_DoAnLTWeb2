using HouseRentalAPI.Data;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Notifications;
using HouseRentalAPI.Extensions;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Repositories;

public class NotificationRepository(AppDbContext context) : Repository<Notification>(context), INotificationRepository
{
    public Task<PagedResult<Notification>> GetByUserAsync(int userId, NotificationQueryDto query)
    {
        var notifications = DbSet.AsNoTracking().Where(n => n.UserId == userId);

        if (query.IsRead.HasValue)
            notifications = notifications.Where(n => n.IsRead == query.IsRead.Value);

        if (query.Type.HasValue)
            notifications = notifications.Where(n => n.Type == query.Type.Value);

        return notifications
            .OrderByDescending(n => n.CreatedAt)
            .ToPagedResultAsync(query.Page, query.PageSize);
    }

    public Task<int> CountUnreadAsync(int userId) =>
        DbSet.CountAsync(n => n.UserId == userId && !n.IsRead);

    public Task<int> MarkAllAsReadAsync(int userId)
    {
        var now = DateTime.UtcNow;
        return DbSet
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now));
    }
}
