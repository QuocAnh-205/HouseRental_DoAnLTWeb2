namespace HouseRentalAPI.Models;

/// <summary>Ảnh người dùng upload qua POST /api/uploads/images (file nằm trong wwwroot/uploads/yyyy/MM)</summary>
public class UploadedImage : BaseEntity
{
    /// <summary>URL công khai để hiển thị / gắn vào tin đăng, avatar</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Đường dẫn tương đối trong wwwroot, ví dụ uploads/2026/11/abc.jpg (dùng khi xóa file)</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Tên file đã lưu trên server (GUID)</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Tên file gốc người dùng chọn</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;

    public int UploadedById { get; set; }
    public User UploadedBy { get; set; } = null!;
}
