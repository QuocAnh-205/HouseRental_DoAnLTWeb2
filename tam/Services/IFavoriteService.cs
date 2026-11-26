using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Properties;

namespace HouseRentalAPI.Services;

public interface IFavoriteService
{
    Task<PagedResult<PropertySummaryDto>> GetMineAsync(PaginationQuery query);
    Task AddAsync(int propertyId);
    Task RemoveAsync(int propertyId);
}
