using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Repositories;

public interface IReviewRepository : IRepository<Review>
{
    /// <summary>Lấy đánh giá kèm người viết và căn nhà (có tracking)</summary>
    Task<Review?> GetDetailAsync(int id);
    Task<PagedResult<Review>> GetByPropertyAsync(int propertyId, PaginationQuery query);
    Task<bool> ExistsAsync(int tenantId, int propertyId);

    /// <summary>Đánh giá của một người thuê cho một căn nhà (kèm người viết)</summary>
    Task<Review?> GetByTenantAsync(int tenantId, int propertyId);
    Task<(double Average, int Count)> GetStatsAsync(int propertyId);
}
