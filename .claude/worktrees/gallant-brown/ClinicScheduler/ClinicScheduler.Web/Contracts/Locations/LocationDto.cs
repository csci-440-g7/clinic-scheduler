namespace ClinicScheduler.Web.Contracts.Locations;

/// <summary>
/// Represents a clinic location's information returned from the API.
/// </summary>
public sealed class LocationDto
{
    /// <summary>
    /// The unique identifier for the location.
    /// </summary>
    /// <example>12345</example>
    public int Id { get; init; }
    
    /// <summary>
    /// The name of the clinic location.
    /// </summary>
    /// <example>Downtown Medical Center</example>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// The street address of the location.
    /// </summary>
    /// <example>123 Main Street</example>
    public string Address { get; init; } = string.Empty;
    
    /// <summary>
    /// The city where the location is situated.
    /// </summary>
    /// <example>Dallas</example>
    public string? City { get; init; }
    
    /// <summary>
    /// The state or province where the location is situated.
    /// </summary>
    /// <example>TX</example>
    public string? State { get; init; }
    
    /// <summary>
    /// The postal/ZIP code for the location.
    /// </summary>
    /// <example>75001</example>
    public string? ZipCode { get; init; }
    
    /// <summary>
    /// The IANA time zone identifier for the location.
    /// </summary>
    /// <example>America/Chicago</example>
    public string? TimeZone { get; init; }
    
    /// <summary>
    /// The timestamp when the location record was created.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// The timestamp when the location record was last updated.
    /// </summary>
    /// <example>2024-01-20T14:45:00Z</example>
    public DateTime UpdatedAt { get; init; }
}