using Microsoft.AspNetCore.Authorization;
using ClinicScheduler.Core.Entities;
using ClinicScheduler.Infrastructure.Data;
using ClinicScheduler.Web.Contracts.TreatmentPlans;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicScheduler.Web.Api;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class TreatmentPlansController : ControllerBase
{
    private static readonly int[] AllowedFrequencies = [2, 3, 4];
    private static readonly int[] AllowedTotalDays = [20, 30, 50];

    private readonly ClinicDbContext _dbContext;

    public TreatmentPlansController(ClinicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TreatmentPlanDto>>> GetAll(CancellationToken ct)
    {
        var plans = await _dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Therapist)
            .Include(x => x.TreatmentPlanTherapies)
                .ThenInclude(x => x.TherapyType)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return Ok(plans.Select(static x => MapToDto(x)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TreatmentPlanDto>> GetById(int id, CancellationToken ct)
    {
        var plan = await _dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Therapist)
            .Include(x => x.TreatmentPlanTherapies)
                .ThenInclude(x => x.TherapyType)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return plan is null ? NotFound() : Ok(MapToDto(plan));
    }

    [HttpPost]
    public async Task<ActionResult<TreatmentPlanDto>> Create(CreateTreatmentPlanRequest request, CancellationToken ct)
    {
        var normalizedTherapyTypeIds = request.TherapyTypeIds.Distinct().ToList();
        DateOnly? endDate = request.EndDate.HasValue ? DateOnly.FromDateTime(request.EndDate.Value) : null;

        var validationResult = await ValidateTreatmentPlanAsync(
            request.PatientId,
            request.TherapistId,
            request.FrequencyPerWeek,
            request.TotalDays,
            request.StartDate,
            endDate,
            normalizedTherapyTypeIds,
            ct);

        if (validationResult is not null)
        {
            return validationResult;
        }

        var treatmentPlan = new TreatmentPlan
        {
            PatientId = request.PatientId,
            TherapistId = request.TherapistId,
            FrequencyPerWeek = request.FrequencyPerWeek,
            TotalDays = request.TotalDays,
            StartDate = request.StartDate,
            EndDate = endDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TreatmentPlanTherapies = normalizedTherapyTypeIds
                .Select(therapyTypeId => new TreatmentPlanTherapy { TherapyTypeId = therapyTypeId })
                .ToList()
        };

        _dbContext.TreatmentPlans.Add(treatmentPlan);
        await _dbContext.SaveChangesAsync(ct);

        var created = await _dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Therapist)
            .Include(x => x.TreatmentPlanTherapies)
                .ThenInclude(x => x.TherapyType)
            .FirstAsync(x => x.Id == treatmentPlan.Id, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateTreatmentPlanRequest request, CancellationToken ct)
    {
        var existing = await _dbContext.TreatmentPlans
            .Include(x => x.TreatmentPlanTherapies)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (existing is null)
        {
            return NotFound();
        }

        var normalizedTherapyTypeIds = request.TherapyTypeIds.Distinct().ToList();

        var validationResult = await ValidateTreatmentPlanAsync(
            request.PatientId,
            request.TherapistId,
            request.FrequencyPerWeek,
            request.TotalDays,
            request.StartDate,
            request.EndDate,
            normalizedTherapyTypeIds,
            ct);

        if (validationResult is not null)
        {
            return validationResult;
        }

        existing.PatientId = request.PatientId;
        existing.TherapistId = request.TherapistId;
        existing.FrequencyPerWeek = request.FrequencyPerWeek;
        existing.TotalDays = request.TotalDays;
        existing.StartDate = request.StartDate;
        existing.EndDate = request.EndDate;
        existing.UpdatedAt = DateTime.UtcNow;

        _dbContext.TreatmentPlanTherapies.RemoveRange(existing.TreatmentPlanTherapies);
        foreach (var therapyTypeId in normalizedTherapyTypeIds)
        {
            existing.TreatmentPlanTherapies.Add(new TreatmentPlanTherapy
            {
                TreatmentPlanId = existing.Id,
                TherapyTypeId = therapyTypeId
            });
        }

        await _dbContext.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var existing = await _dbContext.TreatmentPlans
            .Include(x => x.TreatmentPlanTherapies)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (existing is null)
        {
            return NotFound();
        }

        _dbContext.TreatmentPlanTherapies.RemoveRange(existing.TreatmentPlanTherapies);
        _dbContext.TreatmentPlans.Remove(existing);

        await _dbContext.SaveChangesAsync(ct);
        return NoContent();
    }

    private static TreatmentPlanDto MapToDto(TreatmentPlan treatmentPlan) => new()
    {
        Id = treatmentPlan.Id,
        PatientId = treatmentPlan.PatientId,
        PatientName = $"{treatmentPlan.Patient.FirstName} {treatmentPlan.Patient.LastName}".Trim(),
        TherapistId = treatmentPlan.TherapistId,
        TherapistName = $"{treatmentPlan.Therapist.FirstName} {treatmentPlan.Therapist.LastName}".Trim(),
        FrequencyPerWeek = treatmentPlan.FrequencyPerWeek,
        TotalDays = treatmentPlan.TotalDays,
        StartDate = treatmentPlan.StartDate,
        EndDate = treatmentPlan.EndDate.HasValue
            ? DateTime.SpecifyKind(treatmentPlan.EndDate.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : null,
        Therapies = treatmentPlan.TreatmentPlanTherapies
            .OrderBy(x => x.TherapyTypeId)
            .Select(x => new TreatmentPlanTherapyDto
            {
                TherapyTypeId = x.TherapyTypeId,
                TherapyTypeName = x.TherapyType.Name,
                Specialty = x.TherapyType.Specialty,
                ColorCode = x.TherapyType.ColorCode
            })
            .ToList(),
        CreatedAt = treatmentPlan.CreatedAt,
        UpdatedAt = treatmentPlan.UpdatedAt
    };

    private async Task<ActionResult?> ValidateTreatmentPlanAsync(
        int patientId,
        int therapistId,
        int frequencyPerWeek,
        int totalDays,
        DateOnly startDate,
        DateOnly? endDate,
        IReadOnlyCollection<int> therapyTypeIds,
        CancellationToken ct)
    {
        if (!AllowedFrequencies.Contains(frequencyPerWeek))
        {
            return BadRequest("FrequencyPerWeek must be 2, 3, or 4.");
        }

        if (!AllowedTotalDays.Contains(totalDays))
        {
            return BadRequest("TotalDays must be 20, 30, or 50.");
        }

        if (endDate.HasValue && endDate.Value < startDate)
        {
            return BadRequest("EndDate cannot be earlier than StartDate.");
        }

        var patientExists = await _dbContext.Patients.AnyAsync(x => x.Id == patientId, ct);
        if (!patientExists)
        {
            return BadRequest("Invalid PatientId.");
        }

        var therapistExists = await _dbContext.Therapists.AnyAsync(x => x.Id == therapistId, ct);
        if (!therapistExists)
        {
            return BadRequest("Invalid TherapistId.");
        }


        if (therapyTypeIds.Count == 0)
        {
            return BadRequest("At least one TherapyType is required.");
        }

        var validTherapyCount = await _dbContext.TherapyTypes
            .CountAsync(x => therapyTypeIds.Contains(x.Id), ct);

        if (validTherapyCount != therapyTypeIds.Count)
        {
            return BadRequest("One or more TherapyTypeIds are invalid.");
        }

        return null;
    }
}