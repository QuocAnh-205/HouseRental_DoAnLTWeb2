using HouseRentalAPI.DTOs.Common;
using System.ComponentModel.DataAnnotations;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.DTOs.Properties;

/// <summary>Các trường dùng chung khi tạo mới và cập nhật nhà</summary>
public abstract class PropertyFormDto
{
    [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
    [StringLength(200, MinimumLength = 10, ErrorMessage = "Tiêu đề từ 10 đến 200 ký tự")]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Ward { get; set; }

    [Required(ErrorMessage = "Quận/Huyện là bắt buộc")]
    [StringLength(100)]
    public string District { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tỉnh/Thành phố là bắt buộc")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "Vĩ độ không hợp lệ")]
    public double? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Kinh độ không hợp lệ")]
    public double? Longitude { get; set; }

    [EnumDataType(typeof(PropertyType))]
    public PropertyType Type { get; set; }

    [Range(1, 100_000, ErrorMessage = "Diện tích phải lớn hơn 0")]
    public double Area { get; set; }

    [Range(0, 50)]
    public int Bedrooms { get; set; }

    [Range(0, 50)]
    public int Bathrooms { get; set; }

    [Range(typeof(decimal), "100000", "10000000000", ErrorMessage = "Giá thuê phải từ 100.000đ")]
    public decimal PricePerMonth { get; set; }

    [Range(typeof(decimal), "0", "10000000000")]
    public decimal Deposit { get; set; }

    public List<string> Amenities { get; set; } = [];
}

public class CreatePropertyDto : PropertyFormDto
{
    /// <summary>Danh sách URL ảnh, ảnh đầu tiên sẽ là ảnh đại diện</summary>
    [MaxLength(20, ErrorMessage = "Tối đa 20 ảnh")]
    [ImageUrl]
    public List<string> ImageUrls { get; set; } = [];
}

public class UpdatePropertyDto : PropertyFormDto;

public class UpdatePropertyStatusDto
{
    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    [EnumDataType(typeof(PropertyStatus))]
    public PropertyStatus? Status { get; set; }

    /// <summary>Lý do (khi admin từ chối tin)</summary>
    [StringLength(500)]
    public string? Reason { get; set; }
}

public class AddPropertyImageDto
{
    [Required(ErrorMessage = "URL ảnh là bắt buộc")]
    [ImageUrl]
    [StringLength(500)]
    public string Url { get; set; } = string.Empty;

    public bool IsThumbnail { get; set; }
}
