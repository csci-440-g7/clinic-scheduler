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

    /// <summary>
    /// The maximum number of patients this location can serve in a single day.
    /// Defaults to 12.
    /// </summary>
    public int DailyCapacity { get; private set; } = 12;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Room> Rooms { get; set; } = [];

    /// <summary>
    /// Navigation property for the configurable time slots at this location.
    /// </summary>
    public ICollection<TimeSlot> TimeSlots { get; set; } = [];

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

    /// <summary>
    /// Sets the daily patient capacity for this location.
    /// </summary>
    /// <param name="capacity">The maximum number of patients per day. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is not a positive integer.</exception>
    public void SetDailyCapacity(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Daily capacity must be a positive integer.");
        }

        DailyCapacity = capacity;
        UpdatedAt = DateTime.UtcNow;
    }
}