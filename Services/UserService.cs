using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Users;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Mappings;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;

namespace HouseRentalAPI.Services;

public class UserService(
    IUserRepository userRepository,
    IPropertyRepository propertyRepository,
    IRefreshTokenRepository refreshTokenRepository,
    INotificationService notificationService,
    ICurrentUserService currentUser) : IUserService
{
    public async Task<PagedResult<UserDto>> GetPagedAsync(UserQueryDto query)
    {
        var result = await userRepository.GetPagedAsync(query);
        return result.Map(u => u.ToDto());
    }

    public async Task<UserDto> GetByIdAsync(int id)
    {
        if (!currentUser.IsAdmin && currentUser.UserId != id)
            throw new ForbiddenException("Bạn không có quyền xem thông tin người dùng này");

        return (await FindAsync(id)).ToDto();
    }

    public async Task<UserDto> GetCurrentAsync() =>
        (await FindAsync(currentUser.GetRequiredUserId())).ToDto();

    public async Task<UserDto> UpdateProfileAsync(UpdateProfileDto dto)
    {
        var user = await FindAsync(currentUser.GetRequiredUserId());

        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = dto.PhoneNumber?.Trim();
        user.AvatarUrl = dto.AvatarUrl;

        await userRepository.SaveChangesAsync();
        return user.ToDto();
    }

    public async Task<UserDto> UpdateStatusAsync(int id, bool isActive)
    {
        if (currentUser.UserId == id)
            throw new BadRequestException("Không thể tự khóa tài khoản của chính mình");

        var user = await FindAsync(id);
        user.IsActive = isActive;

        await userRepository.SaveChangesAsync();

        // Khóa tài khoản thì thu hồi luôn các phiên đăng nhập
        if (!isActive)
            await refreshTokenRepository.RevokeAllAsync(id);

        return user.ToDto();
    }

    public async Task<UserDto> UpdateRoleAsync(int id, UserRole role)
    {
        if (currentUser.UserId == id)
            throw new BadRequestException("Không thể tự đổi vai trò của chính mình");

        var user = await FindAsync(id);
        if (user.Role == role)
            return user.ToDto();

        if (user.Role == UserRole.Landlord && await propertyRepository.AnyAsync(p => p.LandlordId == id))
            throw new BadRequestException("Chủ nhà đang có tin đăng, không thể đổi vai trò");

        user.Role = role;
        await notificationService.NotifyAsync(id, NotificationType.System,
            "Vai trò tài khoản đã thay đổi",
            $"Tài khoản của bạn đã được chuyển sang vai trò {role}. Vui lòng đăng nhập lại.",
            "/api/auth/me");

        await userRepository.SaveChangesAsync();

        // Token cũ còn mang role cũ, buộc đăng nhập lại
        await refreshTokenRepository.RevokeAllAsync(id);
        return user.ToDto();
    }

    public async Task<PublicProfileDto> GetPublicProfileAsync(int id)
    {
        var user = await FindAsync(id);
        if (!user.IsActive)
            throw new NotFoundException($"Không tìm thấy người dùng có id {id}");

        var listings = await propertyRepository.FindAsync(p => p.LandlordId == id && p.Status == PropertyStatus.Available);
        var reviewed = await propertyRepository.FindAsync(p => p.LandlordId == id && p.ReviewCount > 0);

        var totalReviews = reviewed.Sum(p => p.ReviewCount);
        var averageRating = totalReviews == 0
            ? 0
            : Math.Round(reviewed.Sum(p => p.AverageRating * p.ReviewCount) / totalReviews, 1);

        return new PublicProfileDto(
            user.Id,
            user.FullName,
            user.AvatarUrl,
            user.Role,
            user.CreatedAt,
            listings.Count,
            averageRating,
            totalReviews);
    }

    private async Task<User> FindAsync(int id) =>
        await userRepository.GetByIdAsync(id)
        ?? throw new NotFoundException($"Không tìm thấy người dùng có id {id}");
}
