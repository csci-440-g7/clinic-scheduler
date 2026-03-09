namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Represents a physical clinic room in a location.
/// </summary>
public class Room
{
    public int Id { get; set; }
    
    public string Name { get; private set; }
    public int Capacity { get; private set; }
    public string? Description { get; private set; }
    
    public int LocationId { get; private set; }
    public Location Location { get; private set; } = null!;

    public ICollection<Appointment> Appointments { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private Room()
    {
        Name = string.Empty;
    }

    public Room(string name, int capacity, Location location)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Room capacity must be a positive number.");
        }
        
        Name = name;
        Capacity = capacity;
        Location = location;
    }

    public void UpdateDetails(string name, int capacity, string? description)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Room capacity must be a positive number.");
        }

        Name = name;
        Capacity = capacity;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}