using ClinicScheduler.Core.Entities;

namespace ClinicScheduler.Web.Contracts.CancelAppointmentRequests;

/// <summary>
/// Represents a cancel appointment request returned by the API.
/// </summary>
public sealed class CancelAppointmentRequestDto
{
    /// <summary>
    /// Unique identifier for the cancel appointment request.
    /// </summary>
    /// <example>1</example>
    public int Id { get; init; }

    /// <summary>
    /// Identifier of the patient who submitted the request.
    /// </summary>
    /// <example>12345</example>
    public int PatientId { get; init; }

    /// <summary>
    /// Full name of the patient.
    /// </summary>
    /// <example>John Doe</example>
    public string PatientName { get; init; } = string.Empty;

    /// <summary>
    /// Identifier of the appointment to cancel.
    /// </summary>
    /// <example>12345</example>
    public int AppointmentId { get; init; }

    /// <summary>
    /// Reason provided by the patient for the cancellation.
    /// </summary>
    /// <example>I have a scheduling conflict and cannot attend.</example>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Current status of the cancellation request.
    /// </summary>
    /// <example>Pending</example>
    public AppointmentRequestStatus Status { get; init; }

    /// <summary>
    /// Timestamp when the request was created (UTC).
    /// </summary>
    /// <example>2024-03-01T09:00:00Z</example>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Reason provided by staff when the request is denied. Null if not denied.
    /// </summary>
    /// <example>The appointment is too close to reschedule.</example>
    public string? DenialReason { get; init; }
}
