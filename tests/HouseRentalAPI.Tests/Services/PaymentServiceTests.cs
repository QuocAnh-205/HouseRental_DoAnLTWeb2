using System.Linq.Expressions;
using HouseRentalAPI.Constants;
using HouseRentalAPI.DTOs.Payments;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;
using HouseRentalAPI.Services;
using Moq;

namespace HouseRentalAPI.Tests.Services;

/// <summary>Người thuê tạo phiếu thanh toán, chủ nhà xác nhận</summary>
public class PaymentServiceTests
{
    private const int TenantId = 10;
    private const int LandlordId = 20;

    private readonly Mock<IPaymentRepository> _payments = new();
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IInvoiceRepository> _invoices = new();
    private readonly Mock<INotificationService> _notifications = new();

    private PaymentService CreateService(int userId, string role) =>
        new(_payments.Object, _bookings.Object, _invoices.Object, _notifications.Object,
            TestHelpers.CurrentUser(userId, role).Object);

    private static Booking ApprovedBooking() => new()
    {
        Id = 1, TenantId = TenantId, Status = BookingStatus.Approved, Deposit = 2_000_000,
        Property = new Property { Title = "Nhà", LandlordId = LandlordId },
        Tenant = new User { FullName = "Người thuê" }
    };

    [Fact]
    public async Task Create_ThanhToanHopDongCuaNguoiKhac_NemForbidden()
    {
        _bookings.Setup(r => r.GetDetailAsync(1)).ReturnsAsync(ApprovedBooking());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateService(userId: 999, AppRoles.Tenant).CreateAsync(1, new CreatePaymentDto { Amount = 100_000 }));
    }

    [Fact]
    public async Task Create_VuotSoTienConLaiCuaHoaDon_NemBadRequest()
    {
        _bookings.Setup(r => r.GetDetailAsync(1)).ReturnsAsync(ApprovedBooking());
        _invoices.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(
            new Invoice { Id = 3, BookingId = 1, TotalAmount = 1_000_000, PaidAmount = 800_000 });

        await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateService(TenantId, AppRoles.Tenant).CreateAsync(1, new CreatePaymentDto { Amount = 500_000, InvoiceId = 3 }));
    }

    [Fact]
    public async Task XacNhan_CongDonVaoHoaDon_VaHoaDonThanhPaid()
    {
        var invoice = new Invoice { Id = 3, TotalAmount = 1_000_000, PaidAmount = 600_000, DueDate = TestHelpers.Today.AddDays(5) };
        var payment = new Payment { Id = 8, Amount = 400_000, Status = PaymentStatus.Pending, Invoice = invoice, Booking = ApprovedBooking() };
        _payments.Setup(r => r.GetDetailAsync(8)).ReturnsAsync(payment);

        await CreateService(LandlordId, AppRoles.Landlord).UpdateStatusAsync(8, PaymentStatus.Success);

        Assert.Equal(PaymentStatus.Success, payment.Status);
        Assert.Equal(1_000_000, invoice.PaidAmount);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.NotNull(payment.PaidAt);
    }

    [Fact]
    public async Task XacNhan_VuotSoConPhaiTra_NemBadRequest_VaKhongDoiHoaDon()
    {
        // Hai phiếu Pending cho cùng hóa đơn: phiếu thứ hai không được làm PaidAmount vượt TotalAmount
        var invoice = new Invoice { Id = 3, TotalAmount = 1_000_000, PaidAmount = 1_000_000, Status = InvoiceStatus.Paid };
        var payment = new Payment { Id = 9, Amount = 1_000_000, Status = PaymentStatus.Pending, Invoice = invoice, Booking = ApprovedBooking() };
        _payments.Setup(r => r.GetDetailAsync(9)).ReturnsAsync(payment);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateService(LandlordId, AppRoles.Landlord).UpdateStatusAsync(9, PaymentStatus.Success));

        Assert.Equal(1_000_000, invoice.PaidAmount);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Theory]
    [InlineData(PaymentStatus.Failed, PaymentStatus.Success)]
    [InlineData(PaymentStatus.Refunded, PaymentStatus.Success)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Refunded)]
    public async Task ChuyenTrangThaiKhongHopLe_NemBadRequest(PaymentStatus from, PaymentStatus to)
    {
        var payment = new Payment { Id = 8, Amount = 100_000, Status = from, Booking = ApprovedBooking() };
        _payments.Setup(r => r.GetDetailAsync(8)).ReturnsAsync(payment);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateService(LandlordId, AppRoles.Landlord).UpdateStatusAsync(8, to));
    }

    [Fact]
    public async Task XacNhan_KhongPhaiChuNha_NemForbidden()
    {
        var payment = new Payment { Id = 8, Amount = 100_000, Status = PaymentStatus.Pending, Booking = ApprovedBooking() };
        _payments.Setup(r => r.GetDetailAsync(8)).ReturnsAsync(payment);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateService(TenantId, AppRoles.Tenant).UpdateStatusAsync(8, PaymentStatus.Success));
    }

    /// <summary>Giả lập bảng Payments trong CSDL để AnyAsync(predicate) chạy thật trên dữ liệu</summary>
    private void SetupExistingPayments(params Payment[] existing) =>
        _payments.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
            .ReturnsAsync((Expression<Func<Payment, bool>> predicate) => existing.Any(predicate.Compile()));

    [Fact]
    public async Task Create_DaCoPhieuCocDangCho_NemConflict()
    {
        // Khách bấm gửi phiếu cọc 2 lần khi phiếu đầu còn Pending
        _bookings.Setup(r => r.GetDetailAsync(1)).ReturnsAsync(ApprovedBooking());
        SetupExistingPayments(new Payment { Id = 1, BookingId = 1, Type = PaymentType.Deposit, Status = PaymentStatus.Pending });

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService(TenantId, AppRoles.Tenant).CreateAsync(1,
                new CreatePaymentDto { Amount = 2_000_000, Type = PaymentType.Deposit }));
    }

    [Fact]
    public async Task Create_TienCocVuotMucCocHopDong_NemBadRequest()
    {
        _bookings.Setup(r => r.GetDetailAsync(1)).ReturnsAsync(ApprovedBooking());
        SetupExistingPayments();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateService(TenantId, AppRoles.Tenant).CreateAsync(1,
                new CreatePaymentDto { Amount = 5_000_000, Type = PaymentType.Deposit }));
    }

    [Fact]
    public async Task XacNhan_PhieuCocThuHai_KhiDaCoPhieuCocSuccess_NemConflict()
    {
        var payment = new Payment { Id = 9, BookingId = 1, Amount = 2_000_000, Type = PaymentType.Deposit, Status = PaymentStatus.Pending, Booking = ApprovedBooking() };
        _payments.Setup(r => r.GetDetailAsync(9)).ReturnsAsync(payment);
        SetupExistingPayments(
            new Payment { Id = 8, BookingId = 1, Type = PaymentType.Deposit, Status = PaymentStatus.Success },
            payment);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService(LandlordId, AppRoles.Landlord).UpdateStatusAsync(9, PaymentStatus.Success));
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }
}
