using HouseRentalAPI.DTOs.Auth;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Mappings;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;
using BC = BCrypt.Net.BCrypt;

namespace HouseRentalAPI.Services;

public class AuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ITokenService tokenService,
    IEmailSender emailSender,
    INotificationService notificationService,
    ICurrentUserService currentUser,
    IHostEnvironment env) : IAuthService
{
    private static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(30);

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (dto.Role == UserRole.Admin)
            throw new BadRequestException("Không thể tự đăng ký tài khoản Admin");

        var email = NormalizeEmail(dto.Email);
        if (await userRepository.EmailExistsAsync(email))
            throw new ConflictException("Email đã được sử dụng");

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            PhoneNumber = dto.PhoneNumber?.Trim(),
            PasswordHash = BC.HashPassword(dto.Password),
            Role = dto.Role,
            LastLoginAt = DateTime.UtcNow
        };

        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();

        await notificationService.NotifyAsync(user.Id, NotificationType.System,
            "Chào mừng bạn đến với House Rental",
            user.Role == UserRole.Landlord
                ? "Hãy đăng tin cho thuê đầu tiên của bạn. Tin sẽ hiển thị sau khi được duyệt."
                : "Hãy bắt đầu tìm kiếm căn nhà phù hợp với bạn.",
            user.Role == UserRole.Landlord ? "/api/properties/mine" : "/api/properties");

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await userRepository.GetByEmailAsync(NormalizeEmail(dto.Email));

        if (user is null || !BC.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedException("Email hoặc mật khẩu không đúng");

        if (!user.IsActive)
            throw new ForbiddenException("Tài khoản đã bị khóa, vui lòng liên hệ quản trị viên");

        user.LastLoginAt = DateTime.UtcNow;
        return await IssueTokensAsync(user);
    }

    public async Task ChangePasswordAsync(ChangePasswordDto dto)
    {
        var user = await userRepository.GetByIdAsync(currentUser.GetRequiredUserId())
            ?? throw new NotFoundException("Không tìm thấy người dùng");

        if (!BC.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new BadRequestException("Mật khẩu hiện tại không đúng");

        user.PasswordHash = BC.HashPassword(dto.NewPassword);
        await userRepository.SaveChangesAsync();

        // Các phiên đăng nhập khác phải đăng nhập lại
        await refreshTokenRepository.RevokeAllAsync(user.Id);
    }

    public async Task<AuthResponseDto> RefreshAsync(string refreshToken)
    {
        var stored = await refreshTokenRepository.GetByHashAsync(tokenService.Hash(refreshToken));

        if (stored is null || !stored.IsActive)
            throw new UnauthorizedException("Refresh token không hợp lệ hoặc đã hết hạn");

        if (!stored.User.IsActive)
            throw new ForbiddenException("Tài khoản đã bị khóa, vui lòng liên hệ quản trị viên");

        stored.RevokedAt = DateTime.UtcNow;
        return await IssueTokensAsync(stored.User);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var stored = await refreshTokenRepository.GetByHashAsync(tokenService.Hash(refreshToken));

        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = DateTime.UtcNow;
            await refreshTokenRepository.SaveChangesAsync();
        }
    }

    public Task LogoutAllAsync() =>
        refreshTokenRepository.RevokeAllAsync(currentUser.GetRequiredUserId());

    public async Task<ForgotPasswordResultDto> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await userRepository.GetByEmailAsync(NormalizeEmail(dto.Email));
        if (user is null || !user.IsActive)
            return new ForgotPasswordResultDto(null, null);

        var token = tokenService.GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.Add(PasswordResetLifetime);

        user.PasswordResetTokenHash = tokenService.Hash(token);
        user.PasswordResetExpiresAt = expiresAt;
        await userRepository.SaveChangesAsync();

        await emailSender.SendAsync(user.Email, "Đặt lại mật khẩu House Rental",
            $"Mã đặt lại mật khẩu của bạn: {token}\nMã có hiệu lực trong {PasswordResetLifetime.TotalMinutes} phút.");

        return env.IsDevelopment()
            ? new ForgotPasswordResultDto(token, expiresAt)
            : new ForgotPasswordResultDto(null, null);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await userRepository.GetByEmailAsync(NormalizeEmail(dto.Email));

        var valid = user is { PasswordResetTokenHash: not null, PasswordResetExpiresAt: not null } &&
                    user.PasswordResetExpiresAt > DateTime.UtcNow &&
                    user.PasswordResetTokenHash == tokenService.Hash(dto.Token.Trim());

        if (!valid)
            throw new BadRequestException("Mã đặt lại mật khẩu không hợp lệ hoặc đã hết hạn");

        user!.PasswordHash = BC.HashPassword(dto.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetExpiresAt = null;

        await notificationService.NotifyAsync(user.Id, NotificationType.System,
            "Mật khẩu đã được đặt lại",
            "Mật khẩu tài khoản của bạn vừa được đặt lại. Nếu không phải bạn, hãy liên hệ quản trị viên ngay.",
            "/api/auth/me");

        await userRepository.SaveChangesAsync();
        await refreshTokenRepository.RevokeAllAsync(user.Id);
    }

    /// <summary>Cấp access token + refresh token mới và lưu mọi thay đổi đang chờ</summary>
    private async Task<AuthResponseDto> IssueTokensAsync(User user)
    {
        var (accessToken, expiresAt) = tokenService.GenerateAccessToken(user);
        var (refreshToken, refreshHash, refreshExpiresAt) = tokenService.GenerateRefreshToken();

        await refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExpiresAt
        });
        await refreshTokenRepository.SaveChangesAsync();

        return new AuthResponseDto(accessToken, "Bearer", expiresAt, refreshToken, refreshExpiresAt, user.ToDto());
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
