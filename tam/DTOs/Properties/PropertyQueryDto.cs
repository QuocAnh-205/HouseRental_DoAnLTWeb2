using System.ComponentModel.DataAnnotations;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.DTOs.Properties;

public class PropertyQueryDto : PaginationQuery
{
    /// <summary>Tìm trong tiêu đề, mô tả, địa chỉ</summary>
    public string? Keyword { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public PropertyType? Type { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public double? MinArea { get; set; }
    public double? MaxArea { get; set; }
    public int? MinBedrooms { get; set; }
    public int? MinBathrooms { get; set; }

    /// <summary>Nhà phải có đủ tất cả tiện ích này, ví dụ amenities=Wifi&amp;amenities=Điều hòa</summary>
    public List<string>? Amenities { get; set; }

    /// <summary>Tìm quanh một vị trí: cần cả Latitude, Longitude và RadiusKm</summary>
    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    [Range(0.1, 100, ErrorMessage = "Bán kính từ 0.1 đến 100 km")]
    public double? RadiusKm { get; set; }

    /// <summary>Chỉ áp dụng cho chủ nhà / admin. Tìm kiếm công khai luôn là Available.</summary>
    public PropertyStatus? Status { get; set; }

    /// <summary>newest | price_asc | price_desc | area_desc | rating_desc | most_viewed | distance (cần Latitude, Longitude, RadiusKm)</summary>
    public string? SortBy { get; set; }
}
