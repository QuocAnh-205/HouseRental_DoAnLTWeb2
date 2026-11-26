using HouseRentalAPI.Data;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Extensions;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Repositories;

public class ConversationRepository(AppDbContext context) : Repository<Conversation>(context), IConversationRepository
{
    public Task<Conversation?> GetDetailAsync(int id) =>
        DbSet
            .Include(c => c.Initiator)
            .Include(c => c.Recipient)
            .Include(c => c.Property)
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<Conversation?> FindBetweenAsync(int userId1, int userId2, int? propertyId) =>
        DbSet
            .Include(c => c.Initiator)
            .Include(c => c.Recipient)
            .Include(c => c.Property)
            .FirstOrDefaultAsync(c =>
                ((c.InitiatorId == userId1 && c.RecipientId == userId2) ||
                 (c.InitiatorId == userId2 && c.RecipientId == userId1)) &&
                c.PropertyId == propertyId);

    public async Task<PagedResult<ConversationSummary>> GetByUserAsync(int userId, PaginationQuery query)
    {
        var page = await DbSet
            .AsNoTracking()
            .Where(c => c.InitiatorId == userId || c.RecipientId == userId)
            .OrderByDescending(c => c.LastMessageAt)
            .Select(c => new
            {
                Conversation = c,
                c.Initiator,
                c.Recipient,
                // Chỉ lấy tiêu đề nhà: tránh kéo cột Description dài vào truy vấn có sắp xếp
                PropertyTitle = c.Property != null ? c.Property.Title : null,
                LastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault(),
                UnreadCount = c.Messages.Count(m => m.SenderId != userId && !m.IsRead)
            })
            .ToPagedResultAsync(query.Page, query.PageSize);

        return page.Map(x =>
        {
            x.Conversation.Initiator = x.Initiator;
            x.Conversation.Recipient = x.Recipient;
            x.Conversation.Property = x.Conversation.PropertyId is { } propertyId
                ? new Property { Id = propertyId, Title = x.PropertyTitle ?? string.Empty }
                : null;
            return new ConversationSummary(x.Conversation, x.LastMessage, x.UnreadCount);
        });
    }

    public Task<PagedResult<Message>> GetMessagesAsync(int conversationId, PaginationQuery query) =>
        Context.Messages
            .AsNoTracking()
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .ToPagedResultAsync(query.Page, query.PageSize);

    public async Task AddMessageAsync(Message message) =>
        await Context.Messages.AddAsync(message);

    public Task<int> MarkAsReadAsync(int conversationId, int userId)
    {
        var now = DateTime.UtcNow;
        return Context.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.IsRead, true)
                .SetProperty(m => m.ReadAt, now));
    }

    public Task<int> CountUnreadAsync(int userId) =>
        Context.Messages.CountAsync(m =>
            m.SenderId != userId &&
            !m.IsRead &&
            (m.Conversation.InitiatorId == userId || m.Conversation.RecipientId == userId));
}
