using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Properties;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Mappings;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;

namespace HouseRentalAPI.Services;

public class PropertyService(
    IPropertyRepository propertyRepository,
    IBookingRepository bookingRepository,
    IFavoriteRepository favoriteRepository,
    INotificationService notificationService,
    ICurrentUserService currentUser) : IPropertyService
{
    public async Task<PagedResult<PropertySummaryDto>> SearchAsync(PropertyQueryDto query)
    {
        query.Status = PropertyStatus.Available;
        var result = await propertyRepository.SearchAsync(query);
        return result.Map(p => p.ToSummaryDto());
    }

    public Task<PropertyStatsDto> GetStatsAsync(string? city, PropertyType? type) =>
        propertyRepository.GetStatsAsync(city, type);

    public async Task<PagedResult<PropertySummaryDto>> GetMineAsync(PropertyQueryDto query)
    {
        var result = await propertyRepository.SearchAsync(query, currentUser.GetRequiredUserId());
        return result.Map(p => p.ToSummaryDto());
    }

    public async Task<PagedResult<PropertySummaryDto>> GetAllForAdminAsync(PropertyQueryDto query)
    {
        var result = await propertyRepository.SearchAsync(query);
        return result.Map(p => p.ToSummaryDto());
    }

    public async Task<PagedResult<PropertySummaryDto>> GetByLandlordAsync(int landlordId, PropertyQueryDto query)
    {
        query.Status = PropertyStatus.Available;
        var result = await propertyRepository.SearchAsync(query, landlordId);
        return result.Map(p => p.ToSummaryDto());
    }

    /// <summary>Trạng thái người ngoài được thấy trong bản tin của chủ nhà</summary>
    private static readonly PropertyStatus[] PublicPostStatuses = [PropertyStatus.Available, PropertyStatus.Rented];

    public async Task<PagedResult<PropertyPostDto>> GetFeedAsync(PropertyType? type, PaginationQuery query)
    {
        var result = await propertyRepository.GetFeedAsync(type, query.Page, query.PageSize);
        return result.Map(p => p.ToPostDto(includeRejectReason: false));
    }

    public async Task<PagedResult<PropertyPostDto>> GetPostsAsync(int userId, PaginationQuery query)
    {
        var isOwnerOrAdmin = currentUser.IsAdmin || currentUser.UserId == userId;
        var result = await propertyRepository.GetPostsByLandlordAsync(
            userId, isOwnerOrAdmin ? null : PublicPostStatuses, query.Page, query.PageSize);
        return result.Map(p => p.ToPostDto(includeRejectReason: isOwnerOrAdmin));
    }

    public async Task<PropertyDetailDto> GetByIdAsync(int id)
    {
        var property = await FindAsync(id);
        var canManage = CanManage(property);

        // Tin chưa duyệt / đã ẩn chỉ chủ nhà và admin được xem
        if (property.Status != PropertyStatus.Available && property.Status != PropertyStatus.Rented && !canManage)
            throw new NotFoundException($"Không tìm thấy nhà có id {id}");

        if (!canManage)
        {
            await propertyRepository.IncrementViewCountAsync(id);
            property.ViewCount++;
        }

        var isFavorited = currentUser.UserId is { } userId && await favoriteRepository.ExistsAsync(userId, id);
        // Khách chưa đăng nhập không nhận email / SĐT chủ nhà (khớp UI "Đăng nhập để xem số điện thoại")
        return property.ToDetailDto(isFavorited, includeContact: currentUser.IsAuthenticated);
    }

    public async Task<IReadOnlyList<PropertySummaryDto>> GetSimilarAsync(int id, int count)
    {
        var property = await propertyRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy nhà có id {id}");

        var similar = await propertyRepository.GetSimilarAsync(property, Math.Clamp(count, 1, 20));
        return similar.Select(p => p.ToSummaryDto()).ToList();
    }

    public async Task<PropertyDetailDto> CreateAsync(CreatePropertyDto dto)
    {
        var property = new Property
        {
            LandlordId = currentUser.GetRequiredUserId(),
            Status = PropertyStatus.PendingApproval
        };
        ApplyForm(property, dto);

        var imageUrls = dto.ImageUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct()
            .ToList();

        for (var i = 0; i < imageUrls.Count; i++)
            property.Images.Add(new PropertyImage { Url = imageUrls[i], IsThumbnail = i == 0 });

        await propertyRepository.AddAsync(property);
        await propertyRepository.SaveChangesAsync();

        await notificationService.NotifyAdminsAsync(NotificationType.Property,
            "Tin đăng mới chờ duyệt",
            $"Tin \"{property.Title}\" vừa được đăng và đang chờ duyệt.",
            $"/api/properties/{property.Id}");
        await propertyRepository.SaveChangesAsync();

        return (await FindAsync(property.Id)).ToDetailDto();
    }

    public async Task<PropertyDetailDto> UpdateAsync(int id, UpdatePropertyDto dto)
    {
        var property = await FindAsync(id);
        EnsureCanManage(property);

        ApplyForm(property, dto);

        // Tin bị từ chối sau khi chủ nhà sửa sẽ được gửi duyệt lại
        if (property.Status == PropertyStatus.Rejected && !currentUser.IsAdmin)
        {
            property.Status = PropertyStatus.PendingApproval;
            await notificationService.NotifyAdminsAsync(NotificationType.Property,
                "Tin đăng được gửi duyệt lại",
                $"Tin \"{property.Title}\" đã được chủ nhà chỉnh sửa và gửi duyệt lại.",
                $"/api/properties/{property.Id}");
        }

        await propertyRepository.SaveChangesAsync();
        return property.ToDetailDto();
    }

    public async Task DeleteAsync(int id)
    {
        var property = await FindAsync(id);
        EnsureCanManage(property);

        if (await bookingRepository.AnyAsync(b => b.PropertyId == id))
            throw new BadRequestException("Nhà đã có lịch sử thuê, không thể xóa. Hãy chuyển sang trạng thái Hidden để ẩn tin.");

        propertyRepository.Remove(property);
        await propertyRepository.SaveChangesAsync();
    }

    public async Task<PropertyDetailDto> UpdateStatusAsync(int id, PropertyStatus status, string? reason)
    {
        var property = await FindAsync(id);

        if (!currentUser.IsAdmin)
        {
            EnsureCanManage(property);

            // Chủ nhà chỉ được ẩn/hiện tin đã được duyệt
            var allowed = (property.Status, status) is
                (PropertyStatus.Available, PropertyStatus.Hidden) or
                (PropertyStatus.Hidden, PropertyStatus.Available);

            if (!allowed)
                throw new BadRequestException("Chủ nhà chỉ có thể ẩn hoặc hiện lại tin đăng đã được duyệt");
        }

        // Ràng buộc với hợp đồng: nhà đang có khách thuê (Approved) không được mở lại "Còn trống",
        // và chỉ nhà có hợp đồng hiệu lực mới được ở trạng thái Rented.
        var hasActiveContract = await bookingRepository.HasActiveContractAsync(property.Id);
        if (hasActiveContract && status is PropertyStatus.Available or PropertyStatus.PendingApproval or PropertyStatus.Rejected)
            throw new BadRequestException(
                "Nhà đang có hợp đồng thuê còn hiệu lực, không thể chuyển sang trạng thái này. " +
                "Hãy kết thúc hợp đồng trước (hoặc Admin chuyển về Rented để hiện lại tin).");
        if (!hasActiveContract && status == PropertyStatus.Rented)
            throw new BadRequestException("Nhà chưa có hợp đồng thuê hiệu lực, không thể chuyển sang Rented.");

        if (currentUser.IsAdmin && property.Status != status && property.LandlordId != currentUser.UserId)
            await NotifyLandlordAsync(property, status, reason?.Trim());

        property.Status = status;
        property.RejectReason = status == PropertyStatus.Rejected ? reason?.Trim() : null;

        // Lần đầu được duyệt: tin lên đầu Bản tin chung
        if (status == PropertyStatus.Available && property.PublishedAt is null)
            property.PublishedAt = DateTime.UtcNow;

        await propertyRepository.SaveChangesAsync();
        return property.ToDetailDto();
    }

    public async Task<PropertyImageDto> AddImageAsync(int id, AddPropertyImageDto dto)
    {
        var property = await FindAsync(id);
        EnsureCanManage(property);

        if (property.Images.Count >= 20)
            throw new BadRequestException("Mỗi tin đăng tối đa 20 ảnh");

        var isThumbnail = dto.IsThumbnail || property.Images.Count == 0;
        if (isThumbnail)
        {
            foreach (var img in property.Images)
                img.IsThumbnail = false;
        }

        var image = new PropertyImage { Url = dto.Url.Trim(), IsThumbnail = isThumbnail };
        property.Images.Add(image);

        await propertyRepository.SaveChangesAsync();
        return image.ToDto();
    }

    public async Task RemoveImageAsync(int id, int imageId)
    {
        var property = await FindAsync(id);
        EnsureCanManage(property);

        var image = FindImage(property, imageId);
        property.Images.Remove(image);

        if (image.IsThumbnail && property.Images.FirstOrDefault() is { } next)
            next.IsThumbnail = true;

        await propertyRepository.SaveChangesAsync();
    }

    public async Task SetThumbnailAsync(int id, int imageId)
    {
        var property = await FindAsync(id);
        EnsureCanManage(property);

        var image = FindImage(property, imageId);
        foreach (var img in property.Images)
            img.IsThumbnail = img == image;

        await propertyRepository.SaveChangesAsync();
    }

    private Task NotifyLandlordAsync(Property property, PropertyStatus status, string? reason)
    {
        var (title, message) = status switch
        {
            PropertyStatus.Available when property.Status == PropertyStatus.PendingApproval =>
                ("Tin đăng đã được duyệt", $"Tin \"{property.Title}\" đã được duyệt và hiển thị công khai."),
            PropertyStatus.Rejected =>
                ("Tin đăng bị từ chối", $"Tin \"{property.Title}\" bị từ chối. Lý do: {reason ?? "không có"}. Hãy chỉnh sửa để gửi duyệt lại."),
            PropertyStatus.Hidden =>
                ("Tin đăng bị ẩn", $"Tin \"{property.Title}\" đã bị quản trị viên ẩn. {reason}".TrimEnd()),
            _ =>
                ("Trạng thái tin đăng thay đổi", $"Tin \"{property.Title}\" đã chuyển sang trạng thái {status}.")
        };

        return notificationService.NotifyAsync(property.LandlordId, NotificationType.Property, title, message,
            $"/api/properties/{property.Id}");
    }

    private async Task<Property> FindAsync(int id) =>
        await propertyRepository.GetDetailAsync(id)
        ?? throw new NotFoundException($"Không tìm thấy nhà có id {id}");

    private static PropertyImage FindImage(Property property, int imageId) =>
        property.Images.FirstOrDefault(i => i.Id == imageId)
        ?? throw new NotFoundException($"Không tìm thấy ảnh có id {imageId}");

    private bool CanManage(Property property) =>
        currentUser.IsAdmin || currentUser.UserId == property.LandlordId;

    private void EnsureCanManage(Property property)
    {
        if (!CanManage(property))
            throw new ForbiddenException("Bạn không có quyền quản lý nhà này");
    }

    private static void ApplyForm(Property property, PropertyFormDto dto)
    {
        property.Title = dto.Title.Trim();
        property.Description = dto.Description.Trim();
        property.Address = dto.Address.Trim();
        property.Ward = dto.Ward?.Trim();
        property.District = dto.District.Trim();
        property.City = dto.City.Trim();
        property.Latitude = dto.Latitude;
        property.Longitude = dto.Longitude;
        property.Type = dto.Type;
        property.Area = dto.Area;
        property.Bedrooms = dto.Bedrooms;
        property.Bathrooms = dto.Bathrooms;
        property.PricePerMonth = dto.PricePerMonth;
        property.Deposit = dto.Deposit;
        property.Amenities = dto.Amenities
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .Distinct()
            .ToList();
    }
}
