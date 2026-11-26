using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Uploads;
using HouseRentalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HouseRentalAPI.Extensions;

namespace HouseRentalAPI.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize]
[Produces("application/json")]
public class UploadsController(IUploadService uploadService) : ControllerBase
{
    /// <summary>
    /// Upload ảnh (jpg, png, webp, gif, tối đa 5MB). File lưu ở wwwroot/uploads/yyyy/MM,
    /// thông tin ảnh lưu vào bảng UploadedImages và trả về id. Dùng URL trả về cho ảnh nhà hoặc avatar.
    /// </summary>
    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting(SecurityExtensions.UploadRateLimitPolicy)]
    // Cho phép nhận request lớn hơn giới hạn ảnh (5MB) để đọc hết file rồi trả lỗi rõ ràng.
    // Nếu giới hạn request sát 5MB, server sẽ cắt kết nối giữa chừng và trình duyệt chỉ thấy "Failed to fetch".
    [RequestSizeLimit(UploadLimits.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadLimits.MaxRequestBytes)]
    public async Task<ActionResult<ApiResponse<UploadResultDto>>> UploadImage(IFormFile file)
    {
        var result = await uploadService.UploadImageAsync(file);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(result, "Upload ảnh thành công"));
    }

    /// <summary>Danh sách ảnh tôi đã upload (Admin: tất cả ảnh), mới nhất trước</summary>
    [HttpGet("images")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UploadResultDto>>>> GetMyImages() =>
        ApiResponse.Ok(await uploadService.GetMineAsync());

    /// <summary>Xem thông tin một ảnh theo id (chỉ người upload hoặc Admin)</summary>
    [HttpGet("images/{id:int}")]
    public async Task<ActionResult<ApiResponse<UploadResultDto>>> GetImage(int id) =>
        ApiResponse.Ok(await uploadService.GetByIdAsync(id));

    /// <summary>Xóa ảnh (người upload hoặc Admin): xóa dòng trong database và file trên ổ đĩa</summary>
    [HttpDelete("images/{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteImage(int id)
    {
        await uploadService.DeleteAsync(id);
        return ApiResponse.Ok("Xóa ảnh thành công");
    }
}

public static class UploadLimits
{
    /// <summary>
    /// Giới hạn request upload (15MB) — rộng hơn mốc 5MB để đọc hết file và báo lỗi tiếng Việt rõ ràng,
    /// nhưng đủ nhỏ để không bị lợi dụng làm tốn RAM/ổ đĩa (kết hợp rate limit "upload"). Ảnh hợp lệ vẫn tối đa 5MB.
    /// </summary>
    public const long MaxRequestBytes = 15L * 1024 * 1024;
}
