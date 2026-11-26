using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Repositories;

public interface IFavoriteRepository : IRepository<Favorite>
{
    Task<Favorite?> GetAsync(int userId, int propertyId);
    Task<bool> ExistsAsync(int userId, int propertyId);

    /// <summary>Danh sách nhà yêu thích kèm ảnh và chủ nhà, mới lưu trước</summary>
    Task<PagedResult<Property>> GetPropertiesAsync(int userId, PaginationQuery query);
}
