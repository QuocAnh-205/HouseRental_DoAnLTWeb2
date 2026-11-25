using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Payments;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Services;

public interface IPaymentService
{
    Task<PaymentDto> CreateAsync(int bookingId, CreatePaymentDto dto);
    Task<IReadOnlyList<PaymentDto>> GetByBookingAsync(int bookingId);

    /// <summary>Lịch sử thanh toán của người thuê đang đăng nhập</summary>
    Task<PagedResult<PaymentDto>> GetMineAsync(PaymentQueryDto query);

    /// <summary>Các khoản thanh toán chủ nhà nhận được</summary>
    Task<PagedResult<PaymentDto>> GetReceivedAsync(PaymentQueryDto query);

    Task<PaymentDto> UpdateStatusAsync(int id, PaymentStatus status);
}
