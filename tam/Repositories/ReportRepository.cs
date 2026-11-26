using HouseRentalAPI.Data;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Reports;
using HouseRentalAPI.Extensions;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Repositories;

public class ReportRepository(AppDbContext context) : Repository<Report>(context), IReportRepository
{
    public Task<Report?> GetDetailAsync(int id) =>
        DbSet
            .Include(r => r.Property)
            .Include(r => r.Reporter)
            .FirstOrDefaultAsync(r => r.Id == id);

    public Task<PagedResult<Report>> GetPagedAsync(ReportQueryDto query, int? reporterId = null)
    {
        var reports = DbSet.AsNoTracking();

        if (reporterId.HasValue)
            reports = reports.Where(r => r.ReporterId == reporterId.Value);

        if (query.Status.HasValue)
            reports = reports.Where(r => r.Status == query.Status.Value);

        if (query.PropertyId.HasValue)
            reports = reports.Where(r => r.PropertyId == query.PropertyId.Value);

        if (query.Reason.HasValue)
            reports = reports.Where(r => r.Reason == query.Reason.Value);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            reports = reports.Where(r =>
                r.Property.Title.Contains(keyword) ||
                (r.Description != null && r.Description.Contains(keyword)) ||
                r.Reporter.FullName.Contains(keyword) ||
                r.Reporter.Email.Contains(keyword));
        }

        reports = query.SortBy?.ToLowerInvariant() == "oldest"
            ? reports.OrderBy(r => r.CreatedAt)
            : reports.OrderByDescending(r => r.CreatedAt);

        return reports
            .Include(r => r.Property)
            .Include(r => r.Reporter)
            .ToPagedResultAsync(query.Page, query.PageSize);
    }

    public Task<bool> HasPendingAsync(int reporterId, int propertyId) =>
        DbSet.AnyAsync(r => r.ReporterId == reporterId && r.PropertyId == propertyId && r.Status == ReportStatus.Pending);
}
