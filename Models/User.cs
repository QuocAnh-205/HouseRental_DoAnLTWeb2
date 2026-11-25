namespace HouseRentalAPI.Models;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Tenant;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    // Quên mật khẩu: chỉ lưu hash của mã đặt lại
    public string? PasswordResetTokenHash { get; set; }
    public DateTime? PasswordResetExpiresAt { get; set; }

    // Chủ nhà: danh sách nhà đăng cho thuê
    public ICollection<Property> Properties { get; set; } = new List<Property>();

    // Người thuê: danh sách yêu cầu thuê và đánh giá
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
