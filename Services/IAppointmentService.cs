using HouseRentalAPI.DTOs.Appointments;
using HouseRentalAPI.DTOs.Common;

namespace HouseRentalAPI.Services;

public interface IAppointmentService
{
    Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto);
    Task<AppointmentDto> GetByIdAsync(int id);
    Task<PagedResult<AppointmentDto>> GetMineAsync(AppointmentQueryDto query);
    Task<PagedResult<AppointmentDto>> GetReceivedAsync(AppointmentQueryDto query);
    Task<AppointmentDto> ConfirmAsync(int id, string? note);
    Task<AppointmentDto> RejectAsync(int id, string? note);

    /// <summary>Người thuê hoặc chủ nhà hủy lịch hẹn chưa diễn ra</summary>
    Task<AppointmentDto> CancelAsync(int id, string? note);

    Task<AppointmentDto> CompleteAsync(int id);
}
