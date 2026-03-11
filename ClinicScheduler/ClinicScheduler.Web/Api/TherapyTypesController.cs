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

        var result = types.Select(static type => new TherapyTypeDto
        {
            Id = type.Id,
            Name = type.Name,
            Description = type.Description,
            Specialty = type.Specialty,
            ColorCode = type.ColorCode,
            CreatedAt = type.CreatedAt,
            UpdatedAt = type.UpdatedAt
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TherapyTypeDto>> GetById(int id, CancellationToken ct)
    {
        var type = await _repository.GetByIdAsync(id, ct);
        if (type is null) return NotFound();

        return Ok(new TherapyTypeDto
        {
            Id = type.Id,
            Name = type.Name,
            Description = type.Description,
            Specialty = type.Specialty,
            ColorCode = type.ColorCode,
            CreatedAt = type.CreatedAt,
            UpdatedAt = type.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<TherapyTypeDto>> Create(CreateTherapyTypeRequest request, CancellationToken ct)
    {
        var therapyType = new TherapyType
        {
            Name = request.Name,
            Description = request.Description,
            Specialty = request.Specialty,
            ColorCode = request.ColorCode
        };

        var created = await _repository.AddAsync(therapyType, ct);

        var result = new TherapyTypeDto
        {
            Id = created.Id,
            Name = created.Name,
            Description = created.Description,
            Specialty = created.Specialty,
            ColorCode = created.ColorCode,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateTherapyTypeRequest request, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Specialty = request.Specialty;
        existing.ColorCode = request.ColorCode;

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