using HouseRentalAPI.Constants;
using HouseRentalAPI.DTOs.Bookings;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HouseRentalAPI.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
[Produces("application/json")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    /// <summary>[Tenant] Gửi yêu cầu thuê nhà</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Tenant)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Create(CreateBookingDto dto)
    {
        var result = await bookingService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse.Ok(result, "Gửi yêu cầu thuê thành công"));
    }

    /// <summary>Chi tiết yêu cầu thuê (người thuê, chủ nhà hoặc admin)</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> GetById(int id)
    {
        var result = await bookingService.GetByIdAsync(id);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Tenant] Các yêu cầu thuê của tôi</summary>
    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Tenant)]
    public async Task<ActionResult<ApiResponse<PagedResult<BookingDto>>>> GetMine([FromQuery] BookingQueryDto query)
    {
        var result = await bookingService.GetMineAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Landlord] Các yêu cầu thuê gửi đến nhà của tôi</summary>
    [HttpGet("received")]
    [Authorize(Roles = AppRoles.Landlord)]
    public async Task<ActionResult<ApiResponse<PagedResult<BookingDto>>>> GetReceived([FromQuery] BookingQueryDto query)
    {
        var result = await bookingService.GetReceivedAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Admin] Toàn bộ yêu cầu thuê / hợp đồng</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<PagedResult<BookingDto>>>> GetAll([FromQuery] BookingQueryDto query)
    {
        var result = await bookingService.GetAllAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Landlord/Admin] Duyệt yêu cầu thuê</summary>
    [HttpPatch("{id:int}/approve")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Approve(int id)
    {
        var result = await bookingService.ApproveAsync(id);
        return ApiResponse.Ok(result, "Đã duyệt yêu cầu thuê");
    }

    /// <summary>[Landlord/Admin] Từ chối yêu cầu thuê</summary>
    [HttpPatch("{id:int}/reject")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Reject(int id, RejectBookingDto dto)
    {
        var result = await bookingService.RejectAsync(id, dto.Reason);
        return ApiResponse.Ok(result, "Đã từ chối yêu cầu thuê");
    }

    /// <summary>[Tenant/Admin] Hủy yêu cầu thuê</summary>
    [HttpPatch("{id:int}/cancel")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Cancel(int id)
    {
        var result = await bookingService.CancelAsync(id);
        return ApiResponse.Ok(result, "Đã hủy yêu cầu thuê");
    }

    /// <summary>[Landlord/Admin] Kết thúc hợp đồng thuê</summary>
    [HttpPatch("{id:int}/complete")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Complete(int id)
    {
        var result = await bookingService.CompleteAsync(id);
        return ApiResponse.Ok(result, "Đã kết thúc hợp đồng thuê");
    }
}
