using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Properties;
using HouseRentalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HouseRentalAPI.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
[Produces("application/json")]
public class FavoritesController(IFavoriteService favoriteService) : ControllerBase
{
    /// <summary>Danh sách nhà yêu thích của tôi</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PropertySummaryDto>>>> GetMine([FromQuery] PaginationQuery query)
    {
        var result = await favoriteService.GetMineAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>Lưu nhà vào danh sách yêu thích</summary>
    [HttpPost("{propertyId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Add(int propertyId)
    {
        await favoriteService.AddAsync(propertyId);
        return ApiResponse.Ok("Đã lưu vào danh sách yêu thích");
    }

    /// <summary>Bỏ nhà khỏi danh sách yêu thích</summary>
    [HttpDelete("{propertyId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Remove(int propertyId)
    {
        await favoriteService.RemoveAsync(propertyId);
        return ApiResponse.Ok("Đã bỏ khỏi danh sách yêu thích");
    }
}
