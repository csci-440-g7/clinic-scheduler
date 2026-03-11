namespace ClinicScheduler.Web.Contracts.TherapyTypes;

/// <summary>
/// Data transfer object representing a therapy type returned from the API.
/// </summary>
public sealed class TherapyTypeDto
{
    /// <summary>
    /// Unique identifier for the therapy type.
    /// </summary>
    /// <example>12345</example>
    public int Id { get; init; }

    /// <summary>
    /// Display name for the therapy type.
    /// </summary>
    /// <example>Cognitive Behavioral Therapy</example>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Optional description of the therapy type.
    /// </summary>
    /// <example>Short-term, goal-oriented psychotherapy for treating anxiety and depression.</example>
    public string? Description { get; init; }

    /// <summary>
    /// Optional specialty category.
    /// </summary>
    /// <example>Mental Health</example>
    public string? Specialty { get; init; }

    /// <summary>
    /// Optional UI color code in hex format.
    /// </summary>
    /// <example>#FF5733</example>
    public string? ColorCode { get; init; }

    /// <summary>
    /// Timestamp when the therapy type was created (UTC).
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Timestamp when the therapy type was last updated (UTC).
    /// </summary>
    /// <example>2024-01-20T14:45:00Z</example>
    public DateTime UpdatedAt { get; init; }
}