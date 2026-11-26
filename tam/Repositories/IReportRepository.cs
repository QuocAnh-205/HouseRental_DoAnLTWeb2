using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Reports;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Repositories;

public interface IReportRepository : IRepository<Report>
{
    /// <summary>Lấy báo cáo kèm nhà và người báo cáo (có tracking)</summary>
    Task<Report?> GetDetailAsync(int id);

    Task<PagedResult<Report>> GetPagedAsync(ReportQueryDto query, int? reporterId = null);
    Task<bool> HasPendingAsync(int reporterId, int propertyId);
}
