using HouseRentalAPI.Constants;
using HouseRentalAPI.Data;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Services;

/// <summary>
/// Tác vụ nền chạy định kỳ:
/// - Kết thúc hợp đồng đã hết hạn và mở lại nhà cho thuê
/// - Đánh dấu hóa đơn quá hạn và nhắc người thuê
/// - Nhắc lịch hẹn xem nhà trong 24 giờ tới
/// - Dọn refresh token hết hạn
/// </summary>
public class RentalMaintenanceService(IServiceScopeFactory scopeFactory, ILogger<RentalMaintenanceService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Đợi app khởi động xong (seed DB...) rồi mới chạy lần đầu
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Rental maintenance failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var today = AppTime.Today;

        // 1. Hợp đồng hết hạn
        var expired = await context.Bookings
            .Include(b => b.Property)
            .Where(b => b.Status == BookingStatus.Approved && b.EndDate <= today)
            .ToListAsync(ct);

        foreach (var booking in expired)
        {
            booking.Status = BookingStatus.Completed;
            if (booking.Property.Status == PropertyStatus.Rented)
                booking.Property.Status = PropertyStatus.Available;

            context.Notifications.AddRange(
                Notify(booking.TenantId, NotificationType.Booking, "Hợp đồng thuê đã hết hạn",
                    $"Hợp đồng thuê \"{booking.Property.Title}\" đã kết thúc ngày {booking.EndDate:dd/MM/yyyy}. Hãy để lại đánh giá nhé!",
                    $"/api/bookings/{booking.Id}"),
                Notify(booking.Property.LandlordId, NotificationType.Booking, "Hợp đồng thuê đã hết hạn",
                    $"Hợp đồng \"{booking.Property.Title}\" đã kết thúc, nhà được mở lại để nhận khách mới.",
                    $"/api/bookings/{booking.Id}"));
        }

        // 2. Hóa đơn quá hạn
        var overdue = await context.Invoices
            .Include(i => i.Booking).ThenInclude(b => b.Property)
            .Where(i => (i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.PartiallyPaid) && i.DueDate < today)
            .ToListAsync(ct);

        foreach (var invoice in overdue)
        {
            invoice.Status = InvoiceStatus.Overdue;
            context.Notifications.Add(Notify(invoice.Booking.TenantId, NotificationType.Invoice,
                $"Hóa đơn tháng {invoice.Month}/{invoice.Year} đã quá hạn",
                $"Hóa đơn \"{invoice.Booking.Property.Title}\" còn {invoice.TotalAmount - invoice.PaidAmount:N0}đ chưa thanh toán (hạn {invoice.DueDate:dd/MM/yyyy}).",
                $"/api/invoices/{invoice.Id}"));
        }

        // 3. Nhắc lịch hẹn trong 24 giờ tới (khung 1 giờ để mỗi lịch chỉ nhắc một lần)
        var remindFrom = now.AddHours(23);
        var remindTo = remindFrom.Add(Interval);
        var upcoming = await context.Appointments
            .Include(a => a.Property)
            .Where(a => a.Status == AppointmentStatus.Confirmed && a.ScheduledAt >= remindFrom && a.ScheduledAt < remindTo)
            .ToListAsync(ct);

        foreach (var appointment in upcoming)
        {
            var message = $"Nhắc lịch: xem nhà \"{appointment.Property.Title}\" lúc {AppTime.Format(appointment.ScheduledAt)}.";
            context.Notifications.AddRange(
                Notify(appointment.TenantId, NotificationType.Appointment, "Lịch xem nhà ngày mai", message, $"/api/appointments/{appointment.Id}"),
                Notify(appointment.Property.LandlordId, NotificationType.Appointment, "Lịch xem nhà ngày mai", message, $"/api/appointments/{appointment.Id}"));
        }

        await context.SaveChangesAsync(ct);

        // 4. Dọn refresh token đã hết hạn / thu hồi quá 30 ngày
        var cutoff = now.AddDays(-30);
        var removedTokens = await context.RefreshTokens
            .Where(t => t.ExpiresAt < now || (t.RevokedAt != null && t.RevokedAt < cutoff))
            .ExecuteDeleteAsync(ct);

        if (expired.Count + overdue.Count + upcoming.Count + removedTokens > 0)
            logger.LogInformation(
                "Maintenance: {Expired} contracts completed, {Overdue} invoices overdue, {Reminders} reminders, {Tokens} tokens removed",
                expired.Count, overdue.Count, upcoming.Count, removedTokens);
    }

    private static Notification Notify(int userId, NotificationType type, string title, string message, string link) =>
        new() { UserId = userId, Type = type, Title = title, Message = message, Link = link };
}
