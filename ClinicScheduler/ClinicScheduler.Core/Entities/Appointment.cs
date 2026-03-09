namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Represents a single scheduled therapy appointment.
/// </summary>
public class Appointment
{
    public int Id { get; set; }
    
    public int PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    
    public int TherapistId { get; private set; }
    public Therapist Therapist { get; private set; } = null!;
    
    public int RoomId { get; private set; }
    public Room Room { get; private set; } = null!;
    
    public int? TreatmentPlanId { get; set; }
    public TreatmentPlan? TreatmentPlan { get; set; }
    
    /// <summary>
    /// Start time of the appointment.
    /// </summary>
    public DateTime StartTime { get; private set; }
    
    /// <summary>
    /// End time of the appointment.
    /// </summary>
    public DateTime EndTime { get; private set; }

    public AppointmentStatus Status { get; private set; } = AppointmentStatus.Scheduled;
    
    /// <summary>
    /// If true, this appointment has a scheduling conflict.
    /// </summary>
    public bool HasConflict { get; set; }
    
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private Appointment() { }

    public Appointment(Patient patient, Therapist therapist, Room room, DateTime startTime, TimeSpan duration)
    {
        Patient = patient;
        Therapist = therapist;
        Room = room;
        StartTime = startTime;
        SetDuration(duration);
        Status = AppointmentStatus.Scheduled;
        HasConflict = false; // Initially, assume no conflict. A separate service would detect and set this.
    }

    public void SetDuration(TimeSpan duration)
    {
        if (duration.TotalMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Appointment duration must be positive.");
        }
        EndTime = StartTime.Add(duration);
    }

    public void Cancel()
    {
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Canceled)
        {
            throw new InvalidOperationException($"Cannot cancel an appointment that is already {Status}.");
        }
        Status = AppointmentStatus.Canceled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != AppointmentStatus.Scheduled)
        {
            throw new InvalidOperationException("Only a scheduled appointment can be marked as completed.");
        }
        Status = AppointmentStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reschedule(DateTime newStartTime)
    {
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Canceled)
        {
            throw new InvalidOperationException($"Cannot reschedule an appointment that is {Status}.");
        }
        var duration = EndTime - StartTime;
        StartTime = newStartTime;
        EndTime = newStartTime.Add(duration);
        Status = AppointmentStatus.Rescheduled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsMissed()
    {
        if (Status != AppointmentStatus.Scheduled && Status != AppointmentStatus.Rescheduled)
        {
            throw new InvalidOperationException($"Cannot mark an appointment as missed if its status is {Status}.");
        }
        Status = AppointmentStatus.Missed;
        UpdatedAt = DateTime.UtcNow;
    }
}