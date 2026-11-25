using HouseRentalAPI.Constants;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Properties;
using HouseRentalAPI.DTOs.Users;
using HouseRentalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HouseRentalAPI.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class UsersController(IUserService userService, IPropertyService propertyService) : ControllerBase
{
    /// <summary>[Admin] Danh sách người dùng, có tìm kiếm và phân trang</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetAll([FromQuery] UserQueryDto query)
    {
        var result = await userService.GetPagedAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>Xem thông tin người dùng (Admin hoặc chính mình)</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(int id)
    {
        var result = await userService.GetByIdAsync(id);
        return ApiResponse.Ok(result);
    }

    /// <summary>Hồ sơ công khai của người dùng (số tin đang cho thuê, điểm đánh giá)</summary>
    [HttpGet("{id:int}/profile")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PublicProfileDto>>> GetPublicProfile(int id)
    {
        var result = await userService.GetPublicProfileAsync(id);
        return ApiResponse.Ok(result);
    }

    /// <summary>Các tin đang cho thuê của một chủ nhà</summary>
    [HttpGet("{id:int}/properties")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<PropertySummaryDto>>>> GetProperties(int id, [FromQuery] PropertyQueryDto query)
    {
        var result = await propertyService.GetByLandlordAsync(id, query);
        return ApiResponse.Ok(result);
    }

    /// <summary>
    /// Bản tin trên trang cá nhân: các tin người dùng đã đăng, mới nhất trước.
    /// Chính chủ và admin thấy cả tin chờ duyệt / bị từ chối / đã ẩn; người khác chỉ thấy tin đã duyệt.
    /// </summary>
    [HttpGet("{id:int}/posts")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<PropertyPostDto>>>> GetPosts(int id, [FromQuery] PaginationQuery query)
    {
        await userService.GetPublicProfileAsync(id); // 404 nếu người dùng không tồn tại / bị khóa
        var result = await propertyService.GetPostsAsync(id, query);
        return ApiResponse.Ok(result);
    }

    /// <summary>Cập nhật hồ sơ cá nhân</summary>
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile(UpdateProfileDto dto)
    {
        var result = await userService.UpdateProfileAsync(dto);
        return ApiResponse.Ok(result, "Cập nhật hồ sơ thành công");
    }

    /// <summary>[Admin] Khóa / mở khóa tài khoản</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateStatus(int id, UpdateUserStatusDto dto)
    {
        var result = await userService.UpdateStatusAsync(id, dto.IsActive);
        return ApiResponse.Ok(result, dto.IsActive ? "Đã mở khóa tài khoản" : "Đã khóa tài khoản");
    }

    /// <summary>[Admin] Đổi vai trò người dùng</summary>
    [HttpPatch("{id:int}/role")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateRole(int id, UpdateUserRoleDto dto)
    {
        var result = await userService.UpdateRoleAsync(id, dto.Role!.Value);
        return ApiResponse.Ok(result, "Đã đổi vai trò người dùng");
    }
}
