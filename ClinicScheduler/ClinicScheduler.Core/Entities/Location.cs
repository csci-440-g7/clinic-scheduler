namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Represents a physical clinic location.
/// </summary>
public class Location
{
    public int Id { get; set; }
    
    public string Name { get; private set; }
    public string Address { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? ZipCode { get; private set; }
    
    /// <summary>
    /// The IANA Time Zone ID for this location (e.g., "America/Chicago").
    /// Helps in correctly scheduling appointments across different regions.
    /// </summary>
    public string? TimeZone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Room> Rooms { get; set; } = [];

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private Location()
    {
        Name = string.Empty;
        Address = string.Empty;
    }

    public Location(string name, string address)
    {
        Name = name;
        Address = address;
    }

    public void UpdateAddress(string address, string? city, string? state, string? zipCode)
    {
        Address = address;
        City = city;
        State = state;
        ZipCode = zipCode;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string address, string? city, string? state, string? zipCode, string? timeZone)
    {
        Name = name;
        Address = address;
        City = city;
        State = state;
        ZipCode = zipCode;
        TimeZone = timeZone;
        UpdatedAt = DateTime.UtcNow;
    }
}