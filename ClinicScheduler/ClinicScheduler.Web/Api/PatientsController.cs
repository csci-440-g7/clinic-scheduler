using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using ClinicScheduler.Web.Contracts.Patients;
using Microsoft.AspNetCore.Mvc;

namespace ClinicScheduler.Web.Api;

/// <summary>Manages patient resources.</summary>
[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IRepository<Patient> _repository;

    /// <summary>Initializes a new instance of <see cref="PatientsController"/>.</summary>
    public PatientsController(IRepository<Patient> repository)
    {
        _repository = repository;
    }

    /// <summary>Returns all patients.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PatientDto>>> GetAll(CancellationToken ct)
    {
        var patients = await _repository.GetAllAsync(ct);
        return Ok(patients.Select(static p => MapToDto(p)).ToList());
    }

    /// <summary>Returns a single patient by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PatientDto>> GetById(int id, CancellationToken ct)
    {
        var patient = await _repository.GetByIdAsync(id, ct);
        return patient is null ? NotFound() : Ok(MapToDto(patient));
    }

    /// <summary>Creates a new patient record.</summary>
    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create(CreatePatientRequest request, CancellationToken ct)
    {
        var patient = new Patient(request.FirstName, request.LastName, request.Email, request.DateOfBirth, request.Phone);
        var created = await _repository.AddAsync(patient, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    /// <summary>Updates a patient's personal and contact information.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdatePatientRequest request, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        existing.UpdateDetails(request.FirstName, request.LastName, request.DateOfBirth);
        existing.UpdateContactInfo(request.Email, request.Phone);

        await _repository.UpdateAsync(existing, ct);
        return NoContent();
    }

    /// <summary>Deletes a patient record.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var patient = await _repository.GetByIdAsync(id, ct);
        if (patient is null) return NotFound();

        await _repository.DeleteAsync(patient, ct);
        return NoContent();
    }

    private static PatientDto MapToDto(Patient patient) => new()
    {
        Id = patient.Id,
        FirstName = patient.FirstName,
        LastName = patient.LastName,
        Email = patient.Email,
        Phone = patient.Phone,
        DateOfBirth = patient.DateOfBirth,
        CreatedAt = patient.CreatedAt,
        UpdatedAt = patient.UpdatedAt
    };
}
