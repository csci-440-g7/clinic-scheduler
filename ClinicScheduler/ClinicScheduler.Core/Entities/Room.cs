namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Represents a physical clinic room in a location.
/// </summary>
public class Room
{
    public int Id { get; set; }
    
    public required string Name { get; set; }
    public int Capacity { get; set; }
    public string? Description { get; set; }
    
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public ICollection<Appointment> Appointments { get; set; } = [];
}