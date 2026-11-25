using HouseRentalAPI.Data;
using HouseRentalAPI.DTOs.Appointments;
using HouseRentalAPI.DTOs.Common;
using HouseRentalAPI.Extensions;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Repositories;

public class AppointmentRepository(AppDbContext context) : Repository<Appointment>(context), IAppointmentRepository
{
    public Task<Appointment?> GetDetailAsync(int id) =>
        DbSet
            .Include(a => a.Property)
            .Include(a => a.Tenant)
            .FirstOrDefaultAsync(a => a.Id == id);

    public Task<PagedResult<Appointment>> GetByTenantAsync(int tenantId, AppointmentQueryDto query) =>
        Filter(DbSet.Where(a => a.TenantId == tenantId), query.Status)
            .ToPagedResultAsync(query.Page, query.PageSize);

    public Task<PagedResult<Appointment>> GetByLandlordAsync(int landlordId, AppointmentQueryDto query) =>
        Filter(DbSet.Where(a => a.Property.LandlordId == landlordId), query.Status)
            .ToPagedResultAsync(query.Page, query.PageSize);

    public Task<bool> HasActiveAsync(int tenantId, int propertyId) =>
        DbSet.AnyAsync(a =>
            a.TenantId == tenantId &&
            a.PropertyId == propertyId &&
            (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) &&
            a.ScheduledAt > DateTime.UtcNow);

    public Task<bool> HasConflictAsync(int propertyId, DateTime scheduledAt, TimeSpan window, int? excludeId = null)
    {
        var from = scheduledAt - window;
        var to = scheduledAt + window;
        return DbSet.AnyAsync(a =>
            a.PropertyId == propertyId &&
            a.Status == AppointmentStatus.Confirmed &&
            (excludeId == null || a.Id != excludeId) &&
            a.ScheduledAt > from &&
            a.ScheduledAt < to);
    }

    private static IQueryable<Appointment> Filter(IQueryable<Appointment> appointments, AppointmentStatus? status)
    {
        if (status.HasValue)
            appointments = appointments.Where(a => a.Status == status.Value);

        return appointments
            .AsNoTracking()
            .Include(a => a.Property)
            .Include(a => a.Tenant)
            .OrderByDescending(a => a.ScheduledAt);
    }
}
