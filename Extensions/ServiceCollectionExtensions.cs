using System.Text;
using System.Text.Json.Serialization;
using HouseRentalAPI.Constants;
using HouseRentalAPI.Data;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;
using HouseRentalAPI.Services;
using HouseRentalAPI.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HouseRentalAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public const string CorsPolicyName = "AllowFrontend";

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IUploadService, UploadService>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddHostedService<RentalMaintenanceService>();
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(JwtSettings.SectionName);
        services.Configure<JwtSettings>(section);

        var jwt = section.Get<JwtSettings>()
            ?? throw new InvalidOperationException("Thiếu cấu hình Jwt");

        if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
            throw new InvalidOperationException("Jwt:Key phải có ít nhất 32 ký tự");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = AppClaimTypes.Name,
                    RoleClaimType = AppClaimTypes.Role
                };

                // Trả lỗi 401/403 cùng định dạng ApiResponse
                options.Events = new JwtBearerEvents
                {
                    // Chấp nhận các cách dán token hay gặp trên Swagger:
                    // "Bearer Bearer eyJ..." (dán cả chữ Bearer vào ô Authorize) hoặc "Bearer \"eyJ...\"" (còn dấu ngoặc kép)
                    OnMessageReceived = context =>
                    {
                        var header = context.Request.Headers.Authorization.ToString();
                        if (string.IsNullOrWhiteSpace(header))
                            return Task.CompletedTask;

                        var token = header.Trim();
                        while (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            token = token[7..].Trim();
                        token = token.Trim('"', '\'', ' ');

                        if (token.Length > 0)
                            context.Token = token;
                        return Task.CompletedTask;
                    },

                    // Token hợp lệ về chữ ký vẫn phải khớp trạng thái HIỆN TẠI của tài khoản trong CSDL:
                    // Admin khóa tài khoản hoặc đổi vai trò → access token cũ bị từ chối NGAY ở request kế tiếp,
                    // không phải chờ token hết hạn (refresh token cũng đã bị thu hồi trong UserService).
                    OnTokenValidated = async context =>
                    {
                        var sub = context.Principal?.FindFirst(AppClaimTypes.UserId)?.Value;
                        var roleClaim = context.Principal?.FindFirst(AppClaimTypes.Role)?.Value;
                        if (!int.TryParse(sub, out var userId))
                        {
                            context.Fail(new AccountStateChangedException("Token không chứa thông tin người dùng hợp lệ"));
                            return;
                        }

                        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var user = await db.Users.AsNoTracking()
                            .Where(u => u.Id == userId)
                            .Select(u => new { u.IsActive, u.Role })
                            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                        if (user is null)
                            context.Fail(new AccountStateChangedException("Tài khoản không còn tồn tại"));
                        else if (!user.IsActive)
                            context.Fail(new AccountStateChangedException("Tài khoản của bạn đã bị khóa"));
                        else if (!string.Equals(user.Role.ToString(), roleClaim, StringComparison.Ordinal))
                            context.Fail(new AccountStateChangedException("Vai trò tài khoản đã thay đổi, vui lòng đăng nhập lại"));
                    },

                    // Báo rõ lý do 401 để người dùng biết cách sửa
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                        var hasHeader = !string.IsNullOrWhiteSpace(context.Request.Headers.Authorization.ToString());
                        var (message, hint) = context.AuthenticateFailure switch
                        {
                            AccountStateChangedException ex =>
                                (ex.Message, "Vui lòng đăng nhập lại. Nếu tài khoản bị khóa, hãy liên hệ quản trị viên."),
                            SecurityTokenExpiredException =>
                                ("Token đã hết hạn",
                                 "Gọi POST /api/auth/login (hoặc /api/auth/refresh) để lấy accessToken mới rồi bấm Authorize lại."),
                            not null =>
                                ("Token không hợp lệ",
                                 "Hãy dán đúng giá trị accessToken (không phải refreshToken), copy đủ toàn bộ chuỗi, không thêm ký tự khác."),
                            null when !hasHeader =>
                                ("Bạn chưa đăng nhập (request không gửi kèm token)",
                                 "Trên Swagger: gọi POST /api/auth/login, copy accessToken, bấm nút Authorize ở đầu trang, dán token rồi gọi lại API. Tải lại trang (F5) sẽ mất token, phải Authorize lại."),
                            _ =>
                                ("Bạn chưa đăng nhập hoặc token không hợp lệ",
                                 "Header phải có dạng: Authorization: Bearer <accessToken>.")
                        };

                        await context.Response.WriteAsJsonAsync(
                            ErrorResponse.Create(context.HttpContext, StatusCodes.Status401Unauthorized, message, [hint]));
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(
                            ErrorResponse.Create(context.HttpContext, StatusCodes.Status403Forbidden,
                                "Bạn không có quyền thực hiện thao tác này"));
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        // Lỗi validate DTO trả về cùng định dạng ErrorResponse như exception
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var http = context.HttpContext;
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e =>
                        string.IsNullOrEmpty(e.ErrorMessage) ? $"{x.Key}: giá trị không hợp lệ" : e.ErrorMessage))
                    .ToList();

                // Request vượt giới hạn dung lượng → báo bằng tiếng Việt, mã 413
                if (errors.Any(e => e.Contains("Request body too large", StringComparison.OrdinalIgnoreCase)
                                    || e.Contains("Multipart body length limit", StringComparison.OrdinalIgnoreCase)))
                    return new ObjectResult(ErrorResponse.Create(http, StatusCodes.Status413PayloadTooLarge,
                        "File quá lớn. Ảnh chỉ được tối đa 5MB."))
                    { StatusCode = StatusCodes.Status413PayloadTooLarge };

                return new BadRequestObjectResult(ErrorResponse.Create(http, StatusCodes.Status400BadRequest,
                    "Dữ liệu không hợp lệ", errors));
            };
        });

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy => policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod());
        });

        return services;
    }
}

/// <summary>Token hợp lệ nhưng tài khoản đã bị khóa / đổi vai trò / bị xóa sau khi token được cấp</summary>
public sealed class AccountStateChangedException(string message) : SecurityTokenException(message);
