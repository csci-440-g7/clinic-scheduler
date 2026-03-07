using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClinicScheduler.Web.Api;

[ApiController]
[Route("api/[controller]")]
public class TherapyTypesController : ControllerBase
{
    private readonly IRepository<TherapyType> _repository;

    public TherapyTypesController(IRepository<TherapyType> repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TherapyType>>> GetAll(CancellationToken ct)
        => Ok(await _repository.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TherapyType>> GetById(int id, CancellationToken ct)
    {
        var type = await _repository.GetByIdAsync(id, ct);
        return type is null ? NotFound() : Ok(type);
    }

    [HttpPost]
    public async Task<ActionResult<TherapyType> Create(TherapyType therapyType, CancellationToken ct)
    {
        var created = await _repository.AddAsync(therapyType, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TherapyType therapyType, CancellationToken ct)
    {
        if (id != therapyType.Id) return BadRequest("ID mismatch.");

        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        existing.Name = therapyType.Name;
        existing.Description = therapyType.Description;
        existing.Specialty = therapyType.Specialty;
        existing.ColorCode = therapyType.ColorCode;

        await _repository.UpdateAsync(existing, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var type = await _repository.GetByIdAsync(id, ct);
        if (type is null) return NotFound();

        await _repository.DeleteAsync(type, ct);
        return NoContent();
    }
}