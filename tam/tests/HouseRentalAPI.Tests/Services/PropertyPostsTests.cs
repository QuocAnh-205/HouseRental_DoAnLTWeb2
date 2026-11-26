using HouseRentalAPI.Constants;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Mappings;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;
using HouseRentalAPI.Services;
using Moq;

namespace HouseRentalAPI.Tests.Services;

/// <summary>"Bản tin" trên trang cá nhân: ai được thấy tin ở trạng thái nào</summary>
public class PropertyPostsTests
{
    private const int LandlordId = 20;

    private readonly Mock<IPropertyRepository> _properties = new();

    private PropertyService CreateService(int? userId, string role) =>
        new(_properties.Object, new Mock<IBookingRepository>().Object, new Mock<IFavoriteRepository>().Object,
            new Mock<INotificationService>().Object, TestHelpers.CurrentUser(userId, role).Object);

    private void SetupPosts(params Property[] items) =>
        _properties.Setup(r => r.GetPostsByLandlordAsync(LandlordId, It.IsAny<IReadOnlyCollection<PropertyStatus>?>(), 1, 10))
            .ReturnsAsync(new PagedResult<Property> { Items = items, Page = 1, PageSize = 10, TotalCount = items.Length });

    private static Property Rejected() => new()
    {
        Id = 1, Title = "Phòng trọ", Description = "Mô tả", LandlordId = LandlordId,
        Status = PropertyStatus.Rejected, RejectReason = "Ảnh mờ"
    };

    [Fact]
    public async Task NguoiLa_ChiThayTinDaDuyetHoacDaChoThue_VaKhongThayLyDoTuChoi()
    {
        SetupPosts(Rejected());

        var result = await CreateService(99, AppRoles.Tenant).GetPostsAsync(LandlordId, new PaginationQuery());

        _properties.Verify(r => r.GetPostsByLandlordAsync(LandlordId,
            It.Is<IReadOnlyCollection<PropertyStatus>?>(s => s != null && s.Count == 2 &&
                s.Contains(PropertyStatus.Available) && s.Contains(PropertyStatus.Rented)), 1, 10));
        Assert.Null(result.Items[0].RejectReason);
    }

    [Fact]
    public async Task KhachChuaDangNhap_CungChiThayTinCongKhai()
    {
        SetupPosts();
        await CreateService(null, AppRoles.Tenant).GetPostsAsync(LandlordId, new PaginationQuery());

        _properties.Verify(r => r.GetPostsByLandlordAsync(LandlordId,
            It.Is<IReadOnlyCollection<PropertyStatus>?>(s => s != null && !s.Contains(PropertyStatus.PendingApproval)), 1, 10));
    }

    [Theory]
    [InlineData(LandlordId, AppRoles.Landlord)]
    [InlineData(1, AppRoles.Admin)]
    public async Task ChinhChuHoacAdmin_ThayMoiTrangThai_KemLyDoTuChoi(int userId, string role)
    {
        SetupPosts(Rejected());

        var result = await CreateService(userId, role).GetPostsAsync(LandlordId, new PaginationQuery());

        _properties.Verify(r => r.GetPostsByLandlordAsync(LandlordId, null, 1, 10));
        Assert.Equal("Ảnh mờ", result.Items[0].RejectReason);
    }

    [Fact]
    public async Task DuyetTinLanDau_GanThoiDiemLenBanTin_AnRoiHienLai_KhongDoi()
    {
        var property = new Property { Id = 7, Title = "Tin mới", LandlordId = LandlordId, Landlord = new User { Id = LandlordId, FullName = "Chủ nhà" }, Status = PropertyStatus.PendingApproval };
        _properties.Setup(r => r.GetDetailAsync(7)).ReturnsAsync(property);

        await CreateService(1, AppRoles.Admin).UpdateStatusAsync(7, PropertyStatus.Available, null);
        var publishedAt = property.PublishedAt;
        Assert.NotNull(publishedAt);

        await CreateService(LandlordId, AppRoles.Landlord).UpdateStatusAsync(7, PropertyStatus.Hidden, null);
        await CreateService(LandlordId, AppRoles.Landlord).UpdateStatusAsync(7, PropertyStatus.Available, null);
        Assert.Equal(publishedAt, property.PublishedAt);
    }

    [Fact]
    public async Task BanTinChung_TraVeTenChuNha()
    {
        _properties.Setup(r => r.GetFeedAsync(null, 1, 10)).ReturnsAsync(new PagedResult<Property>
        {
            Items = [new Property { Id = 1, Title = "T", Description = "D", LandlordId = LandlordId, Landlord = new User { Id = LandlordId, FullName = "Chủ nhà A" }, Status = PropertyStatus.Available }],
            Page = 1, PageSize = 10, TotalCount = 1
        });

        var result = await CreateService(null, AppRoles.Tenant).GetFeedAsync(null, new PaginationQuery());

        Assert.Equal("Chủ nhà A", result.Items[0].LandlordName);
        Assert.Null(result.Items[0].RejectReason);
    }

    [Fact]
    public void DoanMoTaDai_DuocCatOKhoangTrang_VaThemDauBaCham()
    {
        var words = string.Join(' ', Enumerable.Repeat("phòng", 100)); // ~600 ký tự
        var post = new Property { Title = "T", Description = words }.ToPostDto(includeRejectReason: false);

        Assert.True(post.Excerpt.Length <= 281);
        Assert.EndsWith("phòng…", post.Excerpt);
    }

    [Fact]
    public void AnhDaiDien_LuonDungDau_VaToiDa4Anh()
    {
        var property = new Property { Title = "T", Description = "D" };
        for (var i = 1; i <= 6; i++)
            property.Images.Add(new PropertyImage { Id = i, Url = $"/img/{i}.jpg", IsThumbnail = i == 5 });

        var post = property.ToPostDto(includeRejectReason: false);

        Assert.Equal("/img/5.jpg", post.ImageUrls[0]);
        Assert.Equal(4, post.ImageUrls.Count);
        Assert.Equal(6, post.ImageCount);
    }
}
