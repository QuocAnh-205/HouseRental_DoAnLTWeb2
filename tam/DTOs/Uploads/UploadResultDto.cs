namespace HouseRentalAPI.DTOs.Uploads;

/// <summary>Thông tin ảnh đã upload (một dòng trong bảng UploadedImages)</summary>
public record UploadResultDto(
    int Id,
    string Url,
    string FileName,
    string OriginalFileName,
    long Size,
    string ContentType,
    int UploadedById,
    DateTime CreatedAt);

/// <summary>Kết quả lưu file trên ổ đĩa (nội bộ, chưa ghi database)</summary>
public record StoredFileDto(
    string Url,
    string FilePath,
    string FileName,
    long Size,
    string ContentType);
