using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Notifications;
using HouseRentalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HouseRentalAPI.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    /// <summary>Thông báo của tôi (lọc theo đã đọc / loại)</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> GetMine([FromQuery] NotificationQueryDto query)
    {
        var result = await notificationService.GetMineAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>Số thông báo chưa đọc</summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<UnreadCountDto>>> GetUnreadCount()
    {
        var count = await notificationService.GetUnreadCountAsync();
        return ApiResponse.Ok(new UnreadCountDto(count));
    }

    /// <summary>Đánh dấu một thông báo đã đọc</summary>
    [HttpPatch("{id:int}/read")]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> MarkAsRead(int id)
    {
        var result = await notificationService.MarkAsReadAsync(id);
        return ApiResponse.Ok(result);
    }

    /// <summary>Đánh dấu tất cả đã đọc</summary>
    [HttpPatch("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllAsRead()
    {
        var count = await notificationService.MarkAllAsReadAsync();
        return ApiResponse.Ok($"Đã đánh dấu {count} thông báo là đã đọc");
    }

    /// <summary>Xóa thông báo</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await notificationService.DeleteAsync(id);
        return ApiResponse.Ok("Đã xóa thông báo");
    }
}
