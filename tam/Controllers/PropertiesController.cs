using HouseRentalAPI.Constants;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Properties;
using HouseRentalAPI.Models;
using HouseRentalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HouseRentalAPI.Controllers;

[ApiController]
[Route("api/properties")]
[Produces("application/json")]
public class PropertiesController(IPropertyService propertyService) : ControllerBase
{
    /// <summary>Tìm kiếm nhà cho thuê (công khai)</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<PropertySummaryDto>>>> Search([FromQuery] PropertyQueryDto query)
    {
        var result = await propertyService.SearchAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>Số tin đang cho thuê theo loại nhà và theo quận/huyện (công khai)</summary>
    [HttpGet("stats")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PropertyStatsDto>>> GetStats([FromQuery] string? city, [FromQuery] PropertyType? type)
    {
        var result = await propertyService.GetStatsAsync(city, type);
        return ApiResponse.Ok(result);
    }

    /// <summary>
    /// Bản tin chung (công khai): tin cho thuê của mọi chủ nhà dạng bài viết,
    /// tin vừa được admin duyệt hiện lên đầu. Lọc tùy chọn theo loại nhà.
    /// </summary>
    [HttpGet("feed")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<PropertyPostDto>>>> GetFeed([FromQuery] PropertyType? type, [FromQuery] PaginationQuery query)
    {
        var result = await propertyService.GetFeedAsync(type, query);
        return ApiResponse.Ok(result);
    }

    /// <summary>Chi tiết một căn nhà</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PropertyDetailDto>>> GetById(int id)
    {
        var result = await propertyService.GetByIdAsync(id);
        return ApiResponse.Ok(result);
    }

    /// <summary>Nhà tương tự (cùng thành phố, cùng loại, giá gần bằng)</summary>
    [HttpGet("{id:int}/similar")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PropertySummaryDto>>>> GetSimilar(int id, [FromQuery] int count = 6)
    {
        var result = await propertyService.GetSimilarAsync(id, count);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Landlord] Danh sách nhà của tôi</summary>
    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Landlord)]
    public async Task<ActionResult<ApiResponse<PagedResult<PropertySummaryDto>>>> GetMine([FromQuery] PropertyQueryDto query)
    {
        var result = await propertyService.GetMineAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Admin] Quản lý toàn bộ tin đăng (lọc theo Status, ví dụ PendingApproval)</summary>
    [HttpGet("manage")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<PagedResult<PropertySummaryDto>>>> GetAllForAdmin([FromQuery] PropertyQueryDto query)
    {
        var result = await propertyService.GetAllForAdminAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Landlord] Đăng tin cho thuê mới (chờ admin duyệt)</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Landlord)]
    public async Task<ActionResult<ApiResponse<PropertyDetailDto>>> Create(CreatePropertyDto dto)
    {
        var result = await propertyService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse.Ok(result, "Đăng tin thành công, vui lòng chờ duyệt"));
    }

    /// <summary>[Landlord/Admin] Cập nhật thông tin nhà</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<PropertyDetailDto>>> Update(int id, UpdatePropertyDto dto)
    {
        var result = await propertyService.UpdateAsync(id, dto);
        return ApiResponse.Ok(result, "Cập nhật thành công");
    }

    /// <summary>[Landlord/Admin] Xóa tin đăng (chỉ khi chưa có lịch sử thuê)</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await propertyService.DeleteAsync(id);
        return ApiResponse.Ok("Xóa tin đăng thành công");
    }

    /// <summary>
    /// Đổi trạng thái tin đăng.
    /// Admin: duyệt (Available) / từ chối (Rejected, kèm Reason) / bất kỳ. Landlord: chỉ ẩn (Hidden) / hiện (Available).
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<PropertyDetailDto>>> UpdateStatus(int id, UpdatePropertyStatusDto dto)
    {
        var result = await propertyService.UpdateStatusAsync(id, dto.Status!.Value, dto.Reason);
        return ApiResponse.Ok(result, "Cập nhật trạng thái thành công");
    }

    /// <summary>[Landlord/Admin] Thêm ảnh cho nhà</summary>
    [HttpPost("{id:int}/images")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<PropertyImageDto>>> AddImage(int id, AddPropertyImageDto dto)
    {
        var result = await propertyService.AddImageAsync(id, dto);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(result, "Thêm ảnh thành công"));
    }

    /// <summary>[Landlord/Admin] Xóa ảnh của nhà</summary>
    [HttpDelete("{id:int}/images/{imageId:int}")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<object>>> RemoveImage(int id, int imageId)
    {
        await propertyService.RemoveImageAsync(id, imageId);
        return ApiResponse.Ok("Xóa ảnh thành công");
    }

    /// <summary>[Landlord/Admin] Đặt ảnh làm ảnh đại diện</summary>
    [HttpPatch("{id:int}/images/{imageId:int}/thumbnail")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<object>>> SetThumbnail(int id, int imageId)
    {
        await propertyService.SetThumbnailAsync(id, imageId);
        return ApiResponse.Ok("Đã đặt ảnh đại diện");
    }
}
