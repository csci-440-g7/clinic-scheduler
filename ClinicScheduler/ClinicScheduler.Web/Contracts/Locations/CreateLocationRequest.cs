using System.ComponentModel.DataAnnotations;

namespace ClinicScheduler.Web.Contracts.Locations;

/// <summary>
/// Request model for creating a new clinic location.
/// </summary>
public sealed class CreateLocationRequest
{
    /// <summary>
    /// The name of the clinic location.
    /// </summary>
    /// <example>Downtown Medical Center</example>
    [Required]
    [StringLength(150)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The street address of the location.
    /// </summary>
    /// <example>123 Main Street</example>
    [Required]
    [StringLength(250)]
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// The city where the location is situated (optional).
    /// </summary>
    /// <example>Dallas</example>
    [StringLength(100)]
    public string? City { get; init; }

    /// <summary>
    /// The state or province where the location is situated (optional).
    /// </summary>
    /// <example>TX</example>
    [StringLength(100)]
    public string? State { get; init; }

    /// <summary>
    /// The postal/ZIP code for the location (optional).
    /// </summary>
    /// <example>75001</example>
    [StringLength(20)]
    public string? ZipCode { get; init; }

    /// <summary>
    /// The IANA time zone identifier for the location (optional).
    /// </summary>
    /// <example>America/Chicago</example>
    [StringLength(100)]
    public string? TimeZone { get; init; }
}