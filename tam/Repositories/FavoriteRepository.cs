using HouseRentalAPI.Data;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Extensions;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Repositories;

public class FavoriteRepository(AppDbContext context) : Repository<Favorite>(context), IFavoriteRepository
{
    public Task<Favorite?> GetAsync(int userId, int propertyId) =>
        DbSet.FirstOrDefaultAsync(f => f.UserId == userId && f.PropertyId == propertyId);

    public Task<bool> ExistsAsync(int userId, int propertyId) =>
        DbSet.AnyAsync(f => f.UserId == userId && f.PropertyId == propertyId);

    public Task<PagedResult<Property>> GetPropertiesAsync(int userId, PaginationQuery query) =>
        Context.Properties
            .AsNoTracking()
            .Include(p => p.Landlord)
            .Include(p => p.Images)
            .AsSplitQuery()
            .Where(p => p.Favorites.Any(f => f.UserId == userId))
            .OrderByDescending(p => p.Favorites.Where(f => f.UserId == userId).Max(f => f.CreatedAt))
            .ToPagedResultAsync(query.Page, query.PageSize);
}
