using HouseRentalAPI.DTOs.Uploads;

namespace HouseRentalAPI.Services;

public interface IFileStorageService
{
    /// <summary>Lưu ảnh (jpg, png, webp, gif, tối đa 5MB) vào wwwroot/uploads/yyyy/MM, trả về URL công khai + đường dẫn file</summary>
    Task<StoredFileDto> SaveImageAsync(IFormFile file);

    /// <summary>Xóa file theo đường dẫn tương đối trong wwwroot (bỏ qua nếu file không còn)</summary>
    void DeleteFile(string relativePath);
}
