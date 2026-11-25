using HouseRentalAPI.DTOs.Users;

namespace HouseRentalAPI.DTOs.Auth;

public record AuthResponseDto(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserDto User);
