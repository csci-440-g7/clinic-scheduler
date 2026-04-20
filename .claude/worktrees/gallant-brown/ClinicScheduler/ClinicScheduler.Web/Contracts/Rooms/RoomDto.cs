namespace ClinicScheduler.Web.Contracts.Rooms;

/// <summary>
/// Represents a room's information returned from the API.
/// </summary>
public sealed class RoomDto
{
    /// <summary>
    /// Unique identifier for the room.
    /// </summary>
    /// <example>12345</example>
    public int Id { get; init; }
    
    /// <summary>
    /// Room name shown to users.
    /// </summary>
    /// <example>Exam Room A</example>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Maximum capacity of the room.
    /// </summary>
    /// <example>4</example>
    public int Capacity { get; init; }
    
    /// <summary>
    /// Optional description of the room.
    /// </summary>
    /// <example>Equipped with examination table and sink</example>
    public string? Description { get; init; }
    
    /// <summary>
    /// The ID of the location this room belongs to.
    /// </summary>
    /// <example>12345</example>
    public int LocationId { get; init; }
    
    /// <summary>
    /// Optional human-readable name of the location.
    /// </summary>
    /// <example>Downtown Medical Center</example>
    public string? LocationName { get; init; }
}