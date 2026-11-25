using System.ComponentModel.DataAnnotations;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.DTOs.Auth;

public class RegisterDto
{
    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>Landlord (chủ nhà) hoặc Tenant (người thuê). Không cho phép tự đăng ký Admin.</summary>
    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; } = UserRole.Tenant;
}
