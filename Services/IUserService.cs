using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Users;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetPagedAsync(UserQueryDto query);
    Task<UserDto> GetByIdAsync(int id);
    Task<UserDto> GetCurrentAsync();
    Task<UserDto> UpdateProfileAsync(UpdateProfileDto dto);
    Task<UserDto> UpdateStatusAsync(int id, bool isActive);
    Task<UserDto> UpdateRoleAsync(int id, UserRole role);

    /// <summary>Hồ sơ công khai (ai cũng xem được)</summary>
    Task<PublicProfileDto> GetPublicProfileAsync(int id);
}
