using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Invoices;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Repositories;

public interface IInvoiceRepository : IRepository<Invoice>
{
    /// <summary>Lấy hóa đơn kèm hợp đồng, nhà, người thuê (có tracking)</summary>
    Task<Invoice?> GetDetailAsync(int id);

    Task<IReadOnlyList<Invoice>> GetByBookingAsync(int bookingId);
    Task<PagedResult<Invoice>> GetByTenantAsync(int tenantId, InvoiceQueryDto query);
    Task<PagedResult<Invoice>> GetByLandlordAsync(int landlordId, InvoiceQueryDto query);
    Task<bool> ExistsAsync(int bookingId, int year, int month);

    /// <summary>Hóa đơn gần nhất của hợp đồng, dùng để gợi ý chỉ số điện nước cũ</summary>
    Task<Invoice?> GetLatestAsync(int bookingId);
}
