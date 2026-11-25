using HouseRentalAPI.DTOs.Bookings;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Repositories;

public interface IBookingRepository : IRepository<Booking>
{
    /// <summary>Lấy booking kèm nhà và người thuê (có tracking để cập nhật)</summary>
    Task<Booking?> GetDetailAsync(int id);

    Task<PagedResult<Booking>> GetByTenantAsync(int tenantId, BookingQueryDto query);
    Task<PagedResult<Booking>> GetByLandlordAsync(int landlordId, BookingQueryDto query);
    Task<PagedResult<Booking>> GetPagedAsync(BookingQueryDto query);
    Task<IReadOnlyList<Booking>> GetPendingByPropertyAsync(int propertyId, int excludeBookingId);

    /// <summary>Kiểm tra trùng lịch với các hợp đồng đã duyệt</summary>
    Task<bool> HasOverlapAsync(int propertyId, DateOnly startDate, DateOnly endDate, int? excludeBookingId = null);

    Task<bool> HasPendingRequestAsync(int tenantId, int propertyId);

    /// <summary>Người thuê đã từng thuê nhà này (Approved hoặc Completed)</summary>
    Task<bool> HasRentedAsync(int tenantId, int propertyId);

    /// <summary>Nhà đang có hợp đồng còn hiệu lực (Approved)</summary>
    Task<bool> HasActiveContractAsync(int propertyId);
}
