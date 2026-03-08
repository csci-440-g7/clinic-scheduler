using System.ComponentModel.DataAnnotations;

namespace ClinicScheduler.Web.Contracts.Rooms;

/// <summary>
/// Request model for creating a new room.
/// </summary>
public sealed class CreateRoomRequest
{
    /// <summary>
    /// The room's display name.
    /// </summary>
    /// <example>Exam Room A</example>
    [Required]
    [StringLength(100)]
    [MinLength(1)]
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Maximum number of occupants the room supports.
    /// </summary>
    /// <example>4</example>
    public int Capacity { get; init; }
    
    /// <summary>
    /// Optional free-form description of the room (purpose, equipment, notes).
    /// </summary>
    /// <example>Equipped with examination table and sink</example>
    [StringLength(500)]
    public string? Description { get; init; }
    
    /// <summary>
    /// Identifier of the location this room belongs to.
    /// </summary>
    /// <example>12345</example>
    [Required]
    [Range(1, int.MaxValue)]
    public int LocationId { get; init; }
}