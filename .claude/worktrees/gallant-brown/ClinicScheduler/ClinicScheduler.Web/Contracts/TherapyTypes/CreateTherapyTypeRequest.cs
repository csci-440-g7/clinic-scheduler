using System.ComponentModel.DataAnnotations;

namespace ClinicScheduler.Web.Contracts.TherapyTypes;

/// <summary>
/// Request model used to create a new therapy type.
/// </summary>
public sealed class CreateTherapyTypeRequest
{
    /// <summary>
    /// Display name for the therapy type.
    /// </summary>
    /// <example>Cognitive Behavioral Therapy</example>
    [Required]
    [StringLength(100)]
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Optional longer description of the therapy type (purpose, typical use cases).
    /// </summary>
    /// <example>Short-term, goal-oriented psychotherapy for treating anxiety and depression.</example>
    [StringLength(1000)]
    public string? Description { get; init; }
    
    /// <summary>
    /// Optional specialty category the therapy type belongs to.
    /// </summary>
    /// <example>Mental Health</example>
    [StringLength(100)]
    public string? Specialty { get; init; }
    
    /// <summary>
    /// Optional color code (hex) used in UI to represent this therapy type.
    /// Must be a 3- or 6-digit hex color prefixed with '#'. Example: '#FF5733'.
    /// </summary>
    /// <example>#FF5733</example>
    [RegularExpression("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")]
    public string? ColorCode { get; init; }
}