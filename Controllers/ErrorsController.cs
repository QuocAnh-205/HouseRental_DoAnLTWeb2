using HouseRentalAPI.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HouseRentalAPI.Controllers;

/// <summary>
/// Endpoint dùng để kiểm tra Exception Middleware và Request Logging (xem response + file Logs/log-*.txt).
/// CHỈ hoạt động ở môi trường Development: môi trường khác trả 404 như endpoint không tồn tại
/// (tài liệu OpenAPI/Swagger cũng chỉ được bật ở Development, xem Program.cs).
/// </summary>
[ApiController]
[DevelopmentOnly]
[Route("api/[controller]")]
[AllowAnonymous]
[Produces("application/json")]
public class ErrorsController : ControllerBase
{
    /// <summary>Giả lập lỗi 400 (BadRequestException)</summary>
    [HttpGet("bad-request")]
    public IActionResult BadRequestError() =>
        throw new BadRequestException("Đây là lỗi 400 từ Exception Middleware.");

    /// <summary>Giả lập lỗi 401 (UnauthorizedException)</summary>
    [HttpGet("unauthorized")]
    public IActionResult UnauthorizedError() =>
        throw new UnauthorizedException("Đây là lỗi 401 từ Exception Middleware.");

    /// <summary>Giả lập lỗi 403 (ForbiddenException)</summary>
    [HttpGet("forbidden")]
    public IActionResult ForbiddenError() =>
        throw new ForbiddenException("Đây là lỗi 403 từ Exception Middleware.");

    /// <summary>Giả lập lỗi 404 (NotFoundException)</summary>
    [HttpGet("not-found")]
    public IActionResult NotFoundError() =>
        throw new NotFoundException("Đây là lỗi 404 từ Exception Middleware.");

    /// <summary>Giả lập lỗi 409 (ConflictException)</summary>
    [HttpGet("conflict")]
    public IActionResult ConflictError() =>
        throw new ConflictException("Đây là lỗi 409 từ Exception Middleware.");

    /// <summary>Giả lập lỗi 500 (exception không được xử lý)</summary>
    [HttpGet("server")]
    public IActionResult ServerError() =>
        throw new Exception("Đây là lỗi 500 giả lập (unhandled exception).");
}

/// <summary>Chặn endpoint demo/debug ngoài môi trường Development (trả 404)</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DevelopmentOnlyAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var env = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (!env.IsDevelopment())
            context.Result = new NotFoundResult();
    }

    public void OnResourceExecuted(ResourceExecutedContext context) { }
}
