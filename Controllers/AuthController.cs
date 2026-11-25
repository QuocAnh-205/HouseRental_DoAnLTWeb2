using HouseRentalAPI.DTOs.Auth;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Users;
using HouseRentalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using HouseRentalAPI.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HouseRentalAPI.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IAuthService authService, IUserService userService) : ControllerBase
{
    /// <summary>Đăng ký tài khoản (Landlord hoặc Tenant)</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityExtensions.AuthRateLimitPolicy)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(RegisterDto dto)
    {
        var result = await authService.RegisterAsync(dto);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(result, "Đăng ký thành công"));
    }

    /// <summary>Đăng nhập, trả về access token và refresh token</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityExtensions.AuthRateLimitPolicy)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(LoginDto dto)
    {
        var result = await authService.LoginAsync(dto);
        return ApiResponse.Ok(result, "Đăng nhập thành công");
    }

    /// <summary>Lấy access token mới bằng refresh token (refresh token cũ bị thu hồi)</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityExtensions.AuthRateLimitPolicy)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Refresh(RefreshTokenRequestDto dto)
    {
        var result = await authService.RefreshAsync(dto.RefreshToken);
        return ApiResponse.Ok(result, "Làm mới token thành công");
    }

    /// <summary>Đăng xuất: thu hồi refresh token</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Logout(RefreshTokenRequestDto dto)
    {
        await authService.LogoutAsync(dto.RefreshToken);
        return ApiResponse.Ok("Đăng xuất thành công");
    }

    /// <summary>Đăng xuất khỏi mọi thiết bị</summary>
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> LogoutAll()
    {
        await authService.LogoutAllAsync();
        return ApiResponse.Ok("Đã đăng xuất khỏi mọi thiết bị");
    }

    /// <summary>
    /// Quên mật khẩu: gửi mã đặt lại qua email.
    /// Môi trường Development trả luôn mã trong response để test.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityExtensions.AuthRateLimitPolicy)]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResultDto>>> ForgotPassword(ForgotPasswordDto dto)
    {
        var result = await authService.ForgotPasswordAsync(dto);
        return ApiResponse.Ok(result, "Nếu email tồn tại, mã đặt lại mật khẩu đã được gửi");
    }

    /// <summary>Đặt lại mật khẩu bằng mã nhận được</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityExtensions.AuthRateLimitPolicy)]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(ResetPasswordDto dto)
    {
        await authService.ResetPasswordAsync(dto);
        return ApiResponse.Ok("Đặt lại mật khẩu thành công, vui lòng đăng nhập lại");
    }

    /// <summary>Thông tin tài khoản đang đăng nhập</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> Me()
    {
        var result = await userService.GetCurrentAsync();
        return ApiResponse.Ok(result);
    }

    /// <summary>Đổi mật khẩu (các phiên đăng nhập khác sẽ bị đăng xuất)</summary>
    [HttpPut("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordDto dto)
    {
        await authService.ChangePasswordAsync(dto);
        return ApiResponse.Ok("Đổi mật khẩu thành công");
    }
}
