using ClinicScheduler.Core.Entities;

namespace ClinicScheduler.Web.Contracts.Appointments;

/// <summary>
/// Represents an appointment returned by the API.
/// </summary>
public sealed class AppointmentDto
{
    /// <summary>
    /// Unique identifier for the appointment.
    /// </summary>
    /// <example>12345</example>
    public int Id { get; init; }

    /// <summary>
    /// Identifier of the patient for this appointment.
    /// </summary>
    /// <example>12345</example>
    public int PatientId { get; init; }

    /// <summary>
    /// Full name of the patient.
    /// </summary>
    /// <example>John Doe</example>
    public string PatientName { get; init; } = string.Empty;
    
    /// <summary>
    /// Identifier of the therapist for this appointment.
    /// </summary>
    /// <example>12345</example>
    public int TherapistId { get; init; }

    /// <summary>
    /// Full name of the therapist.
    /// </summary>
    /// <example>John Doe</example>
    public string TherapistName { get; init; } = string.Empty;
    
    /// <summary>
    /// Identifier of the room reserved for the appointment.
    /// </summary>
    /// <example>12345</example>
    public int RoomId { get; init; }

    /// <summary>
    /// Human-friendly name of the room.
    /// </summary>
    /// <example>Exam Room A</example>
    public string RoomName { get; init; } = string.Empty;
    
    /// <summary>
    /// Optional associated treatment plan identifier.
    /// </summary>
    /// <example>12</example>
    public int? TreatmentPlanId { get; init; }
    
    /// <summary>
    /// Appointment start time (UTC).
    /// </summary>
    /// <example>2024-03-01T09:00:00Z</example>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// Appointment end time (UTC).
    /// </summary>
    /// <example>2024-03-01T09:30:00Z</example>
    public DateTime EndTime { get; init; }
    
    /// <summary>
    /// Current status of the appointment. See AppointmentStatus enum for possible values.
    /// </summary>
    /// <example>Scheduled</example>
    public AppointmentStatus Status { get; init; }

    /// <summary>
    /// Indicates whether this appointment overlaps/conflicts with another.
    /// </summary>
    /// <example>false</example>
    public bool HasConflict { get; init; }

    /// <summary>
    /// Optional free-form notes for the appointment.
    /// </summary>
    /// <example>Patient prefers morning appointments.</example>
    public string? Notes { get; init; }
    
    /// <summary>
    /// Record creation timestamp (UTC).
    /// </summary>
    /// <example>2024-02-20T14:00:00Z</example>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Record last updated timestamp (UTC).
    /// </summary>
    /// <example>2024-02-21T08:30:00Z</example>
    public DateTime UpdatedAt { get; init; }
}