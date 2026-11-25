using System.Threading.RateLimiting;
using HouseRentalAPI.Data;
using HouseRentalAPI.DTOs.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HouseRentalAPI.Extensions;

/// <summary>
/// Các lớp bảo vệ bổ sung: giới hạn tần suất gọi API, kiểm tra sức khỏe hệ thống, header bảo mật.
/// </summary>
public static class SecurityExtensions
{
    /// <summary>Policy áp cho đăng nhập / đăng ký / quên mật khẩu để chống dò mật khẩu (brute force)</summary>
    public const string AuthRateLimitPolicy = "auth";

    /// <summary>Policy áp cho upload ảnh: mỗi người dùng tối đa 40 ảnh / phút (đủ cho 1 tin đăng 20 ảnh)</summary>
    public const string UploadRateLimitPolicy = "upload";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Mỗi IP tối đa 10 lần gọi API xác thực trong 1 phút
            options.AddPolicy(AuthRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // Upload ảnh: tính theo người dùng (đã đăng nhập), không có thì theo IP
            options.AddPolicy(UploadRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User.FindFirst(Constants.AppClaimTypes.UserId)?.Value
                        ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 40,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // Toàn bộ API: mỗi IP tối đa 300 request / phút
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(
                    ErrorResponse.Create(context.HttpContext, StatusCodes.Status429TooManyRequests,
                        "Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút"), cancellationToken);
            };
        });

        return services;
    }

    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);
        return services;
    }

    /// <summary>Header bảo mật cơ bản cho mọi response</summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self)";
            await next();
        });
}

/// <summary>Kiểm tra kết nối cơ sở dữ liệu cho endpoint /health</summary>
public class DatabaseHealthCheck(AppDbContext context) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext healthContext, CancellationToken cancellationToken = default) =>
        await context.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("Kết nối cơ sở dữ liệu bình thường")
            : HealthCheckResult.Unhealthy("Không kết nối được cơ sở dữ liệu");
}
