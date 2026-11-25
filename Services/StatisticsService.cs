using HouseRentalAPI.Data;
using HouseRentalAPI.DTOs.Statistics;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Services;

/// <summary>
/// Số liệu tổng hợp chỉ đọc, truy vấn thẳng DbContext vì trải trên nhiều bảng
/// và không thuộc riêng repository nào.
/// </summary>
public class StatisticsService(AppDbContext context, ICurrentUserService currentUser) : IStatisticsService
{
    private const int RevenueMonths = 12;

    public async Task<AdminStatisticsDto> GetAdminAsync()
    {
        var usersByRole = await CountByAsync(context.Users, u => u.Role);
        var propertiesByStatus = await CountByAsync(context.Properties, p => p.Status);
        var bookingsByStatus = await CountByAsync(context.Bookings, b => b.Status);

        var successPayments = context.Payments.Where(p => p.Status == PaymentStatus.Success);

        return new AdminStatisticsDto(
            usersByRole.Values.Sum(),
            usersByRole,
            await context.Users.CountAsync(u => !u.IsActive),
            propertiesByStatus.Values.Sum(),
            propertiesByStatus,
            bookingsByStatus.Values.Sum(),
            bookingsByStatus,
            await context.Reports.CountAsync(r => r.Status == ReportStatus.Pending),
            await successPayments.SumAsync(p => (decimal?)p.Amount) ?? 0,
            await RevenueByMonthAsync(successPayments));
    }

    public async Task<LandlordStatisticsDto> GetLandlordAsync()
    {
        var landlordId = currentUser.GetRequiredUserId();

        var properties = context.Properties.Where(p => p.LandlordId == landlordId);
        var propertiesByStatus = await CountByAsync(properties, p => p.Status);

        var ratingStats = await properties
            .Where(p => p.ReviewCount > 0)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Reviews = g.Sum(p => p.ReviewCount),
                WeightedRating = g.Sum(p => p.AverageRating * p.ReviewCount)
            })
            .FirstOrDefaultAsync();

        var bookings = context.Bookings.Where(b => b.Property.LandlordId == landlordId);
        var openInvoices = context.Invoices.Where(i =>
            i.Booking.Property.LandlordId == landlordId &&
            (i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue));
        var successPayments = context.Payments.Where(p =>
            p.Booking.Property.LandlordId == landlordId && p.Status == PaymentStatus.Success);

        return new LandlordStatisticsDto(
            propertiesByStatus.Values.Sum(),
            propertiesByStatus,
            await properties.SumAsync(p => (int?)p.ViewCount) ?? 0,
            ratingStats is null ? 0 : Math.Round(ratingStats.WeightedRating / ratingStats.Reviews, 1),
            ratingStats?.Reviews ?? 0,
            await bookings.CountAsync(b => b.Status == BookingStatus.Pending),
            await bookings.CountAsync(b => b.Status == BookingStatus.Approved),
            await context.Appointments.CountAsync(a => a.Property.LandlordId == landlordId && a.Status == AppointmentStatus.Pending),
            await openInvoices.CountAsync(),
            await openInvoices.SumAsync(i => (decimal?)(i.TotalAmount - i.PaidAmount)) ?? 0,
            await successPayments.SumAsync(p => (decimal?)p.Amount) ?? 0,
            await RevenueByMonthAsync(successPayments));
    }

    public async Task<TenantStatisticsDto> GetTenantAsync()
    {
        var tenantId = currentUser.GetRequiredUserId();

        var bookings = context.Bookings.Where(b => b.TenantId == tenantId);
        var openInvoices = context.Invoices.Where(i =>
            i.Booking.TenantId == tenantId &&
            (i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue));

        return new TenantStatisticsDto(
            await bookings.CountAsync(b => b.Status == BookingStatus.Approved),
            await bookings.CountAsync(b => b.Status == BookingStatus.Pending),
            await context.Appointments.CountAsync(a =>
                a.TenantId == tenantId &&
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) &&
                a.ScheduledAt > DateTime.UtcNow),
            await context.Favorites.CountAsync(f => f.UserId == tenantId),
            await openInvoices.CountAsync(),
            await openInvoices.SumAsync(i => (decimal?)(i.TotalAmount - i.PaidAmount)) ?? 0,
            await context.Payments
                .Where(p => p.Booking.TenantId == tenantId && p.Status == PaymentStatus.Success)
                .SumAsync(p => (decimal?)p.Amount) ?? 0);
    }

    private static async Task<IReadOnlyDictionary<string, int>> CountByAsync<T, TKey>(
        IQueryable<T> source, System.Linq.Expressions.Expression<Func<T, TKey>> keySelector)
        where TKey : struct, Enum
    {
        var counts = await source
            .GroupBy(keySelector)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        // Luôn trả đủ mọi giá trị enum, kể cả bằng 0, để client hiển thị ổn định
        return Enum.GetValues<TKey>().ToDictionary(
            k => k.ToString(),
            k => counts.FirstOrDefault(c => c.Key.Equals(k))?.Count ?? 0);
    }

    private static async Task<IReadOnlyList<MonthlyRevenueDto>> RevenueByMonthAsync(IQueryable<Payment> successPayments)
    {
        var now = DateTime.UtcNow;
        var from = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(RevenueMonths - 1));

        var rows = await successPayments
            .Where(p => p.PaidAt >= from)
            .GroupBy(p => new { p.PaidAt!.Value.Year, p.PaidAt!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(p => p.Amount) })
            .ToListAsync();

        return Enumerable.Range(0, RevenueMonths)
            .Select(i => from.AddMonths(i))
            .Select(m => new MonthlyRevenueDto(
                m.Year,
                m.Month,
                rows.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month)?.Revenue ?? 0))
            .ToList();
    }
}
