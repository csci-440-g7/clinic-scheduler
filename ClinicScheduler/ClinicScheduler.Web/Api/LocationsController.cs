using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;

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
    public async Task<ActionResult<IReadOnlyList<Location>>> GetAll(CancellationToken ct)
        => Ok(await _repository.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Location>> GetById(int id, CancellationToken ct)
    {
        var location = await _repository.GetByIdAsync(id, ct);
        return location is null ? NotFound() : Ok(location);
    }

    [HttpPost]
    public async Task<ActionResult<Location>> Create(Location location, CancellationToken ct)
    {
        var created = await _repository.AddAsync(location, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Location location, CancellationToken ct)
    {
        if (id != location.Id) return BadRequest("ID mismatch.");

        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        existing.Name = location.Name;
        existing.Address = location.Address;
        existing.City = location.City;
        existing.State = location.State;
        existing.ZipCode = location.ZipCode;
        existing.TimeZone = location.TimeZone;

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
}