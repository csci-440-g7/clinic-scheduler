using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClinicScheduler.Web.Api;

[ApiController]
[Route("api/[controller]")]
public class TherapistsController : ControllerBase
{
    private readonly IRepository<Therapist> _repository;

    public TherapistsController(IRepository<Therapist> repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Therapist>>> GetAll(CancellationToken ct)
        => Ok(await _repository.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Therapist>> GetById(int id, CancellationToken ct)
    {
        var therapist = await _repository.GetByIdAsync(id, ct);
        return therapist is null ? NotFound() : Ok(therapist);
    }

    [HttpPost]
    public async Task<ActionResult<Therapist>> Create(Therapist therapist, CancellationToken ct)
    {
        var created = await _repository.AddAsync(therapist, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Therapist therapist, CancellationToken ct)
    {
        if (id != therapist.Id) return BadRequest("ID mismatch.");

        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        existing.FirstName = therapist.FirstName;
        existing.LastName = therapist.LastName;
        existing.Email = therapist.Email;
        existing.Phone = therapist.Phone;
        existing.Specialty = therapist.Specialty;

        await _repository.UpdateAsync(existing, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var therapist = await _repository.GetByIdAsync(id, ct);
        if (therapist is null) return NotFound();

        await _repository.DeleteAsync(therapist, ct);
        return NoContent();
    }
}