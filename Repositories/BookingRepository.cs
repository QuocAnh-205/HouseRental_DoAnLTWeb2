using HouseRentalAPI.Data;
using HouseRentalAPI.DTOs.Bookings;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Extensions;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Repositories;

public class BookingRepository(AppDbContext context) : Repository<Booking>(context), IBookingRepository
{
    public Task<Booking?> GetDetailAsync(int id) =>
        DbSet
            .Include(b => b.Property)
            .Include(b => b.Tenant)
            .FirstOrDefaultAsync(b => b.Id == id);

    public Task<PagedResult<Booking>> GetByTenantAsync(int tenantId, BookingQueryDto query) =>
        Filter(DbSet.Where(b => b.TenantId == tenantId), query)
            .ToPagedResultAsync(query.Page, query.PageSize);

    public Task<PagedResult<Booking>> GetByLandlordAsync(int landlordId, BookingQueryDto query) =>
        Filter(DbSet.Where(b => b.Property.LandlordId == landlordId), query)
            .ToPagedResultAsync(query.Page, query.PageSize);

    public Task<PagedResult<Booking>> GetPagedAsync(BookingQueryDto query) =>
        Filter(DbSet, query)
            .ToPagedResultAsync(query.Page, query.PageSize);

    public async Task<IReadOnlyList<Booking>> GetPendingByPropertyAsync(int propertyId, int excludeBookingId) =>
        await DbSet
            .Where(b => b.PropertyId == propertyId && b.Id != excludeBookingId && b.Status == BookingStatus.Pending)
            .ToListAsync();

    public Task<bool> HasOverlapAsync(int propertyId, DateOnly startDate, DateOnly endDate, int? excludeBookingId = null) =>
        DbSet.AnyAsync(b =>
            b.PropertyId == propertyId &&
            b.Status == BookingStatus.Approved &&
            (excludeBookingId == null || b.Id != excludeBookingId) &&
            b.StartDate < endDate &&
            startDate < b.EndDate);

    public Task<bool> HasPendingRequestAsync(int tenantId, int propertyId) =>
        DbSet.AnyAsync(b => b.TenantId == tenantId && b.PropertyId == propertyId && b.Status == BookingStatus.Pending);

    public Task<bool> HasActiveContractAsync(int propertyId) =>
        DbSet.AnyAsync(b => b.PropertyId == propertyId && b.Status == BookingStatus.Approved);

    public Task<bool> HasRentedAsync(int tenantId, int propertyId) =>
        DbSet.AnyAsync(b =>
            b.TenantId == tenantId &&
            b.PropertyId == propertyId &&
            (b.Status == BookingStatus.Approved || b.Status == BookingStatus.Completed));

    /// <summary>Filtering (trạng thái) + Searching (từ khóa) + Sorting (SortBy) dùng chung cho mọi danh sách hợp đồng</summary>
    private static IQueryable<Booking> Filter(IQueryable<Booking> bookings, BookingQueryDto query)
    {
        if (query.Status.HasValue)
            bookings = bookings.Where(b => b.Status == query.Status.Value);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            bookings = bookings.Where(b =>
                b.Property.Title.Contains(keyword) ||
                b.Property.Address.Contains(keyword) ||
                b.Tenant.FullName.Contains(keyword) ||
                b.Tenant.Email.Contains(keyword) ||
                (b.Tenant.PhoneNumber != null && b.Tenant.PhoneNumber.Contains(keyword)));
        }

        bookings = query.SortBy?.ToLowerInvariant() switch
        {
            "oldest" => bookings.OrderBy(b => b.CreatedAt),
            "start_date" => bookings.OrderBy(b => b.StartDate),
            "start_date_desc" => bookings.OrderByDescending(b => b.StartDate),
            "price_asc" => bookings.OrderBy(b => b.MonthlyPrice),
            "price_desc" => bookings.OrderByDescending(b => b.MonthlyPrice),
            _ => bookings.OrderByDescending(b => b.CreatedAt)
        };

        return bookings
            .AsNoTracking()
            .Include(b => b.Property)
            .Include(b => b.Tenant);
    }
}
