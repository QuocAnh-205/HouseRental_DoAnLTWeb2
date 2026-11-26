using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Reports;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Mappings;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;

namespace HouseRentalAPI.Services;

public class ReportService(
    IReportRepository reportRepository,
    IPropertyRepository propertyRepository,
    INotificationService notificationService,
    ICurrentUserService currentUser) : IReportService
{
    public async Task<ReportDto> CreateAsync(int propertyId, CreateReportDto dto)
    {
        var reporterId = currentUser.GetRequiredUserId();

        var property = await propertyRepository.GetByIdAsync(propertyId)
            ?? throw new NotFoundException($"Không tìm thấy nhà có id {propertyId}");

        if (property.LandlordId == reporterId)
            throw new BadRequestException("Không thể báo cáo tin đăng của chính mình");

        if (await reportRepository.HasPendingAsync(reporterId, propertyId))
            throw new ConflictException("Bạn đã báo cáo tin này và đang chờ xử lý");

        var report = new Report
        {
            PropertyId = propertyId,
            ReporterId = reporterId,
            Reason = dto.Reason!.Value,
            Description = dto.Description?.Trim()
        };

        await reportRepository.AddAsync(report);
        await reportRepository.SaveChangesAsync();

        await notificationService.NotifyAdminsAsync(NotificationType.Report,
            "Có báo cáo vi phạm mới",
            $"Tin \"{property.Title}\" bị báo cáo: {report.Reason}.",
            $"/api/reports?propertyId={propertyId}");
        await reportRepository.SaveChangesAsync();

        return (await reportRepository.GetDetailAsync(report.Id))!.ToDto();
    }

    public async Task<PagedResult<ReportDto>> GetMineAsync(ReportQueryDto query)
    {
        var result = await reportRepository.GetPagedAsync(query, currentUser.GetRequiredUserId());
        return result.Map(r => r.ToDto());
    }

    public async Task<PagedResult<ReportDto>> GetAllAsync(ReportQueryDto query)
    {
        var result = await reportRepository.GetPagedAsync(query);
        return result.Map(r => r.ToDto());
    }

    public async Task<ReportDto> HandleAsync(int id, HandleReportDto dto)
    {
        var report = await reportRepository.GetDetailAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy báo cáo có id {id}");

        if (report.Status != ReportStatus.Pending)
            throw new BadRequestException("Báo cáo này đã được xử lý");

        var status = dto.Status!.Value;
        if (status == ReportStatus.Pending)
            throw new BadRequestException("Trạng thái xử lý chỉ được là Resolved hoặc Dismissed");

        report.Status = status;
        report.AdminNote = dto.AdminNote?.Trim();
        report.HandledAt = DateTime.UtcNow;

        if (status == ReportStatus.Resolved && dto.HideProperty && report.Property.Status != PropertyStatus.Hidden)
        {
            report.Property.Status = PropertyStatus.Hidden;
            await notificationService.NotifyAsync(report.Property.LandlordId, NotificationType.Property,
                "Tin đăng bị ẩn do vi phạm",
                $"Tin \"{report.Property.Title}\" đã bị ẩn sau khi xác minh báo cáo vi phạm. {report.AdminNote}".TrimEnd(),
                $"/api/properties/{report.PropertyId}");
        }

        await notificationService.NotifyAsync(report.ReporterId, NotificationType.Report,
            "Báo cáo của bạn đã được xử lý",
            status == ReportStatus.Resolved
                ? $"Cảm ơn bạn! Báo cáo về tin \"{report.Property.Title}\" đã được xác minh và xử lý."
                : $"Báo cáo về tin \"{report.Property.Title}\" đã được xem xét nhưng chưa phát hiện vi phạm.",
            $"/api/reports/mine");

        await reportRepository.SaveChangesAsync();
        return report.ToDto();
    }
}
