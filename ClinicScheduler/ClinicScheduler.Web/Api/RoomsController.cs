using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using ClinicScheduler.Web.Contracts.Rooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicScheduler.Web.Api;

/// <summary>Manages treatment room resources.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IRepository<Room> _roomRepository;
    private readonly IRepository<Location> _locationRepository;

    /// <summary>Initializes a new instance of <see cref="RoomsController"/>.</summary>
    public RoomsController(IRepository<Room> roomRepository, IRepository<Location> locationRepository)
    {
        _roomRepository = roomRepository;
        _locationRepository = locationRepository;
    }

    /// <summary>Returns all rooms across all locations.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetAll(CancellationToken ct)
    {
        var rooms = await _roomRepository.GetAllAsync(ct);
        var locations = await _locationRepository.GetAllAsync(ct);
        var locationNames = locations.ToDictionary(x => x.Id, x => x.Name);

        return Ok(rooms.Select(room => MapToDto(room, locationNames)).ToList());
    }

    /// <summary>Returns a single room by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDto>> GetById(int id, CancellationToken ct)
    {
        var room = await _roomRepository.GetByIdAsync(id, ct);
        if (room is null) return NotFound();

        var location = await _locationRepository.GetByIdAsync(room.LocationId, ct);
        return Ok(MapToDto(room, location?.Name));
    }

    /// <summary>Returns all rooms within a specific location.</summary>
    [HttpGet("location/{locationId:int}")]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetByLocation(int locationId, CancellationToken ct)
    {
        var location = await _locationRepository.GetByIdAsync(locationId, ct);
        if (location is null) return NotFound("Location not found.");

        var rooms = await _roomRepository.FindAsync(r => r.LocationId == locationId, ct);

        return Ok(rooms.Select(room => MapToDto(room, location.Name)).ToList());
    }

    /// <summary>Creates a new room within the specified location.</summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.AdminOrManager)]
    public async Task<ActionResult<RoomDto>> Create(CreateRoomRequest request, CancellationToken ct)
    {
        var location = await _locationRepository.GetByIdAsync(request.LocationId, ct);
        if (location is null) return BadRequest("Invalid LocationId.");

        var room = new Room(request.Name, request.Capacity, location);
        room.UpdateDetails(request.Name, request.Capacity, request.Description);

        var created = await _roomRepository.AddAsync(room, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created, location.Name));
    }

    /// <summary>Updates a room's name, capacity, and description.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.AdminOrManager)]
    public async Task<IActionResult> Update(int id, UpdateRoomRequest request, CancellationToken ct)
    {
        var existing = await _roomRepository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        existing.UpdateDetails(request.Name, request.Capacity, request.Description);

        await _roomRepository.UpdateAsync(existing, ct);
        return NoContent();
    }

    /// <summary>Deletes a room.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.AdminOrManager)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var room = await _roomRepository.GetByIdAsync(id, ct);
        if (room is null) return NotFound();

        await _roomRepository.DeleteAsync(room, ct);
        return NoContent();
    }

    private static RoomDto MapToDto(Room room, IReadOnlyDictionary<int, string> locationNames) => new()
    {
        Id = room.Id,
        Name = room.Name,
        Capacity = room.Capacity,
        Description = room.Description,
        LocationId = room.LocationId,
        LocationName = locationNames.TryGetValue(room.LocationId, out var name) ? name : null
    };

    private static RoomDto MapToDto(Room room, string? locationName) => new()
    {
        Id = room.Id,
        Name = room.Name,
        Capacity = room.Capacity,
        Description = room.Description,
        LocationId = room.LocationId,
        LocationName = locationName
    };
}
