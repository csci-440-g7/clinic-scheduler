namespace ClinicScheduler.Web.Contracts.TreatmentPlans;

/// <summary>
/// Data transfer object representing a treatment plan returned by the API.
/// </summary>
public sealed class TreatmentPlanDto
{
    /// <summary>
    /// Unique identifier for the treatment plan.
    /// </summary>
    /// <example>`12345</example>
    public int Id { get; init; }
    
    /// <summary>
    /// Patient identifier associated with the plan.
    /// </summary>
    /// <example>12345</example>
    public int PatientId { get; init; }
    
    /// <summary>
    /// Full name of the patient.
    /// </summary>
    /// <example>John Doe</example>
    public string PatientName { get; init; } = string.Empty;
    
    /// <summary>
    /// Therapist identifier assigned to the plan.
    /// </summary>
    /// <example>12345</example>
    public int TherapistId { get; init; }
    
    /// <summary>
    /// Full name of the therapist.
    /// </summary>
    /// <example>John Doe</example>
    public string TherapistName { get; init; } = string.Empty;
    
    /// <summary>
    /// Sessions per week.
    /// </summary>
    /// <example>3</example>
    public int FrequencyPerWeek { get; init; }
    
    /// <summary>
    /// Total duration in days.
    /// </summary>
    /// <example>30</example>
    public int TotalDays { get; init; }
    
    /// <summary>
    /// Start date for the plan.
    /// </summary>
    /// <example>2024-04-01</example>
    public DateOnly StartDate { get; init; }
    
    /// <summary>
    /// Optional end date/time for the plan.
    /// </summary>
    /// <example>2024-05-01T00:00:00Z</example>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// The therapies (therapy types) included in the plan.
    /// </summary>
    public IReadOnlyList<TreatmentPlanTherapyDto> Therapies { get; init; } = [];
    
    /// <summary>
    /// Record creation timestamp (UTC).
    /// </summary>
    /// <example>2024-03-01T09:00:00Z</example>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// Record last updated timestamp (UTC).
    /// </summary>
    /// <example>2024-03-05T12:00:00Z</example>
    public DateTime UpdatedAt { get; init; }
}