namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Represents the lifecycle state of an appointment.
/// </summary>
public enum AppointmentStatus
{
    Scheduled,
    Completed,
    Missed,
    Canceled,
    Rescheduled
}