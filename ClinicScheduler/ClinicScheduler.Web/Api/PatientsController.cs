using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClinicScheduler.Web.Api;

/// <summary>
/// RESTful API controller for Patient CRUD operations.
/// The [ApiController] attribute enables automatic model validation and binding.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IRepository<Patient> _repository;

    public PatientsController(IRepository<Patient> repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Patient>>> GetAll(CancellationToken ct)
        => Ok(await _repository.GetAllAsync(ct));

    [HttpGet("id:int")]
    public async Task<ActionResult<Patient>> GetById(int id, CancellationToken ct)
    {
        var patient = await _repository.GetByIdAsync(id, ct);
        return patient is null ? NotFound() : Ok(patient);
    }

    [HttpPost]
    public async Task<ActionResult<Patient>> Create(Patient patient, CancellationToken ct)
    {
        var created = await _repository.AddAsync(patient, ct);
        return CreatedAtAction(nameof(GetById), new {id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Patient patient, CancellationToken ct)
    {
        if (id != patient.Id) return BadRequest("ID mismatch.");

        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        existing.FirstName = patient.FirstName;
        existing.LastName = patient.LastName;
        existing.Email = patient.Email;
        existing.Phone = patient.Phone;
        existing.DateOfBirth = patient.DateOfBirth;

        await _repository.UpdateAsync(existing, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var patient = await _repository.GetByIdAsync(id, ct);
        if (patient is null) return NotFound();

        await _repository.DeleteAsync(patient, ct);
        return NoContent();
    }
}