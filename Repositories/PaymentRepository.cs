using HouseRentalAPI.Data;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Payments;
using HouseRentalAPI.Extensions;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Repositories;

public class PaymentRepository(AppDbContext context) : Repository<Payment>(context), IPaymentRepository
{
    public Task<Payment?> GetDetailAsync(int id) =>
        DbSet
            .Include(p => p.Booking)
            .ThenInclude(b => b.Property)
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<Payment>> GetByBookingAsync(int bookingId) =>
        await DbSet
            .AsNoTracking()
            .Include(p => p.Booking)
            .ThenInclude(b => b.Property)
            .Where(p => p.BookingId == bookingId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public Task<PagedResult<Payment>> GetByTenantAsync(int tenantId, PaymentQueryDto query) =>
        Filter(DbSet.Where(p => p.Booking.TenantId == tenantId), query)
            .ToPagedResultAsync(query.Page, query.PageSize);

    public Task<PagedResult<Payment>> GetByLandlordAsync(int landlordId, PaymentQueryDto query) =>
        Filter(DbSet.Where(p => p.Booking.Property.LandlordId == landlordId), query)
            .ToPagedResultAsync(query.Page, query.PageSize);

    private static IQueryable<Payment> Filter(IQueryable<Payment> payments, PaymentQueryDto query)
    {
        if (query.Status.HasValue)
            payments = payments.Where(p => p.Status == query.Status.Value);

        if (query.Type.HasValue)
            payments = payments.Where(p => p.Type == query.Type.Value);

        return payments
            .AsNoTracking()
            .Include(p => p.Booking)
            .ThenInclude(b => b.Property)
            .OrderByDescending(p => p.CreatedAt);
    }
}
