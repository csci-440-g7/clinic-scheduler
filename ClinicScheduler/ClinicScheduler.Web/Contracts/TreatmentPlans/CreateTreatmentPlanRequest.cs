using System.ComponentModel.DataAnnotations;

namespace ClinicScheduler.Web.Contracts.TreatmentPlans;

/// <summary>
/// Request model to create a treatment plan for a patient.
/// </summary>
public sealed class CreateTreatmentPlanRequest
{
    /// <summary>
    /// Identifier of the patient the treatment plan is for.
    /// </summary>
    /// <example>12345</example>
    [Required]
    [Range(1, int.MaxValue)]
    public int PatientId { get; init; }
    
    /// <summary>
    /// Identifier of the therapist who will manage the plan.
    /// </summary>
    /// <example>12345</example>
    [Required]
    [Range(1, int.MaxValue)]
    public int TherapistId { get; init; }
    
    /// <summary>
    /// How many sessions per week (allowed values: 2–4).
    /// </summary>
    /// <example>3</example>
    [Required]
    [Range(2, 4)]
    public int FrequencyPerWeek { get; init; }
    
    /// <summary>
    /// Total number of days the treatment plan spans.
    /// </summary>
    /// <example>30</example>
    [Required]
    [Range(1, 365)]
    public int TotalDays { get; init; }
    
    /// <summary>
    /// Start date for the treatment plan (date only).
    /// </summary>
    /// <example>2024-04-01</example>
    [Required]
    public DateOnly StartDate { get; init; }
    
    /// <summary>
    /// Optional computed or provided end date/time for the plan.
    /// </summary>
    /// <example>2024-05-01T00:00:00Z</example>
    public DateTime? EndDate { get; init; }
    
    /// <summary>
    /// List of therapy type identifiers included in this plan (at least one required).
    /// </summary>
    /// <example>[1,2]</example>
    [MinLength(1)]
    public List<int> TherapyTypeIds { get; init; } = [];
}