using HouseRentalAPI.Data;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Properties;
using HouseRentalAPI.Extensions;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Repositories;

public class PropertyRepository(AppDbContext context) : Repository<Property>(context), IPropertyRepository
{
    public Task<Property?> GetDetailAsync(int id) =>
        DbSet
            .Include(p => p.Landlord)
            .Include(p => p.Images)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<PropertyStatsDto> GetStatsAsync(string? city, PropertyType? type)
    {
        var available = DbSet.AsNoTracking().Where(p => p.Status == PropertyStatus.Available);
        if (!string.IsNullOrWhiteSpace(city))
            available = available.Where(p => p.City.Contains(city.Trim()));

        var byType = await available
            .GroupBy(p => p.Type)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var inType = type.HasValue ? available.Where(p => p.Type == type.Value) : available;
        var byDistrict = await inType
            .GroupBy(p => p.District)
            .Select(g => new { g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        return new PropertyStatsDto(
            byDistrict.Sum(x => x.Count),
            byType.OrderByDescending(x => x.Count).Select(x => new CountItemDto(x.Key.ToString(), x.Count)).ToList(),
            byDistrict.Select(x => new CountItemDto(x.Key, x.Count)).ToList());
    }

    public async Task<PagedResult<Property>> SearchAsync(PropertyQueryDto query, int? landlordId = null)
    {
        // Lọc + sắp xếp trên bảng Properties (chưa Include) để chỉ lấy Id của trang cần hiển thị
        var properties = DbSet.AsNoTracking().AsQueryable();

        if (landlordId.HasValue)
            properties = properties.Where(p => p.LandlordId == landlordId.Value);

        if (query.Status.HasValue)
            properties = properties.Where(p => p.Status == query.Status.Value);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            properties = properties.Where(p =>
                p.Title.Contains(keyword) ||
                p.Description.Contains(keyword) ||
                p.Address.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(query.City))
            properties = properties.Where(p => p.City.Contains(query.City.Trim()));

        if (!string.IsNullOrWhiteSpace(query.District))
            properties = properties.Where(p => p.District.Contains(query.District.Trim()));

        if (query.Type.HasValue)
            properties = properties.Where(p => p.Type == query.Type.Value);

        if (query.MinPrice.HasValue)
            properties = properties.Where(p => p.PricePerMonth >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            properties = properties.Where(p => p.PricePerMonth <= query.MaxPrice.Value);

        if (query.MinArea.HasValue)
            properties = properties.Where(p => p.Area >= query.MinArea.Value);

        if (query.MaxArea.HasValue)
            properties = properties.Where(p => p.Area <= query.MaxArea.Value);

        if (query.MinBedrooms.HasValue)
            properties = properties.Where(p => p.Bedrooms >= query.MinBedrooms.Value);

        if (query.MinBathrooms.HasValue)
            properties = properties.Where(p => p.Bathrooms >= query.MinBathrooms.Value);

        if (!string.IsNullOrWhiteSpace(query.Ward))
            properties = properties.Where(p => p.Ward != null && p.Ward.Contains(query.Ward.Trim()));

        foreach (var amenity in query.Amenities?.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()) ?? [])
            properties = properties.Where(p => p.Amenities.Contains(amenity));

        var hasLocation = query is { Latitude: not null, Longitude: not null, RadiusKm: not null };
        double lat = 0, lng = 0, lngScale = 1;
        if (hasLocation)
        {
            // Lọc theo khung vuông bao quanh bán kính (1 độ vĩ ≈ 111,32 km)
            lat = query.Latitude!.Value;
            lng = query.Longitude!.Value;
            lngScale = Math.Cos(lat * Math.PI / 180);
            var latDelta = query.RadiusKm!.Value / 111.32;
            var lngDelta = query.RadiusKm.Value / (111.32 * Math.Max(lngScale, 0.01));

            properties = properties.Where(p =>
                p.Latitude != null && p.Longitude != null &&
                p.Latitude >= lat - latDelta && p.Latitude <= lat + latDelta &&
                p.Longitude >= lng - lngDelta && p.Longitude <= lng + lngDelta);
        }

        properties = query.SortBy?.ToLowerInvariant() switch
        {
            "distance" when hasLocation => properties.OrderBy(p =>
                (p.Latitude!.Value - lat) * (p.Latitude!.Value - lat) +
                (p.Longitude!.Value - lng) * lngScale * ((p.Longitude!.Value - lng) * lngScale)),
            "most_viewed" => properties.OrderByDescending(p => p.ViewCount),
            "price_asc" => properties.OrderBy(p => p.PricePerMonth),
            "price_desc" => properties.OrderByDescending(p => p.PricePerMonth),
            "area_desc" => properties.OrderByDescending(p => p.Area),
            "rating_desc" => properties.OrderByDescending(p => p.AverageRating).ThenByDescending(p => p.ReviewCount),
            _ => properties.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await properties.CountAsync();
        var ids = await properties
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => p.Id)
            .ToListAsync();

        return new PagedResult<Property>
        {
            Items = await LoadInOrderAsync(ids),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Tải đầy đủ (chủ nhà, ảnh) cho danh sách Id và giữ nguyên thứ tự.
    /// Tách làm 2 bước để SQL Server chỉ phải sắp xếp cột khóa thay vì cả dòng có cột Description dài,
    /// tránh xin nhiều bộ nhớ cho mỗi truy vấn (RESOURCE_SEMAPHORE) khi máy chủ thiếu RAM.
    /// </summary>
    private async Task<IReadOnlyList<Property>> LoadInOrderAsync(IReadOnlyCollection<int> ids)
    {
        if (ids.Count == 0)
            return [];

        // 3 truy vấn tra cứu theo khóa, không JOIN / ORDER BY nên không cần cấp thêm bộ nhớ sắp xếp
        var loaded = await DbSet.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var images = await Context.PropertyImages.AsNoTracking()
            .Where(i => ids.Contains(i.PropertyId))
            .ToListAsync();

        var landlordIds = loaded.Values.Select(p => p.LandlordId).Distinct().ToList();
        var landlords = await Context.Users.AsNoTracking()
            .Where(u => landlordIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        foreach (var group in images.GroupBy(i => i.PropertyId))
            loaded[group.Key].Images = group.OrderBy(i => i.Id).ToList();
        foreach (var property in loaded.Values)
            property.Landlord = landlords.GetValueOrDefault(property.LandlordId)!;

        return ids.Where(loaded.ContainsKey).Select(id => loaded[id]).ToList();
    }

    public async Task<PagedResult<Property>> GetFeedAsync(PropertyType? type, int page, int pageSize)
    {
        var feed = DbSet.AsNoTracking().Where(p => p.Status == PropertyStatus.Available);
        if (type.HasValue)
            feed = feed.Where(p => p.Type == type.Value);

        var totalCount = await feed.CountAsync();
        var ids = await feed
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => p.Id)
            .ToListAsync();

        return new PagedResult<Property>
        {
            Items = await LoadInOrderAsync(ids),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResult<Property>> GetPostsByLandlordAsync(
        int landlordId, IReadOnlyCollection<PropertyStatus>? statuses, int page, int pageSize)
    {
        var posts = DbSet.AsNoTracking().Where(p => p.LandlordId == landlordId);
        if (statuses is { Count: > 0 })
            posts = posts.Where(p => statuses.Contains(p.Status));

        var totalCount = await posts.CountAsync();
        var ids = await posts
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => p.Id)
            .ToListAsync();

        return new PagedResult<Property>
        {
            Items = await LoadInOrderAsync(ids),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public Task IncrementViewCountAsync(int id) =>
        DbSet.Where(p => p.Id == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1));

    public async Task<IReadOnlyList<Property>> GetSimilarAsync(Property property, int count)
    {
        var minPrice = property.PricePerMonth * 0.7m;
        var maxPrice = property.PricePerMonth * 1.3m;

        var ids = await DbSet
            .AsNoTracking()
            .Where(p =>
                p.Id != property.Id &&
                p.Status == PropertyStatus.Available &&
                p.City == property.City &&
                p.Type == property.Type &&
                p.PricePerMonth >= minPrice &&
                p.PricePerMonth <= maxPrice)
            .OrderByDescending(p => p.District == property.District)
            .ThenByDescending(p => p.AverageRating)
            .ThenByDescending(p => p.CreatedAt)
            .Take(count)
            .Select(p => p.Id)
            .ToListAsync();

        return await LoadInOrderAsync(ids);
    }
}
