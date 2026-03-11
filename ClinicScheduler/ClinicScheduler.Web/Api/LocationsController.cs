using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using ClinicScheduler.Web.Contracts.Locations;
using Microsoft.AspNetCore.Mvc;

namespace ClinicScheduler.Web.Api;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly IRepository<Location> _repository;

    public LocationsController(IRepository<Location> repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LocationDto>>> GetAll(CancellationToken ct)
    {
        var locations = await _repository.GetAllAsync(ct);
        return Ok(locations.Select(static x => MapToDto(x)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LocationDto>> GetById(int id, CancellationToken ct)
    {
        var location = await _repository.GetByIdAsync(id, ct);
        return location is null ? NotFound() : Ok(MapToDto(location));
    }

    [HttpPost]
    public async Task<ActionResult<LocationDto>> Create(CreateLocationRequest request, CancellationToken ct)
    {
        var location = new Location
        {
            Name = request.Name,
            Address = request.Address,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            TimeZone = request.TimeZone
        };

        var created = await _repository.AddAsync(location, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateLocationRequest request, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        existing.Name = request.Name;
        existing.Address = request.Address;
        existing.City = request.City;
        existing.State = request.State;
        existing.ZipCode = request.ZipCode;
        existing.TimeZone = request.TimeZone;

        await _repository.UpdateAsync(existing, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var location = await _repository.GetByIdAsync(id, ct);
        if (location is null) return NotFound();

        await _repository.DeleteAsync(location, ct);
        return NoContent();
    }

    private static LocationDto MapToDto(Location location) => new()
    {
        Id = location.Id,
        Name = location.Name,
        Address = location.Address,
        City = location.City,
        State = location.State,
        ZipCode = location.ZipCode,
        TimeZone = location.TimeZone,
        CreatedAt = location.CreatedAt,
        UpdatedAt = location.UpdatedAt
    };
}