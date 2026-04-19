namespace ClinicScheduler.Web.Contracts.Therapists;

/// <summary>
/// Represents a therapist's information returned from the API.
/// </summary>
public sealed class TherapistDto
{
    /// <summary>
    /// The unique identifier for the therapist.
    /// </summary>
    /// <example>12345</example>
    public int Id { get; init; }
    
    /// <summary>
    /// The therapist's first name.
    /// </summary>
    /// <example>John</example>
    public string FirstName { get; init; } = string.Empty;
    
    /// <summary>
    /// The therapist's last name.
    /// </summary>
    /// <example>Doe</example>
    public string LastName { get; init; } = string.Empty;
    
    /// <summary>
    /// The therapist's email address.
    /// </summary>
    /// <example>john.doe@example.com</example>
    public string Email { get; init; } = string.Empty;
    
    /// <summary>
    /// The therapist's phone number.
    /// </summary>
    /// <example>555-123-4567</example>
    /// <example>+1-555-123-4567</example>
    public string? Phone { get; init; }
    
    /// <summary>
    /// The timestamp when the therapist record was created.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// The timestamp when the therapist record was last updated.
    /// </summary>
    /// <example>2024-01-20T14:45:00Z</example>
    public DateTime UpdatedAt { get; init; }
}