using ClinicScheduler.Core.Entities;
using ClinicScheduler.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicScheduler.Web.Services;

/// <summary>
/// Creates in-app notifications for patients when staff perform actions on appointments.
/// Follows the same fire-and-forget-on-error pattern as <see cref="AppointmentReminderService"/>:
/// notification failures are logged but never propagated to the caller.
/// </summary>
public sealed class AppointmentNotificationService
{
    private readonly ClinicDbContext _db;
    private readonly ILogger<AppointmentNotificationService> _logger;

    public AppointmentNotificationService(
        ClinicDbContext db,
        ILogger<AppointmentNotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Creates an <see cref="NotificationType.AppointmentCreated"/> notification for the patient.
    /// </summary>
    public async Task NotifyAppointmentCreatedAsync(Appointment appointment, CancellationToken ct = default)
    {
        try
        {
            await EnsurePatientAndTherapistLoadedAsync(appointment, ct);

            var user = await _db.Users.FirstOrDefaultAsync(
                u => u.UserName == appointment.Patient.Email, ct);
            if (user is null) return;

            var localTime = appointment.StartTime.ToLocalTime();
            var notification = new Notification(
                user.Id,
                NotificationType.AppointmentCreated,
                "New Appointment Scheduled",
                $"An appointment has been scheduled with {appointment.Therapist.FullName} on " +
                $"{localTime:ddd, MMM d} at {localTime:h:mm tt}.",
                appointment.Id);

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment-created notification for appointment {AppointmentId}", appointment.Id);
        }
    }

    /// <summary>
    /// Creates an <see cref="NotificationType.AppointmentRescheduled"/> notification with both old and new times.
    /// </summary>
    public async Task NotifyAppointmentRescheduledAsync(
        Appointment appointment,
        DateTime originalStartTime,
        DateTime originalEndTime,
        CancellationToken ct = default)
    {
        try
        {
            await EnsurePatientAndTherapistLoadedAsync(appointment, ct);

            var user = await _db.Users.FirstOrDefaultAsync(
                u => u.UserName == appointment.Patient.Email, ct);
            if (user is null) return;

            var oldLocal = originalStartTime.ToLocalTime();
            var newLocal = appointment.StartTime.ToLocalTime();
            var notification = new Notification(
                user.Id,
                NotificationType.AppointmentRescheduled,
                "Appointment Rescheduled",
                $"Your appointment has been rescheduled from {oldLocal:ddd, MMM d} at {oldLocal:h:mm tt} " +
                $"to {newLocal:ddd, MMM d} at {newLocal:h:mm tt}.",
                appointment.Id);

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment-rescheduled notification for appointment {AppointmentId}", appointment.Id);
        }
    }

    /// <summary>
    /// Creates a <see cref="NotificationType.CancellationApproved"/> notification for the patient.
    /// </summary>
    public async Task NotifyAppointmentCancelledAsync(Appointment appointment, CancellationToken ct = default)
    {
        try
        {
            await EnsurePatientAndTherapistLoadedAsync(appointment, ct);

            var user = await _db.Users.FirstOrDefaultAsync(
                u => u.UserName == appointment.Patient.Email, ct);
            if (user is null) return;

            var localTime = appointment.StartTime.ToLocalTime();
            var notification = new Notification(
                user.Id,
                NotificationType.CancellationApproved,
                "Appointment Cancelled",
                $"Your appointment with {appointment.Therapist.FullName} on " +
                $"{localTime:ddd, MMM d} at {localTime:h:mm tt} has been cancelled.",
                appointment.Id);

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment-cancelled notification for appointment {AppointmentId}", appointment.Id);
        }
    }

    /// <summary>
    /// Creates an <see cref="NotificationType.AppointmentUpdated"/> notification describing what changed.
    /// Uses a fallback message if <paramref name="changeDescription"/> is null or empty.
    /// </summary>
    public async Task NotifyAppointmentUpdatedAsync(
        Appointment appointment,
        string changeDescription,
        CancellationToken ct = default)
    {
        try
        {
            await EnsurePatientAndTherapistLoadedAsync(appointment, ct);

            var user = await _db.Users.FirstOrDefaultAsync(
                u => u.UserName == appointment.Patient.Email, ct);
            if (user is null) return;

            var description = string.IsNullOrWhiteSpace(changeDescription)
                ? "Your appointment details have been updated."
                : $"Your appointment has been updated: {changeDescription}.";

            var notification = new Notification(
                user.Id,
                NotificationType.AppointmentUpdated,
                "Appointment Updated",
                description,
                appointment.Id);

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment-updated notification for appointment {AppointmentId}", appointment.Id);
        }
    }

    /// <summary>
    /// Ensures the Patient and Therapist navigation properties are loaded on the appointment.
    /// Uses explicit loading via the EF Core change tracker when the entity is already tracked.
    /// </summary>
    private async Task EnsurePatientAndTherapistLoadedAsync(Appointment appointment, CancellationToken ct)
    {
        if (appointment.Patient is null)
        {
            await _db.Entry(appointment).Reference(a => a.Patient).LoadAsync(ct);
        }

        if (appointment.Therapist is null)
        {
            await _db.Entry(appointment).Reference(a => a.Therapist).LoadAsync(ct);
        }
    }
}
