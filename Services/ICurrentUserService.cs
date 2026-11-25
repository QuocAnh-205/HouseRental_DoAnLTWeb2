namespace HouseRentalAPI.Services;

/// <summary>
/// Thông tin người dùng đang gọi API, đọc từ JWT của request hiện tại.
/// </summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsInRole(string role);

    /// <summary>Trả về UserId, ném UnauthorizedException nếu chưa đăng nhập</summary>
    int GetRequiredUserId();
}
