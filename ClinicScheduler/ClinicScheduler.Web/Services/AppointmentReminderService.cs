using ClinicScheduler.Core.Entities;
using ClinicScheduler.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicScheduler.Web.Services;

/// <summary>
/// Background service that runs hourly and sends UpcomingAppointment notifications
/// to patients whose appointments start within the next 24 hours.
/// Tracks which appointments have already been notified to avoid duplicates.
/// </summary>
public sealed class AppointmentReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<AppointmentReminderService> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Stagger startup by 30 seconds so the app is fully initialised first
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending appointment reminders");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task SendRemindersAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();

        var now = DateTime.UtcNow;
        var windowEnd = now.AddHours(24);

        var upcoming = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Therapist)
            .Where(a =>
                (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Rescheduled) &&
                a.StartTime > now &&
                a.StartTime <= windowEnd)
            .ToListAsync(ct);

        if (upcoming.Count == 0) return;

        // Collect appointment IDs that already have a reminder to avoid duplicates
        var apptIds = upcoming.Select(a => a.Id).ToList();
        var alreadyNotified = await db.Notifications
            .Where(n => n.Type == NotificationType.UpcomingAppointment &&
                        n.RelatedAppointmentId.HasValue &&
                        apptIds.Contains(n.RelatedAppointmentId.Value))
            .Select(n => n.RelatedAppointmentId!.Value)
            .ToHashSetAsync(ct);

        int sent = 0;
        foreach (var appt in upcoming)
        {
            if (alreadyNotified.Contains(appt.Id)) continue;

            var user = await db.Users.FirstOrDefaultAsync(
                u => u.UserName == appt.Patient!.Email, ct);
            if (user is null) continue;

            var localTime = appt.StartTime.ToLocalTime();
            db.Notifications.Add(new Notification(
                user.Id,
                NotificationType.UpcomingAppointment,
                "Upcoming Appointment Reminder",
                $"You have an appointment with {appt.Therapist?.FullName} on " +
                $"{localTime:ddd, MMM d} at {localTime:h:mm tt}.",
                appt.Id));

            sent++;
        }

        if (sent > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Sent {Count} upcoming appointment reminder(s)", sent);
        }
    }
}
