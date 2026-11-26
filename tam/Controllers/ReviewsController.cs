using HouseRentalAPI.Constants;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Reviews;
using HouseRentalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HouseRentalAPI.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class ReviewsController(IReviewService reviewService) : ControllerBase
{
    /// <summary>Danh sách đánh giá của một căn nhà</summary>
    [HttpGet("properties/{propertyId:int}/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<ReviewDto>>>> GetByProperty(int propertyId, [FromQuery] PaginationQuery query)
    {
        var result = await reviewService.GetByPropertyAsync(propertyId, query);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Tenant] Kiểm tra tôi có được đánh giá căn nhà này không (đã thuê, chưa đánh giá) và đánh giá của tôi</summary>
    [HttpGet("properties/{propertyId:int}/reviews/eligibility")]
    [Authorize(Roles = AppRoles.Tenant)]
    public async Task<ActionResult<ApiResponse<ReviewEligibilityDto>>> GetEligibility(int propertyId) =>
        ApiResponse.Ok(await reviewService.GetEligibilityAsync(propertyId));

    /// <summary>[Tenant] Đánh giá nhà đã thuê</summary>
    [HttpPost("properties/{propertyId:int}/reviews")]
    [Authorize(Roles = AppRoles.Tenant)]
    public async Task<ActionResult<ApiResponse<ReviewDto>>> Create(int propertyId, CreateReviewDto dto)
    {
        var result = await reviewService.CreateAsync(propertyId, dto);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(result, "Cảm ơn bạn đã đánh giá"));
    }

    /// <summary>[Tenant] Sửa đánh giá của mình</summary>
    [HttpPut("reviews/{id:int}")]
    [Authorize(Roles = AppRoles.Tenant)]
    public async Task<ActionResult<ApiResponse<ReviewDto>>> Update(int id, CreateReviewDto dto)
    {
        var result = await reviewService.UpdateAsync(id, dto);
        return ApiResponse.Ok(result, "Cập nhật đánh giá thành công");
    }

    /// <summary>[Landlord/Admin] Phản hồi đánh giá</summary>
    [HttpPut("reviews/{id:int}/reply")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<ReviewDto>>> Reply(int id, ReplyReviewDto dto)
    {
        var result = await reviewService.ReplyAsync(id, dto.Reply);
        return ApiResponse.Ok(result, "Đã phản hồi đánh giá");
    }

    /// <summary>Xóa đánh giá (người viết hoặc admin)</summary>
    [HttpDelete("reviews/{id:int}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await reviewService.DeleteAsync(id);
        return ApiResponse.Ok("Xóa đánh giá thành công");
    }
}
