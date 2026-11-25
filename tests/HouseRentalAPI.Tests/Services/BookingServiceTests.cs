using HouseRentalAPI.Constants;
using HouseRentalAPI.DTOs.Bookings;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;
using HouseRentalAPI.Services;
using Moq;

namespace HouseRentalAPI.Tests.Services;

/// <summary>Luồng thuê nhà: gửi yêu cầu, duyệt, hủy</summary>
public class BookingServiceTests
{
    private const int TenantId = 10;
    private const int LandlordId = 20;

    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IPropertyRepository> _properties = new();
    private readonly Mock<INotificationService> _notifications = new();

    private BookingService CreateService(int userId = TenantId, string role = AppRoles.Tenant) =>
        new(_bookings.Object, _properties.Object, _notifications.Object, TestHelpers.CurrentUser(userId, role).Object);

    private Property SetupProperty(PropertyStatus status = PropertyStatus.Available)
    {
        var property = new Property
        {
            Id = 1, Title = "Phòng trọ test", LandlordId = LandlordId,
            PricePerMonth = 2_000_000, Deposit = 2_000_000, Status = status
        };
        _properties.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(property);
        return property;
    }

    private static CreateBookingDto Request(int startInDays = 3, int months = 6) =>
        new() { PropertyId = 1, StartDate = TestHelpers.Today.AddDays(startInDays), DurationMonths = months };

