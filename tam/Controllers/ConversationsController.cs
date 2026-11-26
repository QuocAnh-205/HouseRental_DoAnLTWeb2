using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Conversations;
using HouseRentalAPI.DTOs.Notifications;
using HouseRentalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HouseRentalAPI.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
[Produces("application/json")]
public class ConversationsController(IConversationService conversationService) : ControllerBase
{
    /// <summary>Bắt đầu trò chuyện (ví dụ người thuê nhắn chủ nhà về một căn nhà)</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> Start(StartConversationDto dto)
    {
        var result = await conversationService.StartAsync(dto);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(result, "Đã gửi tin nhắn"));
    }

    /// <summary>Nhắn tin cho bộ phận hỗ trợ (quản trị viên) — dành cho người thuê và chủ nhà</summary>
    [HttpPost("support")]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> ContactSupport(SendMessageDto dto)
    {
        var result = await conversationService.ContactSupportAsync(dto.Content);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(result, "Đã gửi tin nhắn tới bộ phận hỗ trợ"));
    }

    /// <summary>Danh sách cuộc trò chuyện của tôi, mới nhất trước</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ConversationDto>>>> GetMine([FromQuery] PaginationQuery query)
    {
        var result = await conversationService.GetMineAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>Tổng số tin nhắn chưa đọc</summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<UnreadCountDto>>> GetUnreadCount()
    {
        var count = await conversationService.GetUnreadCountAsync();
        return ApiResponse.Ok(new UnreadCountDto(count));
    }

    /// <summary>Tin nhắn trong cuộc trò chuyện (mới nhất trước), tự đánh dấu đã đọc</summary>
    [HttpGet("{id:int}/messages")]
    public async Task<ActionResult<ApiResponse<PagedResult<MessageDto>>>> GetMessages(int id, [FromQuery] PaginationQuery query)
    {
        var result = await conversationService.GetMessagesAsync(id, query);
        return ApiResponse.Ok(result);
    }

    /// <summary>Gửi tin nhắn</summary>
    [HttpPost("{id:int}/messages")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> Send(int id, SendMessageDto dto)
    {
        var result = await conversationService.SendAsync(id, dto.Content);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(result));
    }
}
