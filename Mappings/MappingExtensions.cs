using HouseRentalAPI.DTOs.Appointments;
using HouseRentalAPI.DTOs.Bookings;
using HouseRentalAPI.DTOs.Conversations;
using HouseRentalAPI.DTOs.Invoices;
using HouseRentalAPI.DTOs.Notifications;
using HouseRentalAPI.DTOs.Payments;
using HouseRentalAPI.DTOs.Properties;
using HouseRentalAPI.DTOs.Reports;
using HouseRentalAPI.DTOs.Reviews;
using HouseRentalAPI.DTOs.Users;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Mappings;

/// <summary>
/// Map thủ công Entity -> DTO (nhẹ, dễ debug, không cần AutoMapper).
/// </summary>
public static class MappingExtensions
{
    public static UserDto ToDto(this User user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.PhoneNumber,
        user.AvatarUrl,
        user.Role,
        user.IsActive,
        user.LastLoginAt,
        user.CreatedAt);

    public static PropertySummaryDto ToSummaryDto(this Property p) => new(
        p.Id,
        p.Title,
        p.Address,
        p.District,
        p.City,
        p.Type,
        p.Area,
        p.Bedrooms,
        p.Bathrooms,
        p.PricePerMonth,
        p.Status,
        p.Images.FirstOrDefault(i => i.IsThumbnail)?.Url ?? p.Images.FirstOrDefault()?.Url,
        p.AverageRating,
        p.ReviewCount,
        p.ViewCount,
        p.LandlordId,
        p.Landlord?.FullName ?? string.Empty,
        p.CreatedAt,
        p.Ward,
        p.Images.Count,
        p.Landlord?.AvatarUrl,
        p.Latitude,
        p.Longitude);

    public static PropertyPostDto ToPostDto(this Property p, bool includeRejectReason)
    {
        const int excerptLength = 280;
        var description = p.Description.Trim();
        var excerpt = description;
        if (description.Length > excerptLength)
        {
            // Cắt ở khoảng trắng gần nhất để không cắt đôi một từ
            var cut = description.LastIndexOf(' ', excerptLength);
            excerpt = description[..(cut > 0 ? cut : excerptLength)].TrimEnd() + "…";
        }
        var images = p.Images.OrderByDescending(i => i.IsThumbnail).ThenBy(i => i.Id).ToList();

        return new PropertyPostDto(
            p.Id, p.Title, excerpt, p.Type, p.PricePerMonth, p.Area, p.Bedrooms, p.Bathrooms,
            p.Address, p.Ward, p.District, p.City, p.Status,
            includeRejectReason ? p.RejectReason : null,
            images.Take(4).Select(i => i.Url).ToList(),
            images.Count,
            p.Amenities.Take(6).ToList(),
            p.ViewCount, p.AverageRating, p.ReviewCount, p.CreatedAt, p.PublishedAt,
            p.LandlordId, p.Landlord?.FullName ?? string.Empty, p.Landlord?.AvatarUrl);
    }

    // includeContact = false: ẩn email / SĐT chủ nhà (khách chưa đăng nhập)
    public static PropertyDetailDto ToDetailDto(this Property p, bool isFavorited = false, bool includeContact = true) => new(
        p.Id,
        p.Title,
        p.Description,
        p.Address,
        p.Ward,
        p.District,
        p.City,
        p.Latitude,
        p.Longitude,
        p.Type,
        p.Area,
        p.Bedrooms,
        p.Bathrooms,
        p.PricePerMonth,
        p.Deposit,
        p.Amenities,
        p.Status,
        p.RejectReason,
        p.AverageRating,
        p.ReviewCount,
        p.ViewCount,
        isFavorited,
        new LandlordInfoDto(p.Landlord.Id, p.Landlord.FullName,
            includeContact ? p.Landlord.Email : null,
            includeContact ? p.Landlord.PhoneNumber : null,
            p.Landlord.AvatarUrl),
        p.Images.OrderByDescending(i => i.IsThumbnail).ThenBy(i => i.Id).Select(i => i.ToDto()).ToList(),
        p.CreatedAt,
        p.UpdatedAt);

    public static PropertyImageDto ToDto(this PropertyImage image) => new(image.Id, image.Url, image.IsThumbnail);

