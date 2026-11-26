using HouseRentalAPI.DTOs.Uploads;

namespace HouseRentalAPI.Services;

public interface IUploadService
{
    /// <summary>Lưu file ảnh và ghi một dòng vào bảng UploadedImages</summary>
    Task<UploadResultDto> UploadImageAsync(IFormFile file);

    Task<UploadResultDto> GetByIdAsync(int id);

    /// <summary>Ảnh do người đang đăng nhập upload (Admin: tất cả ảnh), mới nhất trước</summary>
    Task<IReadOnlyList<UploadResultDto>> GetMineAsync();

    /// <summary>Xóa ảnh (dòng trong database + file trên ổ đĩa)</summary>
    Task DeleteAsync(int id);
}
