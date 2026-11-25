using HouseRentalAPI.DTOs.Statistics;

namespace HouseRentalAPI.Services;

public interface IStatisticsService
{
    Task<AdminStatisticsDto> GetAdminAsync();
    Task<LandlordStatisticsDto> GetLandlordAsync();
    Task<TenantStatisticsDto> GetTenantAsync();
}
