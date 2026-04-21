namespace ClinicScheduler.Core.Entities;

public enum NotificationType
{
    MissedAppointment,
    UpcomingAppointment,
    RequestApproved,
    RequestDenied,
    SchedulingConflict,
    AppointmentRescheduled,
    CancellationRequested,
    CancellationApproved,
    CancellationDenied,
    AppointmentCreated,
    AppointmentUpdated
}

public class Notification
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public NotificationType Type { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int? RelatedAppointmentId { get; private set; }

    private Notification() { }

    public Notification(string userId, NotificationType type, string title, string message, int? relatedAppointmentId = null)
    {
        UserId = userId;
        Type = type;
        Title = title;
        Message = message;
        IsRead = false;
        CreatedAt = DateTime.UtcNow;
        RelatedAppointmentId = relatedAppointmentId;
    }

    public void MarkRead() => IsRead = true;
}
