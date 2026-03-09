namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Represents the lifecycle state of an appointment.
/// </summary>
public enum AppointmentStatus
{
    /// <summary>
    /// The appointment has been created and is scheduled for a future time.
    /// </summary>
    Scheduled,
    /// <summary>
    /// The appointment has been successfully completed by the patient and therapist.
    /// </summary>
    Completed,
    /// <summary>
    /// The patient did not attend the scheduled appointment.
    /// </summary>
    Missed,
    /// <summary>
    /// The appointment was canceled by the patient or staff before it occurred.
    /// </summary>
    Canceled,
    /// <summary>
    /// The appointment's original time was changed to a new time.
    /// </summary>
    Rescheduled
}