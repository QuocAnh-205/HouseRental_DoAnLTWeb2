using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Reviews;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Mappings;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;

namespace HouseRentalAPI.Services;

public class ReviewService(
    IReviewRepository reviewRepository,
    IPropertyRepository propertyRepository,
    IBookingRepository bookingRepository,
    INotificationService notificationService,
    ICurrentUserService currentUser) : IReviewService
{
    public async Task<PagedResult<ReviewDto>> GetByPropertyAsync(int propertyId, PaginationQuery query)
    {
        if (!await propertyRepository.AnyAsync(p => p.Id == propertyId))
            throw new NotFoundException($"Không tìm thấy nhà có id {propertyId}");

        var result = await reviewRepository.GetByPropertyAsync(propertyId, query);
        return result.Map(r => r.ToDto());
    }

    public async Task<ReviewEligibilityDto> GetEligibilityAsync(int propertyId)
    {
        var tenantId = currentUser.GetRequiredUserId();
        if (!await propertyRepository.AnyAsync(p => p.Id == propertyId))
            throw new NotFoundException($"Không tìm thấy nhà có id {propertyId}");

        var hasRented = await bookingRepository.HasRentedAsync(tenantId, propertyId);
        var myReview = await reviewRepository.GetByTenantAsync(tenantId, propertyId);
        return new ReviewEligibilityDto(hasRented && myReview is null, hasRented, myReview?.ToDto());
    }

    public async Task<ReviewDto> CreateAsync(int propertyId, CreateReviewDto dto)
    {
        var tenantId = currentUser.GetRequiredUserId();

        var property = await propertyRepository.GetByIdAsync(propertyId)
            ?? throw new NotFoundException($"Không tìm thấy nhà có id {propertyId}");

        if (!await bookingRepository.HasRentedAsync(tenantId, propertyId))
            throw new ForbiddenException("Bạn chỉ có thể đánh giá nhà mình đã thuê");

        if (await reviewRepository.ExistsAsync(tenantId, propertyId))
            throw new ConflictException("Bạn đã đánh giá nhà này rồi");

        var review = new Review
        {
            PropertyId = propertyId,
            TenantId = tenantId,
            Rating = dto.Rating,
            Comment = dto.Comment?.Trim()
        };

        await reviewRepository.AddAsync(review);
        await notificationService.NotifyAsync(property.LandlordId, NotificationType.Review,
            "Có đánh giá mới",
            $"Nhà \"{property.Title}\" vừa nhận được đánh giá {dto.Rating} sao.",
            $"/api/properties/{property.Id}/reviews");
        await reviewRepository.SaveChangesAsync();

        await RefreshRatingAsync(property);

        var created = await reviewRepository.GetDetailAsync(review.Id);
        return created!.ToDto();
    }

    public async Task<ReviewDto> UpdateAsync(int id, CreateReviewDto dto)
    {
        var review = await FindAsync(id);

        if (review.TenantId != currentUser.UserId)
            throw new ForbiddenException("Bạn chỉ có thể sửa đánh giá của mình");

        review.Rating = dto.Rating;
        review.Comment = dto.Comment?.Trim();
        await reviewRepository.SaveChangesAsync();

        await RefreshRatingAsync(review.Property);
        return review.ToDto();
    }

    public async Task<ReviewDto> ReplyAsync(int id, string reply)
    {
        var review = await FindAsync(id);

        if (!currentUser.IsAdmin && review.Property.LandlordId != currentUser.UserId)
            throw new ForbiddenException("Chỉ chủ nhà mới được phản hồi đánh giá");

        review.LandlordReply = reply.Trim();
        review.RepliedAt = DateTime.UtcNow;

        await notificationService.NotifyAsync(review.TenantId, NotificationType.Review,
            "Chủ nhà đã phản hồi đánh giá của bạn",
            $"Chủ nhà \"{review.Property.Title}\" đã phản hồi đánh giá của bạn.",
            $"/api/properties/{review.PropertyId}/reviews");
        await reviewRepository.SaveChangesAsync();

        return review.ToDto();
    }

    public async Task DeleteAsync(int id)
    {
        var review = await FindAsync(id);

        if (!currentUser.IsAdmin && review.TenantId != currentUser.UserId)
            throw new ForbiddenException("Bạn không có quyền xóa đánh giá này");

        reviewRepository.Remove(review);
        await reviewRepository.SaveChangesAsync();

        await RefreshRatingAsync(review.Property);
    }

    private async Task<Review> FindAsync(int id) =>
        await reviewRepository.GetDetailAsync(id)
        ?? throw new NotFoundException($"Không tìm thấy đánh giá có id {id}");

    private async Task RefreshRatingAsync(Property property)
    {
        var (average, count) = await reviewRepository.GetStatsAsync(property.Id);
        property.AverageRating = Math.Round(average, 1);
        property.ReviewCount = count;
        await propertyRepository.SaveChangesAsync();
    }
}
