namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Represents a patient's request to cancel a scheduled appointment.
/// Staff can review and approve or deny the request.
/// </summary>
public class CancelAppointmentRequest
{
    public int Id { get; private set; }
    public int PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public int AppointmentId { get; private set; }
    public Appointment Appointment { get; private set; } = null!;
    public string Reason { get; private set; } = string.Empty;
    public AppointmentRequestStatus Status { get; private set; } = AppointmentRequestStatus.Pending;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public string? DenialReason { get; private set; }

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private CancelAppointmentRequest() { }

    /// <summary>
    /// Creates a new cancellation request for the given appointment.
    /// </summary>
    /// <param name="patient">The patient requesting cancellation.</param>
    /// <param name="appointment">The appointment to cancel.</param>
    /// <param name="reason">The reason for cancellation.</param>
    /// <exception cref="ArgumentException">Thrown when reason is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when appointment is not in Scheduled or Rescheduled status.</exception>
    public CancelAppointmentRequest(Patient patient, Appointment appointment, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Cancellation reason is required.", nameof(reason));
        }

        if (appointment.Status is not (AppointmentStatus.Scheduled or AppointmentStatus.Rescheduled))
        {
            throw new InvalidOperationException(
                $"Cannot request cancellation for an appointment with status {appointment.Status}. Only Scheduled or Rescheduled appointments can be canceled.");
        }

        Patient = patient;
        PatientId = patient.Id;
        Appointment = appointment;
        AppointmentId = appointment.Id;
        Reason = reason;
        Status = AppointmentRequestStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Approves the cancellation request and cancels the linked appointment.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the request is not in Pending status.</exception>
    public void Approve()
    {
        if (Status != AppointmentRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot approve a cancellation request with status {Status}.");
        }

        Status = AppointmentRequestStatus.Approved;
        Appointment.Cancel();
    }

    /// <summary>
    /// Denies the cancellation request with a reason.
    /// </summary>
    /// <param name="reason">The reason for denial.</param>
    /// <exception cref="ArgumentException">Thrown when reason is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the request is not in Pending status.</exception>
    public void Deny(string reason)
    {
        if (Status != AppointmentRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot deny a cancellation request with status {Status}.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Denial reason is required.", nameof(reason));
        }

        Status = AppointmentRequestStatus.Denied;
        DenialReason = reason;
    }
}
