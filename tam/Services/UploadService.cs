using HouseRentalAPI.DTOs.Uploads;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;

namespace HouseRentalAPI.Services;

public class UploadService(
    IRepository<UploadedImage> uploadRepository,
    IRepository<PropertyImage> propertyImageRepository,
    IRepository<User> userRepository,
    IFileStorageService fileStorage,
    ICurrentUserService currentUser) : IUploadService
{
    public async Task<UploadResultDto> UploadImageAsync(IFormFile file)
    {
        var userId = currentUser.GetRequiredUserId();
        var stored = await fileStorage.SaveImageAsync(file);

        var image = new UploadedImage
        {
            Url = stored.Url,
            FilePath = stored.FilePath,
            FileName = stored.FileName,
            OriginalFileName = Path.GetFileName(file.FileName),
            Size = stored.Size,
            ContentType = stored.ContentType,
            UploadedById = userId
        };

        try
        {
            await uploadRepository.AddAsync(image);
            await uploadRepository.SaveChangesAsync();
        }
        catch
        {
            // Ghi database lỗi → xóa file vừa lưu để không để lại file "mồ côi"
            fileStorage.DeleteFile(stored.FilePath);
            throw;
        }

        return ToDto(image);
    }

    public async Task<UploadResultDto> GetByIdAsync(int id)
    {
        var image = await GetOrThrowAsync(id);

        // Chỉ người upload hoặc Admin được xem thông tin ảnh (tên file gốc, người upload...) → chống IDOR dò id
        if (image.UploadedById != currentUser.GetRequiredUserId() && !currentUser.IsAdmin)
            throw new NotFoundException($"Không tìm thấy ảnh có id {id}");

        return ToDto(image);
    }

    public async Task<IReadOnlyList<UploadResultDto>> GetMineAsync()
    {
        var userId = currentUser.GetRequiredUserId();
        var images = currentUser.IsAdmin
            ? await uploadRepository.GetAllAsync()
            : await uploadRepository.FindAsync(i => i.UploadedById == userId);

        return images.OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id).Select(ToDto).ToList();
    }

    public async Task DeleteAsync(int id)
    {
        var image = await GetOrThrowAsync(id);

        if (image.UploadedById != currentUser.GetRequiredUserId() && !currentUser.IsAdmin)
            throw new ForbiddenException("Bạn chỉ được xóa ảnh do mình upload");

        // Ảnh đang được dùng thì không xóa, tránh làm hỏng ảnh của tin đăng / avatar
        if (await propertyImageRepository.AnyAsync(p => p.Url == image.Url))
            throw new ConflictException("Ảnh đang được dùng trong một tin đăng, hãy gỡ ảnh khỏi tin trước khi xóa");
        if (await userRepository.AnyAsync(u => u.AvatarUrl == image.Url))
            throw new ConflictException("Ảnh đang được dùng làm ảnh đại diện, hãy đổi ảnh đại diện trước khi xóa");

        uploadRepository.Remove(image);
        await uploadRepository.SaveChangesAsync();
        fileStorage.DeleteFile(image.FilePath);
    }

    private async Task<UploadedImage> GetOrThrowAsync(int id) =>
        await uploadRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Không tìm thấy ảnh có id {id}");

    private static UploadResultDto ToDto(UploadedImage i) =>
        new(i.Id, i.Url, i.FileName, i.OriginalFileName, i.Size, i.ContentType, i.UploadedById, i.CreatedAt);
}
