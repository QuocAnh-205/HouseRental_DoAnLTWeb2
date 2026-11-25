using HouseRentalAPI.Constants;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Payments;
using HouseRentalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HouseRentalAPI.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[Produces("application/json")]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    /// <summary>[Tenant] Tạo thanh toán (tiền cọc / tiền thuê tháng) cho hợp đồng</summary>
    [HttpPost("bookings/{bookingId:int}/payments")]
    [Authorize(Roles = AppRoles.Tenant)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Create(int bookingId, CreatePaymentDto dto)
    {
        var result = await paymentService.CreateAsync(bookingId, dto);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(result, "Tạo thanh toán thành công, chờ chủ nhà xác nhận"));
    }

    /// <summary>Lịch sử thanh toán của hợp đồng</summary>
    [HttpGet("bookings/{bookingId:int}/payments")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PaymentDto>>>> GetByBooking(int bookingId)
    {
        var result = await paymentService.GetByBookingAsync(bookingId);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Tenant] Lịch sử thanh toán của tôi</summary>
    [HttpGet("payments/mine")]
    [Authorize(Roles = AppRoles.Tenant)]
    public async Task<ActionResult<ApiResponse<PagedResult<PaymentDto>>>> GetMine([FromQuery] PaymentQueryDto query)
    {
        var result = await paymentService.GetMineAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Landlord] Các khoản thanh toán nhận được (lọc Status=Pending để xem khoản chờ xác nhận)</summary>
    [HttpGet("payments/received")]
    [Authorize(Roles = AppRoles.Landlord)]
    public async Task<ActionResult<ApiResponse<PagedResult<PaymentDto>>>> GetReceived([FromQuery] PaymentQueryDto query)
    {
        var result = await paymentService.GetReceivedAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Landlord/Admin] Xác nhận thanh toán (Pending → Success / Failed) hoặc hoàn tiền (Success → Refunded)</summary>
    [HttpPatch("payments/{id:int}/status")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> UpdateStatus(int id, UpdatePaymentStatusDto dto)
    {
        var result = await paymentService.UpdateStatusAsync(id, dto.Status!.Value);
        return ApiResponse.Ok(result, "Cập nhật thanh toán thành công");
    }
}
