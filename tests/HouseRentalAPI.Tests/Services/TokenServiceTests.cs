using System.Text;
using HouseRentalAPI.Constants;
using HouseRentalAPI.Models;
using HouseRentalAPI.Services;
using HouseRentalAPI.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HouseRentalAPI.Tests.Services;

public class TokenServiceTests
{
    private static readonly JwtSettings Settings = new()
    {
        Key = "Test_Secret_Key_For_Unit_Tests_At_Least_32_Chars!",
        Issuer = "HouseRentalAPI",
        Audience = "HouseRentalClient",
        ExpiryMinutes = 60,
        RefreshTokenDays = 7
    };

    private readonly TokenService _service = new(Options.Create(Settings));

    private static TokenValidationParameters Validation(string key) => new()
    {
        ValidIssuer = Settings.Issuer,
        ValidAudience = Settings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };

    [Fact]
    public async Task AccessToken_HopLe_VaChuaDungClaim()
    {
        var user = new User { Id = 42, Email = "tenant@test.vn", FullName = "Người Thuê", Role = UserRole.Tenant };

        var (token, expiresAt) = _service.GenerateAccessToken(user);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, Validation(Settings.Key));

        Assert.True(result.IsValid);
        Assert.Equal("42", result.Claims[AppClaimTypes.UserId]);
        Assert.Equal("Tenant", result.Claims[AppClaimTypes.Role]);
        Assert.InRange(expiresAt, DateTime.UtcNow.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));
    }

    [Fact]
    public async Task AccessToken_KiemTraBangKhoaKhac_BiTuChoi()
    {
        var (token, _) = _service.GenerateAccessToken(new User { Id = 1, Email = "a@b.vn", FullName = "A", Role = UserRole.Admin });

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token,
            Validation("Another_Completely_Different_Secret_Key_123456"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void RefreshToken_NgauNhien_VaChiLuuHash()
    {
        var first = _service.GenerateRefreshToken();
        var second = _service.GenerateRefreshToken();

        Assert.NotEqual(first.Token, second.Token);
        Assert.Equal(_service.Hash(first.Token), first.Hash);
        Assert.NotEqual(first.Token, first.Hash);
        Assert.True(first.ExpiresAt > DateTime.UtcNow.AddDays(6));
    }
}
