using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using ClinicScheduler.Web.Contracts.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicScheduler.Web.Api;

/// <summary>Manages clinic location resources.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly IRepository<Location> _repository;

    /// <summary>Initializes a new instance of <see cref="LocationsController"/>.</summary>
    public LocationsController(IRepository<Location> repository)
    {
        _repository = repository;
    }

    /// <summary>Returns all clinic locations.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LocationDto>>> GetAll(CancellationToken ct)
    {
        var locations = await _repository.GetAllAsync(ct);
        return Ok(locations.Select(static x => MapToDto(x)).ToList());
    }

    /// <summary>Returns a single location by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LocationDto>> GetById(int id, CancellationToken ct)
    {
        var location = await _repository.GetByIdAsync(id, ct);
        return location is null ? NotFound() : Ok(MapToDto(location));
    }

    /// <summary>Creates a new clinic location.</summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.AdminOrManager)]
    public async Task<ActionResult<LocationDto>> Create(CreateLocationRequest request, CancellationToken ct)
    {
        var location = new Location(request.Name, request.Address);
        location.UpdateAddress(request.Address, request.City, request.State, request.ZipCode);
        location.TimeZone = request.TimeZone;

        var created = await _repository.AddAsync(location, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    /// <summary>Updates an existing location's details.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.AdminOrManager)]
    public async Task<IActionResult> Update(int id, UpdateLocationRequest request, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        existing.UpdateDetails(request.Name, request.Address, request.City, request.State, request.ZipCode, request.TimeZone);

        await _repository.UpdateAsync(existing, ct);
        return NoContent();
    }

    /// <summary>Deletes a clinic location.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.AdminOrManager)]
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
