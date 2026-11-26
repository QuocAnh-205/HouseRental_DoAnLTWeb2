using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Properties;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Services;

public interface IPropertyService
{
    /// <summary>Tìm kiếm công khai, chỉ trả về nhà đang cho thuê (Available)</summary>
    Task<PagedResult<PropertySummaryDto>> SearchAsync(PropertyQueryDto query);

    /// <summary>Danh sách nhà của chủ nhà đang đăng nhập</summary>
    Task<PagedResult<PropertySummaryDto>> GetMineAsync(PropertyQueryDto query);

    /// <summary>Admin xem toàn bộ nhà, lọc theo mọi trạng thái</summary>
    Task<PagedResult<PropertySummaryDto>> GetAllForAdminAsync(PropertyQueryDto query);

    /// <summary>Tin đang cho thuê của một chủ nhà (trang hồ sơ công khai)</summary>
    Task<PagedResult<PropertySummaryDto>> GetByLandlordAsync(int landlordId, PropertyQueryDto query);

    /// <summary>Bản tin chung: tin đang cho thuê của mọi chủ nhà, tin mới được duyệt lên đầu</summary>
    Task<PagedResult<PropertyPostDto>> GetFeedAsync(PropertyType? type, PaginationQuery query);

    /// <summary>
    /// "Bản tin" trên trang cá nhân: các tin người dùng đã đăng, mới nhất trước.
    /// Chính chủ / admin thấy mọi trạng thái (kể cả chờ duyệt, bị từ chối); người khác chỉ thấy tin đã duyệt.
    /// </summary>
    Task<PagedResult<PropertyPostDto>> GetPostsAsync(int userId, PaginationQuery query);

    /// <summary>Chi tiết nhà, tự tăng lượt xem nếu người xem không phải chủ nhà / admin</summary>
    Task<PropertyDetailDto> GetByIdAsync(int id);

    Task<IReadOnlyList<PropertySummaryDto>> GetSimilarAsync(int id, int count);

    Task<PropertyStatsDto> GetStatsAsync(string? city, PropertyType? type);
    Task<PropertyDetailDto> CreateAsync(CreatePropertyDto dto);
    Task<PropertyDetailDto> UpdateAsync(int id, UpdatePropertyDto dto);
    Task DeleteAsync(int id);
    Task<PropertyDetailDto> UpdateStatusAsync(int id, PropertyStatus status, string? reason);
    Task<PropertyImageDto> AddImageAsync(int id, AddPropertyImageDto dto);
    Task RemoveImageAsync(int id, int imageId);
    Task SetThumbnailAsync(int id, int imageId);
}
