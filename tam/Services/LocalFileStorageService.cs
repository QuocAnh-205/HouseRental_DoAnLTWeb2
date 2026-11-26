using HouseRentalAPI.DTOs.Uploads;
using HouseRentalAPI.Exceptions;

namespace HouseRentalAPI.Services;

/// <summary>
/// Lưu file vào wwwroot/uploads/yyyy/MM và phục vụ qua static files.
/// Khi triển khai nhiều server nên thay bằng S3 / Azure Blob / Cloudinary.
/// </summary>
public class LocalFileStorageService(IWebHostEnvironment env) : IFileStorageService
{
    private const long MaxImageBytes = 5 * 1024 * 1024;

    // Chữ ký đầu file để không tin vào phần mở rộng / Content-Type do client gửi
    private static readonly Dictionary<string, byte[][]> ImageSignatures = new()
    {
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".gif"] = [[0x47, 0x49, 0x46, 0x38]],
        [".webp"] = [[0x52, 0x49, 0x46, 0x46]]
    };

    public async Task<StoredFileDto> SaveImageAsync(IFormFile file)
    {
        if (file.Length == 0)
            throw new BadRequestException("File rỗng");

        if (file.Length > MaxImageBytes)
            throw new BadRequestException($"Ảnh tối đa 5MB, ảnh của bạn {file.Length / 1024d / 1024:0.#}MB. Hãy giảm dung lượng ảnh rồi thử lại.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension == ".jpeg")
            extension = ".jpg";

        if (!ImageSignatures.TryGetValue(extension, out var signatures))
            throw new BadRequestException("Chỉ chấp nhận ảnh .jpg, .png, .webp, .gif");

        await using (var stream = file.OpenReadStream())
        {
            var header = new byte[12];
            var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);

            var validSignature = signatures.Any(sig => read >= sig.Length && header.AsSpan(0, sig.Length).SequenceEqual(sig));
            if (extension == ".webp")
                validSignature &= read >= 12 && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);

            if (!validSignature)
                throw new BadRequestException("Nội dung file không phải ảnh hợp lệ");
        }

        var now = DateTime.UtcNow;
        var relativeDir = Path.Combine("uploads", now.ToString("yyyy"), now.ToString("MM"));
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var directory = Path.Combine(webRoot, relativeDir);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using (var output = File.Create(Path.Combine(directory, fileName)))
            await file.CopyToAsync(output);

        // Lưu URL TƯƠNG ĐỐI (/uploads/yyyy/MM/x.jpg): trình duyệt tự ghép với origin đang mở
        // (qua Vite proxy /uploads khi dev, hoặc cùng domain khi deploy) → đổi port / tên miền không gãy ảnh.
        var relativePath = $"{relativeDir.Replace('\\', '/')}/{fileName}";
        var url = $"/{relativePath}";

        return new StoredFileDto(url, relativePath, fileName, file.Length, file.ContentType);
    }

    public void DeleteFile(string relativePath)
    {
        var webRoot = Path.GetFullPath(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"));
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath.TrimStart('/', '\\')));

        // Chỉ cho xóa file nằm trong wwwroot/uploads
        if (!fullPath.StartsWith(Path.Combine(webRoot, "uploads") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return;

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
