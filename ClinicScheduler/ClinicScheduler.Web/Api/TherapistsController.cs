using Microsoft.AspNetCore.Authorization;
using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using ClinicScheduler.Web.Contracts.Therapists;
using Microsoft.AspNetCore.Mvc;

namespace ClinicScheduler.Web.Api;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class TherapistsController : ControllerBase
{
    private readonly IRepository<Therapist> _repository;

    public TherapistsController(IRepository<Therapist> repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TherapistDto>>> GetAll(CancellationToken ct)
    {
        var therapists = await _repository.GetAllAsync(ct);
        return Ok(therapists.Select(static therapist => MapToDto(therapist)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TherapistDto>> GetById(int id, CancellationToken ct)
    {
        var therapist = await _repository.GetByIdAsync(id, ct);
        return therapist is null ? NotFound() : Ok(MapToDto(therapist));
    }

    [HttpPost]
    public async Task<ActionResult<TherapistDto>> Create(CreateTherapistRequest request, CancellationToken ct)
    {
        var therapist = new Therapist
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Specialty = request.Specialty
        };

        var created = await _repository.AddAsync(therapist, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateTherapistRequest request, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        existing.FirstName = request.FirstName;
        existing.LastName = request.LastName;
        existing.Email = request.Email;
        existing.Phone = request.Phone;
        existing.Specialty = request.Specialty;

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

    private static TherapistDto MapToDto(Therapist therapist) => new()
    {
        Id = therapist.Id,
        FirstName = therapist.FirstName,
        LastName = therapist.LastName,
        Email = therapist.Email,
        Phone = therapist.Phone,
        CreatedAt = therapist.CreatedAt,
        UpdatedAt = therapist.UpdatedAt
    };
}