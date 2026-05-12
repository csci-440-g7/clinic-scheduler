using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using ClinicScheduler.Web.Contracts.Therapists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicScheduler.Web.Api;

/// <summary>Manages therapist resources.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TherapistsController : ControllerBase
{
    private readonly IRepository<Therapist> _repository;

    /// <summary>Initializes a new instance of <see cref="TherapistsController"/>.</summary>
    public TherapistsController(IRepository<Therapist> repository)
    {
        _repository = repository;
    }

    /// <summary>Returns all therapists.</summary>
    [HttpGet]
    [Authorize(Roles = RoleNames.StaffOrAbove)]
    public async Task<ActionResult<IReadOnlyList<TherapistDto>>> GetAll(CancellationToken ct)
    {
        var therapists = await _repository.GetAllAsync(ct);
        return Ok(therapists.Select(static t => MapToDto(t)).ToList());
    }

    /// <summary>Returns a single therapist by ID.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = RoleNames.StaffOrAbove)]
    public async Task<ActionResult<TherapistDto>> GetById(int id, CancellationToken ct)
    {
        var therapist = await _repository.GetByIdAsync(id, ct);
        return therapist is null ? NotFound() : Ok(MapToDto(therapist));
    }

    /// <summary>Creates a new therapist record.</summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.AdminOrManager)]
    public async Task<ActionResult<TherapistDto>> Create(CreateTherapistRequest request, CancellationToken ct)
    {
        var therapist = new Therapist(request.FirstName, request.LastName, request.Email, request.Phone, request.Specialty);
        var created = await _repository.AddAsync(therapist, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    /// <summary>Updates a therapist's personal, contact, and specialty information.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.AdminOrManager)]
    public async Task<IActionResult> Update(int id, UpdateTherapistRequest request, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        existing.UpdateDetails(request.FirstName, request.LastName, request.Specialty);
        existing.UpdateContactInfo(request.Email, request.Phone);

        await _repository.UpdateAsync(existing, ct);
        return NoContent();
    }

    /// <summary>Deletes a therapist record.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.AdminOrManager)]
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
        Specialty = therapist.Specialty,
        NpiNumber = therapist.NpiNumber,
        CreatedAt = therapist.CreatedAt,
        UpdatedAt = therapist.UpdatedAt
    };
}
