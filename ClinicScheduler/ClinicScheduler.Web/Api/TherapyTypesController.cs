using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using ClinicScheduler.Web.Contracts.TherapyTypes;
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
    public async Task<ActionResult<IReadOnlyList<TherapyTypeDto>>> GetAll(CancellationToken ct)
    {
        var types = await _repository.GetAllAsync(ct);
        return Ok(types.Select(static t => MapToDto(t)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TherapyTypeDto>> GetById(int id, CancellationToken ct)
    {
        var type = await _repository.GetByIdAsync(id, ct);
        return type is null ? NotFound() : Ok(MapToDto(type));
    }

    [HttpPost]
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

    [HttpPut("{id:int}")]
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

    [HttpDelete("{id:int}")]
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
