using HouseRentalAPI.Constants;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Reports;
using HouseRentalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HouseRentalAPI.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[Produces("application/json")]
public class ReportsController(IReportService reportService) : ControllerBase
{
    /// <summary>Báo cáo tin đăng vi phạm</summary>
    [HttpPost("properties/{propertyId:int}/reports")]
    public async Task<ActionResult<ApiResponse<ReportDto>>> Create(int propertyId, CreateReportDto dto)
    {
        var result = await reportService.CreateAsync(propertyId, dto);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(result, "Cảm ơn bạn đã báo cáo, chúng tôi sẽ xem xét sớm"));
    }

    /// <summary>Các báo cáo tôi đã gửi</summary>
    [HttpGet("reports/mine")]
    public async Task<ActionResult<ApiResponse<PagedResult<ReportDto>>>> GetMine([FromQuery] ReportQueryDto query)
    {
        var result = await reportService.GetMineAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Admin] Danh sách báo cáo vi phạm</summary>
    [HttpGet("reports")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReportDto>>>> GetAll([FromQuery] ReportQueryDto query)
    {
        var result = await reportService.GetAllAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Admin] Xử lý báo cáo (Resolved / Dismissed), có thể ẩn luôn tin đăng</summary>
    [HttpPatch("reports/{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<ReportDto>>> Handle(int id, HandleReportDto dto)
    {
        var result = await reportService.HandleAsync(id, dto);
        return ApiResponse.Ok(result, "Đã xử lý báo cáo");
    }
}
