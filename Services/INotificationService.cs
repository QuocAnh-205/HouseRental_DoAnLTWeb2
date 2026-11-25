using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Notifications;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Services;

public interface INotificationService
{
    /// <summary>
    /// Thêm thông báo vào DbContext. Thông báo được lưu cùng lần SaveChanges kế tiếp
    /// của nghiệp vụ gọi nó, nên nghiệp vụ thất bại thì thông báo cũng không được tạo.
    /// </summary>
    Task NotifyAsync(int userId, NotificationType type, string title, string message, string? link = null);

    /// <summary>Gửi thông báo cho mọi admin đang hoạt động (cũng chưa lưu)</summary>
    Task NotifyAdminsAsync(NotificationType type, string title, string message, string? link = null);

    Task<PagedResult<NotificationDto>> GetMineAsync(NotificationQueryDto query);
    Task<int> GetUnreadCountAsync();
    Task<NotificationDto> MarkAsReadAsync(int id);
    Task<int> MarkAllAsReadAsync();
    Task DeleteAsync(int id);
}
