using HouseRentalAPI.Constants;
using HouseRentalAPI.DTOs.Appointments;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Mappings;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;

namespace HouseRentalAPI.Services;

/// <summary>
/// Lịch hẹn xem nhà: Pending -> Confirmed -> Completed
///                   Pending -> Rejected
///                   Pending | Confirmed -> Cancelled
/// </summary>
public class AppointmentService(
    IAppointmentRepository appointmentRepository,
    IPropertyRepository propertyRepository,
    INotificationService notificationService,
    ICurrentUserService currentUser) : IAppointmentService
{
    // Hai lịch hẹn đã xác nhận của cùng căn nhà phải cách nhau ít nhất 30 phút
    private static readonly TimeSpan MinGap = TimeSpan.FromMinutes(30);

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto)
    {
        var tenantId = currentUser.GetRequiredUserId();

        var property = await propertyRepository.GetByIdAsync(dto.PropertyId)
            ?? throw new NotFoundException($"Không tìm thấy nhà có id {dto.PropertyId}");

        if (property.Status != PropertyStatus.Available)
            throw new BadRequestException("Nhà này hiện không nhận lịch hẹn xem");

        if (property.LandlordId == tenantId)
            throw new BadRequestException("Bạn không thể hẹn xem nhà của chính mình");

        var scheduledAt = DateTime.SpecifyKind(dto.ScheduledAt!.Value, DateTimeKind.Utc);
        if (scheduledAt < DateTime.UtcNow.AddHours(1))
            throw new BadRequestException("Thời gian hẹn phải sau thời điểm hiện tại ít nhất 1 giờ");

        if (scheduledAt > DateTime.UtcNow.AddDays(60))
            throw new BadRequestException("Chỉ được hẹn trong vòng 60 ngày tới");

        if (await appointmentRepository.HasActiveAsync(tenantId, property.Id))
            throw new ConflictException("Bạn đã có lịch hẹn xem nhà này đang chờ hoặc đã xác nhận");

        var appointment = new Appointment
        {
            PropertyId = property.Id,
            TenantId = tenantId,
            ScheduledAt = scheduledAt,
            Note = dto.Note?.Trim()
        };

        await appointmentRepository.AddAsync(appointment);
        await appointmentRepository.SaveChangesAsync();

        await notificationService.NotifyAsync(property.LandlordId, NotificationType.Appointment,
            "Có lịch hẹn xem nhà mới",
            $"Có người muốn xem nhà \"{property.Title}\" lúc {AppTime.Format(scheduledAt)}.",
            Link(appointment.Id));
        await appointmentRepository.SaveChangesAsync();

        return (await FindAsync(appointment.Id)).ToDto();
    }

    public async Task<AppointmentDto> GetByIdAsync(int id)
    {
        var appointment = await FindAsync(id);

        var userId = currentUser.UserId;
        if (!currentUser.IsAdmin && appointment.TenantId != userId && appointment.Property.LandlordId != userId)
            throw new ForbiddenException("Bạn không có quyền xem lịch hẹn này");

        return appointment.ToDto();
    }

    public async Task<PagedResult<AppointmentDto>> GetMineAsync(AppointmentQueryDto query)
    {
        var result = await appointmentRepository.GetByTenantAsync(currentUser.GetRequiredUserId(), query);
        return result.Map(a => a.ToDto());
    }

    public async Task<PagedResult<AppointmentDto>> GetReceivedAsync(AppointmentQueryDto query)
    {
        var result = await appointmentRepository.GetByLandlordAsync(currentUser.GetRequiredUserId(), query);
        return result.Map(a => a.ToDto());
    }

    public async Task<AppointmentDto> ConfirmAsync(int id, string? note)
    {
        var appointment = await FindForLandlordAsync(id);
        EnsureStatus(appointment, AppointmentStatus.Pending, "Chỉ có thể xác nhận lịch hẹn đang chờ");

        if (appointment.ScheduledAt < DateTime.UtcNow)
            throw new BadRequestException("Lịch hẹn đã qua thời gian, không thể xác nhận");

        if (await appointmentRepository.HasConflictAsync(appointment.PropertyId, appointment.ScheduledAt, MinGap, appointment.Id))
            throw new ConflictException("Đã có lịch hẹn khác được xác nhận gần thời điểm này");

        appointment.Status = AppointmentStatus.Confirmed;
        appointment.LandlordNote = note?.Trim();

        await notificationService.NotifyAsync(appointment.TenantId, NotificationType.Appointment,
            "Lịch hẹn xem nhà đã được xác nhận",
            $"Chủ nhà đã xác nhận lịch xem \"{appointment.Property.Title}\" lúc {AppTime.Format(appointment.ScheduledAt)}.",
            Link(appointment.Id));
        await appointmentRepository.SaveChangesAsync();

        return appointment.ToDto();
    }

    public async Task<AppointmentDto> RejectAsync(int id, string? note)
    {
        var appointment = await FindForLandlordAsync(id);
        EnsureStatus(appointment, AppointmentStatus.Pending, "Chỉ có thể từ chối lịch hẹn đang chờ");

        appointment.Status = AppointmentStatus.Rejected;
        appointment.LandlordNote = note?.Trim();

        await notificationService.NotifyAsync(appointment.TenantId, NotificationType.Appointment,
            "Lịch hẹn xem nhà bị từ chối",
            $"Chủ nhà không thể đón bạn xem \"{appointment.Property.Title}\" vào thời gian đã hẹn. {appointment.LandlordNote}".TrimEnd(),
            Link(appointment.Id));
        await appointmentRepository.SaveChangesAsync();

        return appointment.ToDto();
    }

    public async Task<AppointmentDto> CancelAsync(int id, string? note)
    {
        var appointment = await FindAsync(id);
        var userId = currentUser.UserId;

        var isTenant = appointment.TenantId == userId;
        var isLandlord = appointment.Property.LandlordId == userId;
        if (!isTenant && !isLandlord && !currentUser.IsAdmin)
            throw new ForbiddenException("Bạn không có quyền hủy lịch hẹn này");

        if (appointment.Status is not (AppointmentStatus.Pending or AppointmentStatus.Confirmed))
            throw new BadRequestException($"Không thể hủy lịch hẹn ở trạng thái {appointment.Status}");

        appointment.Status = AppointmentStatus.Cancelled;
        if (!isTenant)
            appointment.LandlordNote = note?.Trim();

        // Báo cho phía còn lại
        var notifyUserId = isTenant ? appointment.Property.LandlordId : appointment.TenantId;
        await notificationService.NotifyAsync(notifyUserId, NotificationType.Appointment,
            "Lịch hẹn xem nhà đã bị hủy",
            $"Lịch xem \"{appointment.Property.Title}\" lúc {AppTime.Format(appointment.ScheduledAt)} đã bị hủy. {note?.Trim()}".TrimEnd(),
            Link(appointment.Id));
        await appointmentRepository.SaveChangesAsync();

        return appointment.ToDto();
    }

    public async Task<AppointmentDto> CompleteAsync(int id)
    {
        var appointment = await FindForLandlordAsync(id);
        EnsureStatus(appointment, AppointmentStatus.Confirmed, "Chỉ có thể hoàn tất lịch hẹn đã xác nhận");

        appointment.Status = AppointmentStatus.Completed;
        await appointmentRepository.SaveChangesAsync();

        return appointment.ToDto();
    }

    private static string Link(int id) => $"/api/appointments/{id}";

    private async Task<Appointment> FindAsync(int id) =>
        await appointmentRepository.GetDetailAsync(id)
        ?? throw new NotFoundException($"Không tìm thấy lịch hẹn có id {id}");

    private async Task<Appointment> FindForLandlordAsync(int id)
    {
        var appointment = await FindAsync(id);

        if (!currentUser.IsAdmin && appointment.Property.LandlordId != currentUser.UserId)
            throw new ForbiddenException("Bạn không phải chủ của căn nhà này");

        return appointment;
    }

    private static void EnsureStatus(Appointment appointment, AppointmentStatus expected, string message)
    {
        if (appointment.Status != expected)
            throw new BadRequestException(message);
    }
}
