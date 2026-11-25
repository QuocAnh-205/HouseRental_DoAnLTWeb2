using HouseRentalAPI.Models;

namespace HouseRentalAPI.DTOs.Users;

public record UserDto(
    int Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? AvatarUrl,
    UserRole Role,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

/// <summary>Hồ sơ công khai, không lộ email / số điện thoại</summary>
public record PublicProfileDto(
    int Id,
    string FullName,
    string? AvatarUrl,
    UserRole Role,
    DateTime JoinedAt,
    int ActiveListings,
    double AverageRating,
    int TotalReviews);
