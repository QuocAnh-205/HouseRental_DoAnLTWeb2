namespace HouseRentalAPI.Settings;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>Khóa ký HMAC-SHA256, tối thiểu 32 ký tự</summary>
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 30;

    /// <summary>Thời hạn refresh token (ngày)</summary>
    public int RefreshTokenDays { get; set; } = 7;
}
