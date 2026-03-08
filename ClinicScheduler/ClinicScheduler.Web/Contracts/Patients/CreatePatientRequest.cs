using System.ComponentModel.DataAnnotations;

namespace ClinicScheduler.Web.Contracts.Patients;

/// <summary>
/// Request model for creating a new patient in the clinic system.
/// </summary>
public sealed class CreatePatientRequest
{
    /// <summary>
    /// The patient's first name.
    /// </summary>
    /// <example>John</example>
    [Required]
    [StringLength(100)]
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// The patient's last name.
    /// </summary>
    /// <example>Doe</example>
    [Required]
    [StringLength(100)]
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// The patient's email address for contact and account purposes.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;
    
    /// <summary>
    /// The patient's phone number (optional).
    /// </summary>
    /// <example>555-123-4567</example>
    /// <example>+1-555-123-4567</example>
    [Phone]
    [StringLength(25)]
    public string? Phone { get; init; }
    
    /// <summary>
    /// The patient's date of birth.
    /// </summary>
    /// <example>1990-05-15</example>
    [Required]
    public DateOnly DateOfBirth { get; init; }
}