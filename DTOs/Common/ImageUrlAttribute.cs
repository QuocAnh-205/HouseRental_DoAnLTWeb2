using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace HouseRentalAPI.DTOs.Common;

/// <summary>
/// URL ảnh hợp lệ: đường dẫn tương đối của ảnh đã upload lên server (/uploads/...)
/// hoặc URL tuyệt đối http(s)://... (ảnh ngoài). Dùng được cho string và danh sách string.
/// Thay cho [Url] vì [Url] từ chối đường dẫn tương đối mà API upload và dữ liệu mẫu đang dùng.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ImageUrlAttribute : ValidationAttribute
{
    public ImageUrlAttribute() : base("URL ảnh không hợp lệ (chấp nhận /uploads/... hoặc http(s)://...)") { }

    public override bool IsValid(object? value) => value switch
    {
        null => true,
        string s => IsValidUrl(s),
        IEnumerable items => items.Cast<object?>().All(i => i is string s && IsValidUrl(s)),
        _ => false
    };

    public static bool IsValidUrl(string value)
    {
        var url = value.Trim();
        if (url.Length == 0)
            return false;

        // Đường dẫn tương đối tới file tĩnh của server, không cho "//host" hay "../"
        if (url.StartsWith('/'))
            return !url.StartsWith("//") && !url.Contains("..") && !url.Contains('\\');

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
