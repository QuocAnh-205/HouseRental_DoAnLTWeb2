using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Reviews;

namespace HouseRentalAPI.Services;

public interface IReviewService
{
    Task<PagedResult<ReviewDto>> GetByPropertyAsync(int propertyId, PaginationQuery query);
    Task<ReviewDto> CreateAsync(int propertyId, CreateReviewDto dto);

    /// <summary>Người thuê hiện tại có được đánh giá căn nhà này không, và đánh giá đã viết (nếu có)</summary>
    Task<ReviewEligibilityDto> GetEligibilityAsync(int propertyId);

    /// <summary>Người viết sửa đánh giá của mình</summary>
    Task<ReviewDto> UpdateAsync(int id, CreateReviewDto dto);

    /// <summary>Chủ nhà phản hồi đánh giá (gửi lại sẽ ghi đè phản hồi cũ)</summary>
    Task<ReviewDto> ReplyAsync(int id, string reply);

    Task DeleteAsync(int id);
}
