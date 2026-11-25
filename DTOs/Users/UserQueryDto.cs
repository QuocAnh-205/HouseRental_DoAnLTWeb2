using System.ComponentModel.DataAnnotations;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.DTOs.Users;

public class UserQueryDto : PaginationQuery
{
    /// <summary>Tìm theo họ tên, email hoặc số điện thoại</summary>
    public string? Keyword { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }

    /// <summary>newest (mặc định) | oldest | name_asc | name_desc | email_asc | last_login</summary>
    public string? SortBy { get; set; }
}

public class UpdateUserStatusDto
{
    public bool IsActive { get; set; }
}

public class UpdateUserRoleDto
{
    [Required(ErrorMessage = "Vai trò là bắt buộc")]
    [EnumDataType(typeof(UserRole))]
    public UserRole? Role { get; set; }
}
