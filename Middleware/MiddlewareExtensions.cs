namespace HouseRentalAPI.Middleware;

/// <summary>Extension method gắn các middleware tự viết vào pipeline trong Program.cs</summary>
public static class MiddlewareExtensions
{
    /// <summary>Bắt exception toàn cục → trả ErrorResponse. Đặt đầu tiên trong pipeline.</summary>
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();

    /// <summary>Ghi log mỗi request (method, path, status, thời gian, user, TraceId)</summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestLoggingMiddleware>();
}
