using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Properties;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Mappings;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;

namespace HouseRentalAPI.Services;

public class FavoriteService(
    IFavoriteRepository favoriteRepository,
    IPropertyRepository propertyRepository,
    ICurrentUserService currentUser) : IFavoriteService
{
    public async Task<PagedResult<PropertySummaryDto>> GetMineAsync(PaginationQuery query)
    {
        var result = await favoriteRepository.GetPropertiesAsync(currentUser.GetRequiredUserId(), query);
        return result.Map(p => p.ToSummaryDto());
    }

    public async Task AddAsync(int propertyId)
    {
        var userId = currentUser.GetRequiredUserId();

        var visible = await propertyRepository.AnyAsync(p =>
            p.Id == propertyId && (p.Status == PropertyStatus.Available || p.Status == PropertyStatus.Rented));
        if (!visible)
            throw new NotFoundException($"Không tìm thấy nhà có id {propertyId}");

        if (await favoriteRepository.ExistsAsync(userId, propertyId))
            return;

        await favoriteRepository.AddAsync(new Favorite { UserId = userId, PropertyId = propertyId });
        await favoriteRepository.SaveChangesAsync();
    }

    public async Task RemoveAsync(int propertyId)
    {
        var favorite = await favoriteRepository.GetAsync(currentUser.GetRequiredUserId(), propertyId);
        if (favorite is null)
            return;

        favoriteRepository.Remove(favorite);
        await favoriteRepository.SaveChangesAsync();
    }
}
