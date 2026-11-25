using HouseRentalAPI.Data;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Repositories;

public class RefreshTokenRepository(AppDbContext context) : Repository<RefreshToken>(context), IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash) =>
        DbSet.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    public Task<int> RevokeAllAsync(int userId)
    {
        var now = DateTime.UtcNow;
        return DbSet
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
    }
}
