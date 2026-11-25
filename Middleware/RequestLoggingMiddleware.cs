using System.Diagnostics;
using System.Security.Claims;
using HouseRentalAPI.Constants;

namespace HouseRentalAPI.Middleware;

/// <summary>
/// Ghi một dòng log cho mỗi request: method, path, query, status, thời gian xử lý, user, TraceId.
/// Status >= 500 ghi mức Error, 4xx ghi Warning, còn lại Information.
/// </summary>
/// <remarks>
/// Log được ghi trong Response.OnCompleted (sau khi response đã gửi xong) chứ không ghi ngay sau next():
/// middleware này đứng SAU ExceptionHandlingMiddleware, nên khi có exception thì lúc next() ném lỗi,
/// status vẫn còn là 200; chỉ khi ExceptionHandlingMiddleware xử lý xong mới có status thật (400/404/500...).
/// </remarks>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        context.Response.OnCompleted(() =>
        {
            stopwatch.Stop();
            LogRequest(context, stopwatch.ElapsedMilliseconds);
            return Task.CompletedTask;
        });

        await next(context);
    }

    private void LogRequest(HttpContext context, long elapsedMs)
    {
        var statusCode = context.Response.StatusCode;
        var user = context.User;
        var userName = user.Identity?.IsAuthenticated == true
            ? user.FindFirstValue(AppClaimTypes.Email)
              ?? user.Identity.Name
              ?? user.FindFirstValue(AppClaimTypes.UserId)
              ?? "authenticated"
            : "anonymous";

        var level = statusCode >= 500 ? LogLevel.Error
            : statusCode >= 400 ? LogLevel.Warning
            : LogLevel.Information;

        logger.Log(level,
            "HTTP {Method} {Path}{Query} => {StatusCode} in {ElapsedMs}ms User={User} TraceId={TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty,
            statusCode,
            elapsedMs,
            userName,
            context.TraceIdentifier);
    }
}
