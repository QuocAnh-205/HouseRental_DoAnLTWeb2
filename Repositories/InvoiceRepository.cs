using HouseRentalAPI.Data;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Invoices;
using HouseRentalAPI.Extensions;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Repositories;

public class InvoiceRepository(AppDbContext context) : Repository<Invoice>(context), IInvoiceRepository
{
    public Task<Invoice?> GetDetailAsync(int id) =>
        DbSet
            .Include(i => i.Booking).ThenInclude(b => b.Property)
            .Include(i => i.Booking).ThenInclude(b => b.Tenant)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<IReadOnlyList<Invoice>> GetByBookingAsync(int bookingId) =>
        await WithDetails(DbSet.Where(i => i.BookingId == bookingId)).ToListAsync();

    public Task<PagedResult<Invoice>> GetByTenantAsync(int tenantId, InvoiceQueryDto query) =>
        WithDetails(Filter(DbSet.Where(i => i.Booking.TenantId == tenantId), query))
            .ToPagedResultAsync(query.Page, query.PageSize);

    public Task<PagedResult<Invoice>> GetByLandlordAsync(int landlordId, InvoiceQueryDto query) =>
        WithDetails(Filter(DbSet.Where(i => i.Booking.Property.LandlordId == landlordId), query))
            .ToPagedResultAsync(query.Page, query.PageSize);

    public Task<bool> ExistsAsync(int bookingId, int year, int month) =>
        DbSet.AnyAsync(i => i.BookingId == bookingId && i.Year == year && i.Month == month && i.Status != InvoiceStatus.Cancelled);

    public Task<Invoice?> GetLatestAsync(int bookingId) =>
        DbSet
            .AsNoTracking()
            .Where(i => i.BookingId == bookingId && i.Status != InvoiceStatus.Cancelled)
            .OrderByDescending(i => i.Year).ThenByDescending(i => i.Month)
            .FirstOrDefaultAsync();

    private static IQueryable<Invoice> Filter(IQueryable<Invoice> invoices, InvoiceQueryDto query)
    {
        if (query.Status.HasValue)
            invoices = invoices.Where(i => i.Status == query.Status.Value);

        if (query.Year.HasValue)
            invoices = invoices.Where(i => i.Year == query.Year.Value);

        if (query.Month.HasValue)
            invoices = invoices.Where(i => i.Month == query.Month.Value);

        return invoices;
    }

    private static IQueryable<Invoice> WithDetails(IQueryable<Invoice> invoices) =>
        invoices
            .AsNoTracking()
            .Include(i => i.Booking).ThenInclude(b => b.Property)
            .Include(i => i.Booking).ThenInclude(b => b.Tenant)
            .OrderByDescending(i => i.Year)
            .ThenByDescending(i => i.Month);
}
