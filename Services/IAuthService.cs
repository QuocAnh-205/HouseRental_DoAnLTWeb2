using HouseRentalAPI.DTOs.Auth;

namespace HouseRentalAPI.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task ChangePasswordAsync(ChangePasswordDto dto);

    /// <summary>Đổi refresh token cũ lấy cặp token mới (token cũ bị thu hồi)</summary>
    Task<AuthResponseDto> RefreshAsync(string refreshToken);

    Task LogoutAsync(string refreshToken);

    /// <summary>Đăng xuất khỏi mọi thiết bị của người dùng hiện tại</summary>
    Task LogoutAllAsync();

    /// <summary>Tạo mã đặt lại mật khẩu và gửi email. Không báo email có tồn tại hay không.</summary>
    Task<ForgotPasswordResultDto> ForgotPasswordAsync(ForgotPasswordDto dto);

    Task ResetPasswordAsync(ResetPasswordDto dto);
}
