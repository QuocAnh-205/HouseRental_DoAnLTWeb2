using HouseRentalAPI.Models;

namespace HouseRentalAPI.DTOs.Properties;

/// <summary>Thông tin rút gọn dùng cho danh sách / kết quả tìm kiếm</summary>
public record PropertySummaryDto(
    int Id,
    string Title,
    string Address,
    string District,
    string City,
    PropertyType Type,
    double Area,
    int Bedrooms,
    int Bathrooms,
    decimal PricePerMonth,
    PropertyStatus Status,
    string? ThumbnailUrl,
    double AverageRating,
    int ReviewCount,
    int ViewCount,
    int LandlordId,
    string LandlordName,
    DateTime CreatedAt,
    string? Ward = null,
    int ImageCount = 0,
    string? LandlordAvatarUrl = null,
    double? Latitude = null,
    double? Longitude = null);

/// <summary>Một bài trong "Bản tin" trên trang cá nhân: tin đăng kèm đoạn mô tả ngắn và vài ảnh</summary>
public record PropertyPostDto(
    int Id,
    string Title,
    string Excerpt,
    PropertyType Type,
    decimal PricePerMonth,
    double Area,
    int Bedrooms,
    int Bathrooms,
    string Address,
    string? Ward,
    string District,
    string City,
    PropertyStatus Status,
    string? RejectReason,
    IReadOnlyList<string> ImageUrls,
    int ImageCount,
    IReadOnlyList<string> Amenities,
    int ViewCount,
    double AverageRating,
    int ReviewCount,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    int LandlordId,
    string LandlordName,
    string? LandlordAvatarUrl);

/// <summary>Số tin đang cho thuê theo loại nhà và theo quận/huyện</summary>
public record PropertyStatsDto(
    int Total,
    IReadOnlyList<CountItemDto> ByType,
    IReadOnlyList<CountItemDto> ByDistrict);

public record CountItemDto(string Key, int Count);

/// <summary>Thông tin chi tiết một căn nhà</summary>
public record PropertyDetailDto(
    int Id,
    string Title,
    string Description,
    string Address,
    string? Ward,
    string District,
    string City,
    double? Latitude,
    double? Longitude,
    PropertyType Type,
    double Area,
    int Bedrooms,
    int Bathrooms,
    decimal PricePerMonth,
    decimal Deposit,
    IReadOnlyList<string> Amenities,
    PropertyStatus Status,
    string? RejectReason,
    double AverageRating,
    int ReviewCount,
    int ViewCount,
    bool IsFavorited,
    LandlordInfoDto Landlord,
    IReadOnlyList<PropertyImageDto> Images,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>Email / SĐT chỉ trả về khi người xem đã đăng nhập (null → bị bỏ khỏi JSON)</summary>
public record LandlordInfoDto(
    int Id,
    string FullName,
    string? Email,
    string? PhoneNumber,
    string? AvatarUrl);

public record PropertyImageDto(
    int Id,
    string Url,
    bool IsThumbnail);
