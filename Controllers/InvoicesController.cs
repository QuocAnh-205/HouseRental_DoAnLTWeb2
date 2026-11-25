using HouseRentalAPI.Constants;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Invoices;
using HouseRentalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HouseRentalAPI.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[Produces("application/json")]
public class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    /// <summary>
    /// [Landlord/Admin] Lập hóa đơn tháng (tiền thuê + điện + nước + dịch vụ).
    /// Để chỉ số cũ = 0 thì hệ thống lấy chỉ số mới của hóa đơn trước.
    /// </summary>
    [HttpPost("bookings/{bookingId:int}/invoices")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Create(int bookingId, CreateInvoiceDto dto)
    {
        var result = await invoiceService.CreateAsync(bookingId, dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse.Ok(result, "Đã lập hóa đơn"));
    }

    /// <summary>Hóa đơn của một hợp đồng</summary>
    [HttpGet("bookings/{bookingId:int}/invoices")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InvoiceDto>>>> GetByBooking(int bookingId)
    {
        var result = await invoiceService.GetByBookingAsync(bookingId);
        return ApiResponse.Ok(result);
    }

    /// <summary>Chi tiết hóa đơn</summary>
    [HttpGet("invoices/{id:int}")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetById(int id)
    {
        var result = await invoiceService.GetByIdAsync(id);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Tenant] Hóa đơn của tôi</summary>
    [HttpGet("invoices/mine")]
    [Authorize(Roles = AppRoles.Tenant)]
    public async Task<ActionResult<ApiResponse<PagedResult<InvoiceDto>>>> GetMine([FromQuery] InvoiceQueryDto query)
    {
        var result = await invoiceService.GetMineAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Landlord] Hóa đơn đã lập cho các nhà của tôi</summary>
    [HttpGet("invoices/received")]
    [Authorize(Roles = AppRoles.Landlord)]
    public async Task<ActionResult<ApiResponse<PagedResult<InvoiceDto>>>> GetReceived([FromQuery] InvoiceQueryDto query)
    {
        var result = await invoiceService.GetReceivedAsync(query);
        return ApiResponse.Ok(result);
    }

    /// <summary>[Landlord/Admin] Hủy hóa đơn chưa thanh toán</summary>
    [HttpPatch("invoices/{id:int}/cancel")]
    [Authorize(Roles = AppRoles.AdminOrLandlord)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Cancel(int id)
    {
        var result = await invoiceService.CancelAsync(id);
        return ApiResponse.Ok(result, "Đã hủy hóa đơn");
    }
}
