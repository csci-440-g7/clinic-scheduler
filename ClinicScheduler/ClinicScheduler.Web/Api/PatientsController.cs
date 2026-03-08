using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using ClinicScheduler.Web.Contracts.Patients;
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
    public async Task<ActionResult<IReadOnlyList<PatientDto>>> GetAll(CancellationToken ct)
    {
        var patients = await _repository.GetAllAsync(ct);

        var result = patients.Select(static patient => new PatientDto
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            Email = patient.Email,
            Phone = patient.Phone,
            DateOfBirth = patient.DateOfBirth,
            CreatedAt = patient.CreatedAt,
            UpdatedAt = patient.UpdatedAt
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PatientDto>> GetById(int id, CancellationToken ct)
    {
        var patient = await _repository.GetByIdAsync(id, ct);
        if (patient is null) return NotFound();

        return Ok(new PatientDto
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            Email = patient.Email,
            Phone = patient.Phone,
            DateOfBirth = patient.DateOfBirth,
            CreatedAt = patient.CreatedAt,
            UpdatedAt = patient.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create(CreatePatientRequest request, CancellationToken ct)
    {
        var patient = new Patient
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth
        };

        var created = await _repository.AddAsync(patient, ct);

        var result = new PatientDto
        {
            Id = created.Id,
            FirstName = created.FirstName,
            LastName = created.LastName,
            Email = created.Email,
            Phone = created.Phone,
            DateOfBirth = created.DateOfBirth,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdatePatientRequest request, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        existing.FirstName = request.FirstName;
        existing.LastName = request.LastName;
        existing.Email = request.Email;
        existing.Phone = request.Phone;
        existing.DateOfBirth = request.DateOfBirth;

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