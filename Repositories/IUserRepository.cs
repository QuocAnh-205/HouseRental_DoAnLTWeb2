using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Users;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<PagedResult<User>> GetPagedAsync(UserQueryDto query);
    Task<IReadOnlyList<int>> GetActiveAdminIdsAsync();
}