    [Fact]
    public async Task Create_NhaKhongTonTai_NemNotFound()
    {
        _properties.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Property?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CreateAsync(Request()));
    }

    [Theory]
    [InlineData(PropertyStatus.Rented)]
    [InlineData(PropertyStatus.Hidden)]
    [InlineData(PropertyStatus.PendingApproval)]
    public async Task Create_NhaKhongSanSangChoThue_NemBadRequest(PropertyStatus status)
    {
        SetupProperty(status);
        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(Request()));
    }

    [Fact]
    public async Task Create_ThueNhaCuaChinhMinh_NemBadRequest()
    {
        SetupProperty();
        await Assert.ThrowsAsync<BadRequestException>(() => CreateService(userId: LandlordId).CreateAsync(Request()));
    }

    [Fact]
    public async Task Create_NgayBatDauOQuaKhu_NemBadRequest()
    {
        SetupProperty();
        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(Request(startInDays: -1)));
    }

    [Fact]
    public async Task Create_TrungLichHopDongKhac_NemConflict()
    {
        SetupProperty();
        _bookings.Setup(r => r.HasOverlapAsync(1, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), null)).ReturnsAsync(true);
        await Assert.ThrowsAsync<ConflictException>(() => CreateService().CreateAsync(Request()));
    }

    [Fact]
    public async Task Create_HopLe_ChotGia_TinhTongTien_VaBaoChuNha()
    {
        var property = SetupProperty();
        Booking? saved = null;
        _bookings.Setup(r => r.AddAsync(It.IsAny<Booking>()))
            .Callback<Booking>(b => { b.Id = 99; saved = b; })
            .Returns(Task.CompletedTask);
        _bookings.Setup(r => r.GetDetailAsync(99)).ReturnsAsync(() =>
        {
            saved!.Property = property;
            return saved;
        });

        var result = await CreateService().CreateAsync(Request(months: 6));

        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Equal(12_000_000, result.TotalAmount);
        Assert.Equal(saved!.StartDate.AddMonths(6), result.EndDate);
        _notifications.Verify(n => n.NotifyAsync(LandlordId, NotificationType.Booking,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Approve_KhongPhaiChuNha_NemForbidden()
    {
        var booking = new Booking
        {
            Id = 5, TenantId = TenantId, Status = BookingStatus.Pending,
            Property = new Property { LandlordId = LandlordId }
        };
        _bookings.Setup(r => r.GetDetailAsync(5)).ReturnsAsync(booking);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateService(userId: 999, role: AppRoles.Landlord).ApproveAsync(5));
    }

    [Fact]
    public async Task Approve_HopLe_NhaChuyenSangDaThue_VaTuChoiCacYeuCauKhac()
    {
        var property = new Property { Id = 1, Title = "Nhà", LandlordId = LandlordId, Status = PropertyStatus.Available };
        var booking = new Booking
        {
            Id = 5, PropertyId = 1, TenantId = TenantId, Status = BookingStatus.Pending,
            StartDate = TestHelpers.Today.AddDays(3), EndDate = TestHelpers.Today.AddMonths(6),
            Property = property, Tenant = new User()
        };
        var other = new Booking { Id = 6, PropertyId = 1, TenantId = 11, Status = BookingStatus.Pending };
        _bookings.Setup(r => r.GetDetailAsync(5)).ReturnsAsync(booking);
        _bookings.Setup(r => r.GetPendingByPropertyAsync(1, 5)).ReturnsAsync([other]);

        var result = await CreateService(userId: LandlordId, role: AppRoles.Landlord).ApproveAsync(5);

        Assert.Equal(BookingStatus.Approved, result.Status);
        Assert.Equal(PropertyStatus.Rented, property.Status);
        Assert.Equal(BookingStatus.Rejected, other.Status);
    }

    [Theory]
    [InlineData(PropertyStatus.Hidden)]
    [InlineData(PropertyStatus.Rejected)]
    [InlineData(PropertyStatus.PendingApproval)]
    public async Task Approve_TinKhongConAvailable_NemBadRequest_VaGiuNguyenTrangThaiNha(PropertyStatus status)
    {
        // Admin đã ẩn tin (vd: báo cáo lừa đảo) sau khi khách gửi yêu cầu → chủ nhà không được duyệt để "lách" lệnh ẩn
        var property = new Property { Id = 1, LandlordId = LandlordId, Status = status };
        var booking = new Booking
        {
            Id = 5, PropertyId = 1, TenantId = TenantId, Status = BookingStatus.Pending,
            StartDate = TestHelpers.Today.AddDays(3), EndDate = TestHelpers.Today.AddMonths(6),
            Property = property, Tenant = new User()
        };
        _bookings.Setup(r => r.GetDetailAsync(5)).ReturnsAsync(booking);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateService(userId: LandlordId, role: AppRoles.Landlord).ApproveAsync(5));
        Assert.Equal(status, property.Status);
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Fact]
    public async Task Approve_YeuCauDaQuaNgayKetThuc_NemBadRequest()
    {
        var booking = new Booking
        {
            Id = 5, PropertyId = 1, TenantId = TenantId, Status = BookingStatus.Pending,
            StartDate = TestHelpers.Today.AddMonths(-2), EndDate = TestHelpers.Today,
            Property = new Property { Id = 1, LandlordId = LandlordId, Status = PropertyStatus.Available },
            Tenant = new User()
        };
        _bookings.Setup(r => r.GetDetailAsync(5)).ReturnsAsync(booking);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateService(userId: LandlordId, role: AppRoles.Landlord).ApproveAsync(5));
    }

    [Fact]
    public async Task Cancel_HopDongDaBatDau_NemBadRequest()
    {
        var booking = new Booking
        {
            Id = 7, TenantId = TenantId, Status = BookingStatus.Approved,
            StartDate = TestHelpers.Today.AddDays(-10),
            Property = new Property { LandlordId = LandlordId }, Tenant = new User()
        };
        _bookings.Setup(r => r.GetDetailAsync(7)).ReturnsAsync(booking);

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CancelAsync(7));
    }

    [Theory]
    [InlineData(PropertyStatus.Rented, PropertyStatus.Available)]
    [InlineData(PropertyStatus.Hidden, PropertyStatus.Hidden)]
    public async Task Cancel_HopDongChuaBatDau_ChiMoLaiNhaNeuDangRented_KhongMoTinDaBiAn(
        PropertyStatus currentPropertyStatus, PropertyStatus expectedPropertyStatus)
    {
        var property = new Property { Id = 1, Title = "Nhà", LandlordId = LandlordId, Status = currentPropertyStatus };
        var booking = new Booking
        {
            Id = 8, TenantId = TenantId, Status = BookingStatus.Approved,
            StartDate = TestHelpers.Today.AddDays(5),
            Property = property, Tenant = new User { FullName = "Người thuê" }
        };
        _bookings.Setup(r => r.GetDetailAsync(8)).ReturnsAsync(booking);

        var result = await CreateService().CancelAsync(8);

        Assert.Equal(BookingStatus.Cancelled, result.Status);
        Assert.Equal(expectedPropertyStatus, property.Status);
    }
}
