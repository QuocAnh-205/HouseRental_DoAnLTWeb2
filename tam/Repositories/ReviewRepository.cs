using HouseRentalAPI.Data;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Extensions;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Repositories;

public class ReviewRepository(AppDbContext context) : Repository<Review>(context), IReviewRepository
{
    public Task<Review?> GetDetailAsync(int id) =>
        DbSet.Include(r => r.Tenant).Include(r => r.Property).FirstOrDefaultAsync(r => r.Id == id);

    public Task<PagedResult<Review>> GetByPropertyAsync(int propertyId, PaginationQuery query) =>
        DbSet
            .AsNoTracking()
            .Include(r => r.Tenant)
            .Where(r => r.PropertyId == propertyId)
            .OrderByDescending(r => r.CreatedAt)
            .ToPagedResultAsync(query.Page, query.PageSize);

    public Task<Review?> GetByTenantAsync(int tenantId, int propertyId) =>
        DbSet
            .AsNoTracking()
            .Include(r => r.Tenant)
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.PropertyId == propertyId);

    public Task<bool> ExistsAsync(int tenantId, int propertyId) =>
        DbSet.AnyAsync(r => r.TenantId == tenantId && r.PropertyId == propertyId);

    public async Task<(double Average, int Count)> GetStatsAsync(int propertyId)
    {
        var stats = await DbSet
            .Where(r => r.PropertyId == propertyId)
            .GroupBy(_ => 1)
            .Select(g => new { Average = g.Average(r => (double)r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync();

        return stats is null ? (0, 0) : (stats.Average, stats.Count);
    }
}
