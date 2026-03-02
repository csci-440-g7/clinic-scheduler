namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Represents a single scheduled therapy appointment.
/// </summary>
public class Appointment
{
    public int Id { get; set; }
    
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    
    public int TherapistId { get; set; }
    public Therapist Therapist { get; set; } = null!;
    
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;
    
    public int? TreatmentPlanId { get; set; }
    public TreatmentPlan? TreatmentPlan { get; set; }
    
    /// <summary>
    /// Start time of the appointment (30-min slots within 8 AM–5 PM).
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// End time of the appointment.
    /// </summary>
    public DateTime EndTime { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    
    /// <summary>
    /// If true, this appointment has a scheduling conflict.
    /// </summary>
    public bool HasConflict { get; set; }
    
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}