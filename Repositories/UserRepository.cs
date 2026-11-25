using HouseRentalAPI.Data;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Users;
using HouseRentalAPI.Extensions;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Repositories;

public class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository
{
    public Task<User?> GetByEmailAsync(string email) =>
        DbSet.FirstOrDefaultAsync(u => u.Email == email);

    public Task<bool> EmailExistsAsync(string email) =>
        DbSet.AnyAsync(u => u.Email == email);

    public Task<PagedResult<User>> GetPagedAsync(UserQueryDto query)
    {
        var users = DbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            users = users.Where(u =>
                u.FullName.Contains(keyword) ||
                u.Email.Contains(keyword) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(keyword)));
        }

        if (query.Role.HasValue)
            users = users.Where(u => u.Role == query.Role.Value);

        if (query.IsActive.HasValue)
            users = users.Where(u => u.IsActive == query.IsActive.Value);

        users = query.SortBy?.ToLowerInvariant() switch
        {
            "oldest" => users.OrderBy(u => u.CreatedAt),
            "name_asc" => users.OrderBy(u => u.FullName),
            "name_desc" => users.OrderByDescending(u => u.FullName),
            "email_asc" => users.OrderBy(u => u.Email),
            "last_login" => users.OrderByDescending(u => u.LastLoginAt),
            _ => users.OrderByDescending(u => u.CreatedAt)
        };

        return users.ToPagedResultAsync(query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<int>> GetActiveAdminIdsAsync() =>
        await DbSet
            .Where(u => u.Role == UserRole.Admin && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();
}
