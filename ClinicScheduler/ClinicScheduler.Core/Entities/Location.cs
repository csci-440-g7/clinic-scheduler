namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Represents a physical clinic location.
/// </summary>
public class Location
{
    public int Id { get; set; }
    
    public required string Name { get; set; }
    public required string Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? TimeZone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Room> Rooms { get; set; } = [];
}