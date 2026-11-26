using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Conversations;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Mappings;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;

namespace HouseRentalAPI.Services;

public class ConversationService(
    IConversationRepository conversationRepository,
    IUserRepository userRepository,
    IPropertyRepository propertyRepository,
    ICurrentUserService currentUser) : IConversationService
{
    public async Task<ConversationDto> StartAsync(StartConversationDto dto)
    {
        var userId = currentUser.GetRequiredUserId();

        if (dto.RecipientId == userId)
            throw new BadRequestException("Không thể nhắn tin cho chính mình");

        var recipient = await userRepository.GetByIdAsync(dto.RecipientId);
        if (recipient is not { IsActive: true })
            throw new NotFoundException($"Không tìm thấy người dùng có id {dto.RecipientId}");

        if (dto.PropertyId.HasValue && !await propertyRepository.AnyAsync(p => p.Id == dto.PropertyId.Value))
            throw new NotFoundException($"Không tìm thấy nhà có id {dto.PropertyId}");

        var conversation = await conversationRepository.FindBetweenAsync(userId, dto.RecipientId, dto.PropertyId);
        if (conversation is null)
        {
            conversation = new Conversation
            {
                InitiatorId = userId,
                RecipientId = dto.RecipientId,
                PropertyId = dto.PropertyId
            };
            await conversationRepository.AddAsync(conversation);
        }

        var message = await AddMessageAsync(conversation, userId, dto.Content);
        await conversationRepository.SaveChangesAsync();

        var created = await conversationRepository.GetDetailAsync(conversation.Id);
        return created!.ToDto(userId, message, 0);
    }

    public async Task<ConversationDto> ContactSupportAsync(string content)
    {
        if (currentUser.IsAdmin)
            throw new BadRequestException("Quản trị viên không cần liên hệ bộ phận hỗ trợ");

        var userId = currentUser.GetRequiredUserId();
        var adminIds = await userRepository.GetActiveAdminIdsAsync();
        if (adminIds.Count == 0)
            throw new NotFoundException("Hiện chưa có quản trị viên nào để hỗ trợ");

        // Ưu tiên admin đã từng trò chuyện để giữ nguyên lịch sử hỗ trợ
        var recipientId = adminIds[0];
        foreach (var adminId in adminIds)
        {
            if (await conversationRepository.FindBetweenAsync(userId, adminId, null) is not null)
            {
                recipientId = adminId;
                break;
            }
        }

        return await StartAsync(new StartConversationDto { RecipientId = recipientId, Content = content });
    }

    public async Task<PagedResult<ConversationDto>> GetMineAsync(PaginationQuery query)
    {
        var userId = currentUser.GetRequiredUserId();
        var result = await conversationRepository.GetByUserAsync(userId, query);
        return result.Map(x => x.Conversation.ToDto(userId, x.LastMessage, x.UnreadCount));
    }

    public async Task<PagedResult<MessageDto>> GetMessagesAsync(int conversationId, PaginationQuery query)
    {
        var conversation = await FindMineAsync(conversationId);
        var userId = currentUser.GetRequiredUserId();

        var result = await conversationRepository.GetMessagesAsync(conversation.Id, query);
        await conversationRepository.MarkAsReadAsync(conversation.Id, userId);

        return result.Map(m => m.ToDto());
    }

    public async Task<MessageDto> SendAsync(int conversationId, string content)
    {
        var conversation = await FindMineAsync(conversationId);
        var userId = currentUser.GetRequiredUserId();

        var message = await AddMessageAsync(conversation, userId, content);
        await conversationRepository.SaveChangesAsync();

        message.Sender = conversation.InitiatorId == userId ? conversation.Initiator : conversation.Recipient;
        return message.ToDto();
    }

    public Task<int> GetUnreadCountAsync() =>
        conversationRepository.CountUnreadAsync(currentUser.GetRequiredUserId());

    private async Task<Message> AddMessageAsync(Conversation conversation, int senderId, string content)
    {
        var message = new Message
        {
            Conversation = conversation,
            SenderId = senderId,
            Content = content.Trim()
        };

        conversation.LastMessageAt = DateTime.UtcNow;
        await conversationRepository.AddMessageAsync(message);
        return message;
    }

    private async Task<Conversation> FindMineAsync(int id)
    {
        var conversation = await conversationRepository.GetDetailAsync(id);
        var userId = currentUser.GetRequiredUserId();

        if (conversation is null || (conversation.InitiatorId != userId && conversation.RecipientId != userId))
            throw new NotFoundException($"Không tìm thấy cuộc trò chuyện có id {id}");

        return conversation;
    }
}
