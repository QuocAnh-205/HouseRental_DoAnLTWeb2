using System.Text.Json;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Middleware;

/// <summary>
/// Bọc toàn bộ request: bắt mọi exception, ghi log (Warning cho lỗi nghiệp vụ, Error cho lỗi 500)
/// và trả về JSON <see cref="ErrorResponse"/>. Chỉ môi trường Development mới trả stack trace trong "detail".
/// </summary>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment env)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Response đã gửi một phần thì không thể đổi status/nội dung nữa → chỉ ghi log rồi ném tiếp
            if (context.Response.HasStarted)
            {
                logger.LogError(ex, "Exception sau khi response đã bắt đầu gửi. TraceId={TraceId} {Method} {Path}",
                    context.TraceIdentifier, context.Request.Method, context.Request.Path);
                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = MapException(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception,
                "Unhandled exception. TraceId={TraceId} {Method} {Path}",
                context.TraceIdentifier, context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogWarning(exception,
                "Handled exception {StatusCode}. TraceId={TraceId} {Method} {Path}",
                statusCode, context.TraceIdentifier, context.Request.Method, context.Request.Path);
        }

        var response = ErrorResponse.Create(context, statusCode, message,
            detail: env.IsDevelopment() ? exception.ToString() : null);

        context.Response.Clear();
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    /// <summary>Exception → (HTTP status, thông báo cho người dùng)</summary>
    private static (int StatusCode, string Message) MapException(Exception exception) => exception switch
    {
        // Lỗi nghiệp vụ do code tự throw (400, 401, 403, 404, 409)
        AppException appEx => (appEx.StatusCode, appEx.Message),

        // Lỗi request do Kestrel/ASP.NET phát hiện (body quá lớn, request hỏng...)
        BadHttpRequestException badRequest => (badRequest.StatusCode,
            badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge ? "Dữ liệu gửi lên quá lớn" : "Request không hợp lệ"),

        // Lỗi cơ sở dữ liệu
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
            "Dữ liệu đã bị thay đổi bởi người khác, vui lòng tải lại"),
        DbUpdateException => (StatusCodes.Status409Conflict,
            "Không thể lưu dữ liệu do vi phạm ràng buộc"),

        // Exception chuẩn của .NET
        ArgumentException or InvalidOperationException => (StatusCodes.Status400BadRequest, exception.Message),
        KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, exception.Message),

        // Còn lại: lỗi hệ thống, không lộ thông tin nội bộ ra ngoài
        _ => (StatusCodes.Status500InternalServerError, "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.")
    };
}
