using System.Text.Json.Serialization;

namespace HouseRentalAPI.DTOs.Common;

/// <summary>
/// Định dạng lỗi thống nhất của toàn bộ API: exception (qua ExceptionHandlingMiddleware),
/// lỗi validate dữ liệu, lỗi 401/403 của JWT đều trả về cùng một JSON này.
/// </summary>
public class ErrorResponse
{
    /// <summary>Luôn là false, giúp client phân biệt nhanh với response thành công (ApiResponse)</summary>
    public bool Success { get; init; } = false;

    /// <summary>HTTP status code (400, 401, 403, 404, 409, 500...)</summary>
    public int StatusCode { get; set; }

    /// <summary>Thông báo lỗi cho người dùng</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Danh sách lỗi chi tiết (lỗi validate từng trường, gợi ý cách sửa...)</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string>? Errors { get; set; }

    /// <summary>Chi tiết kỹ thuật (stack trace) — chỉ có ở môi trường Development, còn lại là null</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? Detail { get; set; }

    /// <summary>Mã truy vết request, dùng để tìm dòng log tương ứng trong file Logs/log-*.txt</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>Thời điểm xảy ra lỗi (UTC)</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ErrorResponse Create(HttpContext context, int statusCode, string message,
        IEnumerable<string>? errors = null, string? detail = null) => new()
    {
        StatusCode = statusCode,
        Message = message,
        Errors = errors,
        Detail = detail,
        TraceId = context.TraceIdentifier,
        Timestamp = DateTime.UtcNow
    };
}
