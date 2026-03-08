namespace ClinicScheduler.Web.Contracts;

/// <summary>
/// Represents a patient's information returned from the API.
/// </summary>
public sealed class PatientDto
{
    /// <summary>
    /// The unique identifier for the patient.
    /// </summary>
    /// <example>12345</example>
    public int Id { get; init; }
    
    /// <summary>
    /// The patient's first name.
    /// </summary>
    /// <example>John</example>
    public string FirstName { get; init; } = string.Empty;
    
    /// <summary>
    /// The patient's last name.
    /// </summary>
    /// <example>Doe</example>
    public string LastName { get; init; } = string.Empty;
    
    /// <summary>
    /// The patient's email address.
    /// </summary>
    /// <example>john.doe@example.com</example>
    public string Email { get; init; } = string.Empty;
    
    /// <summary>
    /// The patient's phone number.
    /// </summary>
    /// <example>555-123-4567</example>
    public string? Phone { get; init; }
    
    /// <summary>
    /// The patient's date of birth.
    /// </summary>
    /// <example>1990-05-15</example>
    public DateOnly DateOfBirth { get; init; }
    
    /// <summary>
    /// The timestamp when the patient record was created.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// The timestamp when the patient record was last updated.
    /// </summary>
    /// <example>2024-01-20T14:45:00Z</example>
    public DateTime UpdatedAt { get; init; }
}