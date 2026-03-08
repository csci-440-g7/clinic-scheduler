using System.ComponentModel.DataAnnotations;

namespace ClinicScheduler.Web.Contracts.Appointments;

/// <summary>
/// Request model for creating a new appointment.
/// </summary>
public sealed class CreateAppointmentRequest
{
    /// <summary>
    /// Identifier of the patient for the appointment.
    /// </summary>
    /// <example>12345</example>
    [Required]
    [Range(1, int.MaxValue)]
    public int PatientId { get; init; }
    
    /// <summary>
    /// Identifier of the therapist for the appointment.
    /// </summary>
    /// <example>12345</example>
    [Required]
    [Range(1, int.MaxValue)]
    public int TherapistId { get; init; }
    
    /// <summary>
    /// Identifier of the room to reserve for the appointment.
    /// </summary>
    /// <example>12345</example>
    [Required]
    [Range(1, int.MaxValue)]
    public int RoomId { get; init; }
    
    /// <summary>
    /// Optional associated treatment plan identifier.
    /// </summary>
    /// <example>12</example>
    [Range(1, int.MaxValue)]
    public int? TreatmentPlanId { get; init; }
    
    /// <summary>
    /// Appointment start time (UTC).
    /// </summary>
    /// <example>2024-03-01T09:00:00Z</example>
    [Required]
    public DateTime StartTime { get; init; }
    
    /// <summary>
    /// Appointment end time (UTC).
    /// </summary>
    /// <example>2024-03-01T09:30:00Z</example>
    [Required]
    public DateTime EndTime { get; init; }
    
    /// <summary>
    /// Optional notes about the appointment.
    /// </summary>
    /// <example>Bring previous MRI reports.</example>
    [StringLength(2000)]
    public string? Notes { get; init; }
}