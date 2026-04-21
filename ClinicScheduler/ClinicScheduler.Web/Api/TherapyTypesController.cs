using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using ClinicScheduler.Web.Contracts.TherapyTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicScheduler.Web.Api;

/// <summary>Manages therapy type resources.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TherapyTypesController : ControllerBase
{
    private readonly IRepository<TherapyType> _repository;

    /// <summary>Initializes a new instance of <see cref="TherapyTypesController"/>.</summary>
    public TherapyTypesController(IRepository<TherapyType> repository)
    {
        _repository = repository;
    }

    /// <summary>Returns all therapy types.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TherapyTypeDto>>> GetAll(CancellationToken ct)
    {
        var types = await _repository.GetAllAsync(ct);
        return Ok(types.Select(static t => MapToDto(t)).ToList());
    }

    /// <summary>Returns a single therapy type by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TherapyTypeDto>> GetById(int id, CancellationToken ct)
    {
        var type = await _repository.GetByIdAsync(id, ct);
        return type is null ? NotFound() : Ok(MapToDto(type));
    }

    /// <summary>Creates a new therapy type.</summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.AdminOrManager)]
    public async Task<ActionResult<TherapyTypeDto>> Create(CreateTherapyTypeRequest request, CancellationToken ct)
    {
        try
        {
            var therapyType = new TherapyType(request.Name, request.Description, request.Specialty, request.ColorCode);
            var created = await _repository.AddAsync(therapyType, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Updates an existing therapy type.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.AdminOrManager)]
    public async Task<IActionResult> Update(int id, UpdateTherapyTypeRequest request, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        try
        {
            existing.UpdateDetails(request.Name, request.Description, request.Specialty, request.ColorCode);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        await _repository.UpdateAsync(existing, ct);
        return NoContent();
    }

    /// <summary>Deletes a therapy type.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.AdminOrManager)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var type = await _repository.GetByIdAsync(id, ct);
        if (type is null) return NotFound();

        await _repository.DeleteAsync(type, ct);
        return NoContent();
    }

    private static TherapyTypeDto MapToDto(TherapyType type) => new()
    {
        Id = type.Id,
        Name = type.Name,
        Description = type.Description,
        Specialty = type.Specialty,
        ColorCode = type.ColorCode,
        CreatedAt = type.CreatedAt,
        UpdatedAt = type.UpdatedAt
    };
}
