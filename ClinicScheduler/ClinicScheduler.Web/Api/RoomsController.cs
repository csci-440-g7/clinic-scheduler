using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using ClinicScheduler.Web.Contracts.Rooms;
using Microsoft.AspNetCore.Mvc;

namespace ClinicScheduler.Web.Api;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly IRepository<Room> _roomRepository;
    private readonly IRepository<Location> _locationRepository;

    public RoomsController(IRepository<Room> roomRepository, IRepository<Location> locationRepository)
    {
        _roomRepository = roomRepository;
        _locationRepository = locationRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetAll(CancellationToken ct)
    {
        var rooms = await _roomRepository.GetAllAsync(ct);
        var locations = await _locationRepository.GetAllAsync(ct);
        var locationNames = locations.ToDictionary(x => x.Id, x => x.Name);

        return Ok(rooms.Select(room => MapToDto(room, locationNames)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDto>> GetById(int id, CancellationToken ct)
    {
        var room = await _roomRepository.GetByIdAsync(id, ct);
        if (room is null) return NotFound();

        var location = await _locationRepository.GetByIdAsync(room.LocationId, ct);
        return Ok(MapToDto(room, location?.Name));
    }

    [HttpGet("location/{locationId:int}")]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetByLocation(int locationId, CancellationToken ct)
    {
        var location = await _locationRepository.GetByIdAsync(locationId, ct);
        if (location is null) return NotFound("Location not found.");

        var rooms = (await _roomRepository.GetAllAsync(ct))
            .Where(r => r.LocationId == locationId)
            .ToList();

        return Ok(rooms.Select(room => MapToDto(room, location.Name)).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<RoomDto>> Create(CreateRoomRequest request, CancellationToken ct)
    {
        var location = await _locationRepository.GetByIdAsync(request.LocationId, ct);
        if (location is null) return BadRequest("Invalid LocationId.");

        var room = new Room(request.Name, request.Capacity, location);
        room.UpdateDetails(request.Name, request.Capacity, request.Description);

        var created = await _roomRepository.AddAsync(room, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created, location.Name));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateRoomRequest request, CancellationToken ct)
    {
        var existing = await _roomRepository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        existing.UpdateDetails(request.Name, request.Capacity, request.Description);

        await _roomRepository.UpdateAsync(existing, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
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
