using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Repositories;

public record ConversationSummary(Conversation Conversation, Message? LastMessage, int UnreadCount);

public interface IConversationRepository : IRepository<Conversation>
{
    /// <summary>Lấy cuộc trò chuyện kèm hai người tham gia và căn nhà (có tracking)</summary>
    Task<Conversation?> GetDetailAsync(int id);

    /// <summary>Tìm cuộc trò chuyện giữa hai người về cùng một căn nhà (không phân biệt ai bắt đầu)</summary>
    Task<Conversation?> FindBetweenAsync(int userId1, int userId2, int? propertyId);

    Task<PagedResult<ConversationSummary>> GetByUserAsync(int userId, PaginationQuery query);

    /// <summary>Tin nhắn mới nhất trước, kèm người gửi</summary>
    Task<PagedResult<Message>> GetMessagesAsync(int conversationId, PaginationQuery query);

    Task AddMessageAsync(Message message);

    /// <summary>Đánh dấu đã đọc mọi tin nhắn người khác gửi cho userId trong cuộc trò chuyện</summary>
    Task<int> MarkAsReadAsync(int conversationId, int userId);

    Task<int> CountUnreadAsync(int userId);
}
