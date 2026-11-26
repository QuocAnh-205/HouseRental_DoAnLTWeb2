using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Conversations;

namespace HouseRentalAPI.Services;

public interface IConversationService
{
    /// <summary>Gửi tin nhắn đầu tiên; nếu đã có cuộc trò chuyện với người này về cùng căn nhà thì dùng lại</summary>
    Task<ConversationDto> StartAsync(StartConversationDto dto);

    /// <summary>
    /// Người thuê / chủ nhà nhắn tin cho bộ phận hỗ trợ (quản trị viên).
    /// Nếu đã từng trò chuyện với một admin thì tiếp tục cuộc trò chuyện đó.
    /// </summary>
    Task<ConversationDto> ContactSupportAsync(string content);

    Task<PagedResult<ConversationDto>> GetMineAsync(PaginationQuery query);

    /// <summary>Tin nhắn mới nhất trước; đồng thời đánh dấu đã đọc tin nhắn gửi đến</summary>
    Task<PagedResult<MessageDto>> GetMessagesAsync(int conversationId, PaginationQuery query);

    Task<MessageDto> SendAsync(int conversationId, string content);
    Task<int> GetUnreadCountAsync();
}
