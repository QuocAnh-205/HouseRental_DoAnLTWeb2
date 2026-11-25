using HouseRentalAPI.Data;
using HouseRentalAPI.Extensions;
using HouseRentalAPI.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;

// Logger tạm dùng trong lúc khởi động (trước khi đọc được appsettings) để không mất log lỗi khởi động
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Đang khởi động HouseRentalAPI...");

    var builder = WebApplication.CreateBuilder(args);

    // ===== Serilog: đọc cấu hình từ appsettings.json (mục "Serilog") → ghi Console + file Logs/log-YYYYMMDD.txt =====
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // ===== Services =====
    builder.Services
        .AddDatabase(builder.Configuration)          // AppDbContext (SQL Server)
        .AddRepositories()                            // Repository pattern
        .AddApplicationServices()                     // Service nghiệp vụ
        .AddJwtAuthentication(builder.Configuration)  // JWT Bearer (mục "Jwt" trong appsettings)
        .AddApiControllers()                          // Controllers + lỗi validate trả ErrorResponse
        .AddCorsPolicy(builder.Configuration)
        .AddApiRateLimiting()
        .AddApiHealthChecks();

    // Enum hiển thị dạng chữ (Admin, Landlord...) cả trong tài liệu OpenAPI → Swagger có ô chọn giá trị
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

    // Tài liệu OpenAPI + nút Authorize (Bearer token) trên Swagger
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        options.AddOperationTransformer<FormFileOperationTransformer>();
    });

    var app = builder.Build();

    // ===== Middleware pipeline =====
    // 1. Bắt exception toàn cục → ErrorResponse. Đặt ĐẦU TIÊN để bắt được lỗi của mọi middleware phía sau.
    app.UseExceptionHandling();
    // 2. Ghi log mỗi request (method, path, status, thời gian xử lý, user, TraceId)
    app.UseRequestLogging();

    app.UseSecurityHeaders();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();                       // /openapi/v1.json
        app.UseSwaggerUI(options =>             // /swagger
        {
            options.SwaggerEndpoint("/openapi/v1.json", "House Rental API v1");
            options.DocumentTitle = "House Rental API";
        });
        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
        app.MapScalarApiReference(options =>    // /scalar
        {
            options.WithTitle("House Rental API");
        });
    }

    // Chỉ chuyển hướng HTTPS khi app thực sự lắng nghe cổng HTTPS (profile "http" thì bỏ qua)
    if (!string.IsNullOrEmpty(app.Configuration["ASPNETCORE_HTTPS_PORT"]) ||
        (app.Configuration["ASPNETCORE_URLS"] ?? app.Configuration["urls"] ?? string.Empty).Contains("https://"))
        app.UseHttpsRedirection();
    app.UseStaticFiles();                       // ảnh upload: /uploads/...
    app.UseCors(ServiceCollectionExtensions.CorsPolicyName);
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    // Kiểm tra sức khỏe hệ thống (API + cơ sở dữ liệu), dùng cho giám sát / triển khai
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                status = report.Status.ToString(),
                totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds),
                checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString(), description = e.Value.Description })
            });
        }
    }).AllowAnonymous();

    app.MapControllers();

    if (app.Configuration.GetValue<bool>("Database:SeedOnStartup"))
        await DbSeeder.SeedAsync(app.Services);

    app.Run();
}
// HostAbortedException: công cụ "dotnet ef" chủ động dừng host khi tạo migration → không phải lỗi
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Ứng dụng dừng bất thường khi khởi động.");
}
finally
{
    Log.CloseAndFlush();
}
