using System.ComponentModel.DataAnnotations;

namespace ClinicScheduler.Web.Contracts.TherapyTypes;

/// <summary>
/// Request model used to update an existing therapy type.
/// </summary>
public sealed class UpdateTherapyTypeRequest
{
    /// <summary>
    /// Updated display name for the therapy type.
    /// </summary>
    /// <example>Cognitive Behavioral Therapy</example>
    [Required]
    [StringLength(100)]
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Optional updated description.
    /// </summary>
    /// <example>Short-term, goal-oriented psychotherapy for treating anxiety and depression.</example>
    [StringLength(1000)]
    public string? Description { get; init; }
    
    /// <summary>
    /// Optional updated specialty category.
    /// </summary>
    /// <example>Mental Health</example>
    [StringLength(100)]
    public string? Specialty { get; init; }
    
    /// <summary>
    /// Optional updated UI color code in hex format. Must match the pattern '#RGB' or '#RRGGBB'.
    /// </summary>
    /// <example>#FF5733</example>
    [RegularExpression("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")]
    public string? ColorCode { get; init; }
}