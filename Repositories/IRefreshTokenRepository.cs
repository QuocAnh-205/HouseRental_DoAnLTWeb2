using HouseRentalAPI.Models;

namespace HouseRentalAPI.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>Tìm theo hash, kèm người dùng (có tracking)</summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash);

    /// <summary>Thu hồi mọi refresh token còn hiệu lực của người dùng (đăng xuất mọi thiết bị)</summary>
    Task<int> RevokeAllAsync(int userId);
}
