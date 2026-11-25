using HouseRentalAPI.DTOs.Appointments;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Models;

namespace HouseRentalAPI.Repositories;

public interface IAppointmentRepository : IRepository<Appointment>
{
    /// <summary>Lấy lịch hẹn kèm nhà và người thuê (có tracking để cập nhật)</summary>
    Task<Appointment?> GetDetailAsync(int id);

    Task<PagedResult<Appointment>> GetByTenantAsync(int tenantId, AppointmentQueryDto query);
    Task<PagedResult<Appointment>> GetByLandlordAsync(int landlordId, AppointmentQueryDto query);

    /// <summary>Người thuê đã có lịch hẹn đang chờ / đã xác nhận với căn nhà này</summary>
    Task<bool> HasActiveAsync(int tenantId, int propertyId);

    /// <summary>Căn nhà đã có lịch hẹn được xác nhận trong khoảng ±window quanh thời điểm này</summary>
    Task<bool> HasConflictAsync(int propertyId, DateTime scheduledAt, TimeSpan window, int? excludeId = null);
}
