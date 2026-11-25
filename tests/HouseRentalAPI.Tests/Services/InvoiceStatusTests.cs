using HouseRentalAPI.Models;
using HouseRentalAPI.Services;

namespace HouseRentalAPI.Tests.Services;

/// <summary>Trạng thái hóa đơn được tính lại theo số tiền đã trả và hạn thanh toán</summary>
public class InvoiceStatusTests
{
    private static Invoice NewInvoice(decimal total, decimal paid, int dueInDays, InvoiceStatus status = InvoiceStatus.Unpaid) => new()
    {
        TotalAmount = total,
        PaidAmount = paid,
        DueDate = TestHelpers.Today.AddDays(dueInDays),
        Status = status
    };

    [Fact]
    public void ChuaTraDongNao_ConHan_LaUnpaid() =>
        Assert.Equal(InvoiceStatus.Unpaid, InvoiceService.ResolveStatus(NewInvoice(1_000_000, 0, 5)));

    [Fact]
    public void TraMotPhan_ConHan_LaPartiallyPaid() =>
        Assert.Equal(InvoiceStatus.PartiallyPaid, InvoiceService.ResolveStatus(NewInvoice(1_000_000, 400_000, 5)));

    [Fact]
    public void TraDu_LaPaid_DuDaQuaHan() =>
        Assert.Equal(InvoiceStatus.Paid, InvoiceService.ResolveStatus(NewInvoice(1_000_000, 1_000_000, -3)));

    [Fact]
    public void ChuaTraDu_QuaHan_LaOverdue() =>
        Assert.Equal(InvoiceStatus.Overdue, InvoiceService.ResolveStatus(NewInvoice(1_000_000, 200_000, -1)));

    [Fact]
    public void HoaDonDaHuy_GiuNguyenCancelled() =>
        Assert.Equal(InvoiceStatus.Cancelled, InvoiceService.ResolveStatus(NewInvoice(1_000_000, 1_000_000, 5, InvoiceStatus.Cancelled)));
}
