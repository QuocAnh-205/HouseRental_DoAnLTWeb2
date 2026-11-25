using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.DTOs.Invoices;

namespace HouseRentalAPI.Services;

public interface IInvoiceService
{
    Task<InvoiceDto> CreateAsync(int bookingId, CreateInvoiceDto dto);
    Task<InvoiceDto> GetByIdAsync(int id);
    Task<IReadOnlyList<InvoiceDto>> GetByBookingAsync(int bookingId);
    Task<PagedResult<InvoiceDto>> GetMineAsync(InvoiceQueryDto query);
    Task<PagedResult<InvoiceDto>> GetReceivedAsync(InvoiceQueryDto query);

    /// <summary>Hủy hóa đơn chưa có khoản thanh toán nào</summary>
    Task<InvoiceDto> CancelAsync(int id);
}
