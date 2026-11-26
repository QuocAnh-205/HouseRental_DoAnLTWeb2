using System.ComponentModel.DataAnnotations;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.DTOs.Reports;

public record ReportDto(
    int Id,
    int PropertyId,
    string PropertyTitle,
    int ReporterId,
    string ReporterName,
    ReportReason Reason,
    string? Description,
    ReportStatus Status,
    string? AdminNote,
    DateTime? HandledAt,
    DateTime CreatedAt);

public class CreateReportDto
{
    [Required(ErrorMessage = "Lý do báo cáo là bắt buộc")]
    [EnumDataType(typeof(ReportReason))]
    public ReportReason? Reason { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }
}

public class HandleReportDto
{
    /// <summary>Resolved hoặc Dismissed</summary>
    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    [EnumDataType(typeof(ReportStatus))]
    public ReportStatus? Status { get; set; }

    [StringLength(500)]
    public string? AdminNote { get; set; }

    /// <summary>Ẩn tin đăng bị báo cáo (chỉ khi Resolved)</summary>
    public bool HideProperty { get; set; }
}

public class ReportQueryDto : PaginationQuery
{
    /// <summary>Lọc theo trạng thái xử lý</summary>
    public ReportStatus? Status { get; set; }

    /// <summary>Lọc theo tin đăng bị báo cáo</summary>
    public int? PropertyId { get; set; }

    /// <summary>Lọc theo lý do báo cáo</summary>
    public ReportReason? Reason { get; set; }

    /// <summary>Tìm theo tiêu đề tin đăng, mô tả báo cáo, tên / email người báo cáo</summary>
    public string? Keyword { get; set; }

    /// <summary>newest (mặc định) | oldest</summary>
    public string? SortBy { get; set; }
}
