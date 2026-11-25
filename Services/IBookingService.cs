using HouseRentalAPI.DTOs.Bookings;
using HouseRentalAPI.DTOs.Common;

namespace HouseRentalAPI.Services;

public interface IBookingService
{
    Task<BookingDto> CreateAsync(CreateBookingDto dto);
    Task<BookingDto> GetByIdAsync(int id);

    /// <summary>Yêu cầu thuê của người thuê đang đăng nhập</summary>
    Task<PagedResult<BookingDto>> GetMineAsync(BookingQueryDto query);

    /// <summary>Yêu cầu thuê gửi tới các nhà của chủ nhà đang đăng nhập</summary>
    Task<PagedResult<BookingDto>> GetReceivedAsync(BookingQueryDto query);

    /// <summary>Admin xem toàn bộ yêu cầu thuê / hợp đồng</summary>
    Task<PagedResult<BookingDto>> GetAllAsync(BookingQueryDto query);

    Task<BookingDto> ApproveAsync(int id);
    Task<BookingDto> RejectAsync(int id, string reason);
    Task<BookingDto> CancelAsync(int id);
    Task<BookingDto> CompleteAsync(int id);
}
