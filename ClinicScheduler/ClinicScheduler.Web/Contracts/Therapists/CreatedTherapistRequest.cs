using System.ComponentModel.DataAnnotations;

namespace ClinicScheduler.Web.Contracts.Therapists;

/// <summary>
/// Request model for creating a new therapist in the clinic system.
/// </summary>
public sealed class CreatedTherapistRequest
{
    /// <summary>
    /// The therapist's first name.
    /// </summary>
    /// <example>John</example>
    [Required]
    [StringLength(100)]
    public string FirstName { get; init; } = string.Empty;
    
    /// <summary>
    /// The therapist's last name.
    /// </summary>
    /// <example>Doe</example>
    [Required]
    [StringLength(100)]
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// The therapist's email address for contact and account purposes.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;
    
    /// <summary>
    /// The therapist's phone number (optional).
    /// </summary>
    /// <example>555-123-4567</example>
    /// <example>+1-555-123-4567</example>
    [Phone]
    [StringLength(25)]
    public string? Phone { get; init; }
    
    /// <summary>
    /// The therapist's area of specialty (optional).
    /// </summary>
    /// <example>Physical Therapy</example>
    [StringLength(100)]
    public string? Specialty { get; init; }
}