namespace ClinicScheduler.Core.Entities;

/// <summary>
/// A treatment plan assigned to a patient, specifying frequency and duration of therapy.
/// </summary>
public class TreatmentPlan
{
    public int Id { get; set; }
    
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    
    public int TherapistId { get; set; }
    public Therapist Therapist { get; set; } = null!;
    
    /// <summary>
    /// How many days per week: 2, 3, or 4.
    /// </summary>
    public int FrequencyPerWeek { get; set; }
    
    /// <summary>
    /// Total treatment duration in days: 20, 30, or 50.
    /// </summary>
    public int TotalDays { get; set; }
    
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Many-to-many: a treatment plan can include multiple therapy types
    public ICollection<TreatmentPlanTherapy> TreatmentPlanTherapies { get; set; } = [];
}