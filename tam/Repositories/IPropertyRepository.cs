using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Properties;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Repositories;

public interface IPropertyRepository : IRepository<Property>
{
    /// <summary>Lấy nhà kèm chủ nhà và ảnh (có tracking để cập nhật)</summary>
    Task<Property?> GetDetailAsync(int id);

    Task<PagedResult<Property>> SearchAsync(PropertyQueryDto query, int? landlordId = null);

    /// <summary>Bản tin chung: tin Available, sắp theo thời điểm được duyệt (mới nhất trước)</summary>
    Task<PagedResult<Property>> GetFeedAsync(PropertyType? type, int page, int pageSize);

    /// <summary>Tin của một chủ nhà, mới nhất trước (null = mọi trạng thái)</summary>
    Task<PagedResult<Property>> GetPostsByLandlordAsync(int landlordId, IReadOnlyCollection<PropertyStatus>? statuses, int page, int pageSize);

    /// <summary>Tăng lượt xem trực tiếp trong DB, không đụng tới change tracker</summary>
    Task IncrementViewCountAsync(int id);

    /// <summary>Nhà đang cho thuê cùng thành phố, cùng loại, giá chênh lệch không quá 30%</summary>
    Task<IReadOnlyList<Property>> GetSimilarAsync(Property property, int count);

    /// <summary>Đếm nhà đang cho thuê theo loại và theo quận (lọc tùy chọn theo thành phố / loại)</summary>
    Task<PropertyStatsDto> GetStatsAsync(string? city, PropertyType? type);
}