    public static BookingDto ToDto(this Booking b) => new(
        b.Id,
        b.PropertyId,
        b.Property?.Title ?? string.Empty,
        b.Property?.Address ?? string.Empty,
        b.Property?.LandlordId ?? 0,
        b.TenantId,
        b.Tenant?.FullName ?? string.Empty,
        b.Tenant?.PhoneNumber,
        b.StartDate,
        b.EndDate,
        b.DurationMonths,
        b.MonthlyPrice,
        b.Deposit,
        b.TotalAmount,
        b.Status,
        b.Note,
        b.RejectReason,
        b.CreatedAt);

    public static ReviewDto ToDto(this Review r) => new(
        r.Id,
        r.PropertyId,
        r.TenantId,
        r.Tenant?.FullName ?? string.Empty,
        r.Tenant?.AvatarUrl,
        r.Rating,
        r.Comment,
        r.LandlordReply,
        r.RepliedAt,
        r.CreatedAt,
        r.UpdatedAt);

    public static PaymentDto ToDto(this Payment p) => new(
        p.Id,
        p.BookingId,
        p.InvoiceId,
        p.Booking?.Property?.Title,
        p.Amount,
        p.Type,
        p.Method,
        p.Status,
        p.TransactionCode,
        p.Note,
        p.PaidAt,
        p.CreatedAt);

    public static NotificationDto ToDto(this Notification n) => new(
        n.Id,
        n.Type,
        n.Title,
        n.Message,
        n.Link,
        n.IsRead,
        n.ReadAt,
        n.CreatedAt);

    public static MessageDto ToDto(this Message m) => new(
        m.Id,
        m.ConversationId,
        m.SenderId,
        m.Sender?.FullName ?? string.Empty,
        m.Content,
        m.IsRead,
        m.ReadAt,
        m.CreatedAt);

    /// <summary>Map cuộc trò chuyện theo góc nhìn của người dùng hiện tại</summary>
    public static ConversationDto ToDto(this Conversation c, int currentUserId, Message? lastMessage, int unreadCount)
    {
        var other = c.InitiatorId == currentUserId ? c.Recipient : c.Initiator;
        return new ConversationDto(
            c.Id,
            new ParticipantDto(other.Id, other.FullName, other.AvatarUrl, other.Role),
            c.PropertyId,
            c.Property?.Title,
            lastMessage?.Content,
            c.LastMessageAt,
            unreadCount);
    }

    public static AppointmentDto ToDto(this Appointment a) => new(
        a.Id,
        a.PropertyId,
        a.Property?.Title ?? string.Empty,
        a.Property?.Address ?? string.Empty,
        a.Property?.LandlordId ?? 0,
        a.TenantId,
        a.Tenant?.FullName ?? string.Empty,
        a.Tenant?.PhoneNumber,
        a.ScheduledAt,
        a.Status,
        a.Note,
        a.LandlordNote,
        a.CreatedAt);

    public static ReportDto ToDto(this Report r) => new(
        r.Id,
        r.PropertyId,
        r.Property?.Title ?? string.Empty,
        r.ReporterId,
        r.Reporter?.FullName ?? string.Empty,
        r.Reason,
        r.Description,
        r.Status,
        r.AdminNote,
        r.HandledAt,
        r.CreatedAt);

    public static InvoiceDto ToDto(this Invoice i) => new(
        i.Id,
        i.BookingId,
        i.Booking?.PropertyId ?? 0,
        i.Booking?.Property?.Title ?? string.Empty,
        i.Booking?.TenantId ?? 0,
        i.Booking?.Tenant?.FullName ?? string.Empty,
        i.Month,
        i.Year,
        i.RentAmount,
        i.ElectricityOldIndex,
        i.ElectricityNewIndex,
        i.ElectricityUnitPrice,
        i.ElectricityAmount,
        i.WaterOldIndex,
        i.WaterNewIndex,
        i.WaterUnitPrice,
        i.WaterAmount,
        i.ServiceFee,
        i.OtherFee,
        i.TotalAmount,
        i.PaidAmount,
        Math.Max(0, i.TotalAmount - i.PaidAmount),
        i.DueDate,
        i.Status,
        i.Note,
        i.CreatedAt);
}
