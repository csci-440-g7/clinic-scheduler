namespace ClinicScheduler.Web.Contracts.TreatmentPlans;

/// <summary>
/// Represents a therapy type item included in a treatment plan.
/// </summary>
public sealed class TreatmentPlanTherapyDto
{
    /// <summary>
    /// Identifier of the therapy type.
    /// </summary>
    /// <example>12345</example>
    public int TherapyTypeId { get; init; }
    /// <summary>
    /// Display name of the therapy type.
    /// </summary>
    /// <example>Cognitive Behavioral Therapy</example>
    public string TherapyTypeName { get; init; } = string.Empty;
    /// <summary>
    /// Optional specialty category for the therapy type.
    /// </summary>
    /// <example>Mental Health</example>
    public string? Specialty { get; init; }
    /// <summary>
    /// Optional UI color code for the therapy type (hex).
    /// </summary>
    /// <example>#FF5733</example>
    public string? ColorCode { get; init; }
}