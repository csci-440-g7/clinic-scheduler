using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
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
    public async Task<ActionResult<IReadOnlyList<Room>>> GetAll(CancellationToken ct) =>
        Ok(await _roomRepository.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Room>> GetById(int id, CancellationToken ct)
    {
        var room = await _roomRepository.GetByIdAsync(id, ct);
        return room is null ? NotFound() : Ok(room);
    }

    [HttpGet("location/{locationId:int}")]
    public async Task<ActionResult<IReadOnlyList<Room>>> GetByLocation(int locationId, CancellationToken ct)
    {
        var location = await _locationRepository.GetByIdAsync(locationId, ct);
        if (location is null) return NotFound("Location not found.");

        var rooms = (await _roomRepository.GetAllAsync(ct))
            .Where(r => r.LocationId == locationId)
            .ToList();

        return Ok(rooms);
    }

    [HttpPost]
    public async Task<ActionResult<Room>> Create(Room room, CancellationToken ct)
    {
        var location = await _locationRepository.GetByIdAsync(room.LocationId, ct);
        if (location is null) return BadRequest("Invalid LocationId.");

        var created = await _roomRepository.AddAsync(room, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Room room, CancellationToken ct)
    {
        if (id != room.Id) return BadRequest("ID mismatch.");

        var existing = await _roomRepository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        var location = await _locationRepository.GetByIdAsync(room.LocationId, ct);
        if (location is null) return BadRequest("Invalid LocationId.");

        existing.Name = room.Name;
        existing.Capacity = room.Capacity;
        existing.Description = room.Description;
        existing.LocationId = room.LocationId;

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
}