using System.ComponentModel.DataAnnotations;
using HouseRentalAPI.DTOs.Common;

namespace HouseRentalAPI.DTOs.Users;

public class UpdateProfileDto
{
    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [ImageUrl(ErrorMessage = "Avatar phải là đường dẫn ảnh hợp lệ (/uploads/... hoặc http(s)://...)")]
    [StringLength(500)]
    public string? AvatarUrl { get; set; }
}
