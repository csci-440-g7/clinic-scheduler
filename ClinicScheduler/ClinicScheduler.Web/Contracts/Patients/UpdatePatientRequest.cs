using System.ComponentModel.DataAnnotations;

namespace ClinicScheduler.Web.Contracts.Patients;

/// <summary>
/// Request model for updating an existing patient's information.
/// </summary>
public sealed class UpdatePatientRequest
{
    /// <summary>
    /// The patient's updated first name.
    /// </summary>
    /// <example>John</example>
    [Required]
    [StringLength(100)]
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// The patient's updated last name.
    /// </summary>
    /// <example>Doe</example>
    [Required]
    [StringLength(100)]
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// The patient's updated email address.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;
    
    /// <summary>
    /// The patient's updated phone number (optional).
    /// </summary>
    /// <example>555-123-4567</example>
    /// <example>+1-555-123-4567</example>
    [Phone]
    [StringLength(25)]
    public string? Phone { get; init; }
    
    /// <summary>
    /// The patient's updated date of birth.
    /// </summary>
    /// <example>1990-05-15</example>
    [Required]
    public DateOnly DateOfBirth { get; init; }
}