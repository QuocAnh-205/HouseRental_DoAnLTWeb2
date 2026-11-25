using HouseRentalAPI.Constants;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Statistics;
using HouseRentalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HouseRentalAPI.Controllers;

[ApiController]
[Route("api/statistics")]
[Authorize]
[Produces("application/json")]
public class StatisticsController(IStatisticsService statisticsService) : ControllerBase
{
    /// <summary>[Admin] Tổng quan hệ thống: người dùng, tin đăng, hợp đồng, doanh thu 12 tháng</summary>
    [HttpGet("admin")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<AdminStatisticsDto>>> GetAdmin()
    {
        var result = await statisticsService.GetAdminAsync();
        return ApiResponse.Ok(result);
    }

    /// <summary>[Landlord] Tổng quan của chủ nhà: tin đăng, lượt xem, hợp đồng, công nợ, doanh thu</summary>
    [HttpGet("landlord")]
    [Authorize(Roles = AppRoles.Landlord)]
    public async Task<ActionResult<ApiResponse<LandlordStatisticsDto>>> GetLandlord()
    {
        var result = await statisticsService.GetLandlordAsync();
        return ApiResponse.Ok(result);
    }

    /// <summary>[Tenant] Tổng quan của người thuê: hợp đồng, lịch hẹn, hóa đơn chưa trả</summary>
    [HttpGet("tenant")]
    [Authorize(Roles = AppRoles.Tenant)]
    public async Task<ActionResult<ApiResponse<TenantStatisticsDto>>> GetTenant()
    {
        var result = await statisticsService.GetTenantAsync();
        return ApiResponse.Ok(result);
    }
}
