using HouseRentalAPI.Models;

namespace HouseRentalAPI.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);

    /// <summary>Tạo refresh token ngẫu nhiên; chỉ lưu Hash vào DB, trả Token cho client</summary>
    (string Token, string Hash, DateTime ExpiresAt) GenerateRefreshToken();

    /// <summary>Chuỗi ngẫu nhiên an toàn (base64url)</summary>
    string GenerateSecureToken(int byteLength = 32);

    /// <summary>SHA-256 dạng hex, dùng để lưu token mà không lưu bản gốc</summary>
    string Hash(string token);
}
