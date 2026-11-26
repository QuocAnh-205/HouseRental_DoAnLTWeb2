using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Reports;

namespace HouseRentalAPI.Services;

public interface IReportService
{
    Task<ReportDto> CreateAsync(int propertyId, CreateReportDto dto);

    /// <summary>Báo cáo do người dùng hiện tại gửi</summary>
    Task<PagedResult<ReportDto>> GetMineAsync(ReportQueryDto query);

    /// <summary>Admin xem toàn bộ báo cáo</summary>
    Task<PagedResult<ReportDto>> GetAllAsync(ReportQueryDto query);

    Task<ReportDto> HandleAsync(int id, HandleReportDto dto);
}
