using HouseRentalAPI.Constants;
using HouseRentalAPI.DTOs.Appointments;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HouseRentalAPI.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
[Produces("application/json")]
public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    /// <summary>[Tenant] Đặt lịch hẹn xem nhà</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Tenant)]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> Create(CreateAppointmentDto dto)
    {
        var result = await appointmentService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse.Ok(result, "Đã gửi lịch hẹn, chờ chủ nhà xác nhận"));
    }

    /// <summary>Chi tiết lịch hẹn (người thuê, chủ nhà hoặc admin)</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> GetById(int id)
    {
        var result = await appointmentService.GetByIdAsync(id);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Tenant] Lịch hẹn của tôi</summary>
    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Tenant)]
    public async Task<ActionResult<ApiResponse<PagedResult<AppointmentDto>>>> GetMine([FromQuery] AppointmentQueryDto query)
    {
        var result = await appointmentService.GetMineAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Landlord] Lịch hẹn xem các nhà của tôi</summary>
    [HttpGet("received")]
    [Authorize(Roles = AppRoles.Landlord)]
    public async Task<ActionResult<ApiResponse<PagedResult<AppointmentDto>>>> GetReceived([FromQuery] AppointmentQueryDto query)
    {
        var result = await appointmentService.GetReceivedAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Landlord/Admin] Xác nhận lịch hẹn</summary>
    [HttpPatch("{id:int}/confirm")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> Confirm(int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AppointmentResponseDto? dto)
    {
        var result = await appointmentService.ConfirmAsync(id, dto?.Note);
        return ApiResponse.Ok(result, "Đã xác nhận lịch hẹn");
    }

    /// <summary>[Landlord/Admin] Từ chối lịch hẹn</summary>
    [HttpPatch("{id:int}/reject")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> Reject(int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AppointmentResponseDto? dto)
    {
        var result = await appointmentService.RejectAsync(id, dto?.Note);
        return ApiResponse.Ok(result, "Đã từ chối lịch hẹn");
    }

    /// <summary>Hủy lịch hẹn (người thuê, chủ nhà hoặc admin)</summary>
    [HttpPatch("{id:int}/cancel")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> Cancel(int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AppointmentResponseDto? dto)
    {
        var result = await appointmentService.CancelAsync(id, dto?.Note);
        return ApiResponse.Ok(result, "Đã hủy lịch hẹn");
    }

    /// <summary>[Landlord/Admin] Đánh dấu đã dẫn khách xem nhà</summary>
    [HttpPatch("{id:int}/complete")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> Complete(int id)
    {
        var result = await appointmentService.CompleteAsync(id);
        return ApiResponse.Ok(result, "Đã hoàn tất lịch hẹn");
    }
}
