using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Payments;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    /// <summary>Lấy thanh toán kèm hợp đồng, nhà, hóa đơn (có tracking)</summary>
    Task<Payment?> GetDetailAsync(int id);

    Task<IReadOnlyList<Payment>> GetByBookingAsync(int bookingId);
    Task<PagedResult<Payment>> GetByTenantAsync(int tenantId, PaymentQueryDto query);
    Task<PagedResult<Payment>> GetByLandlordAsync(int landlordId, PaymentQueryDto query);
}
