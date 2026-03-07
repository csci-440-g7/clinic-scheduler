using ClinicScheduler.Core.Entities;
using ClinicScheduler.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicScheduler.Web.Api;

[ApiController]
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
    public async Task<ActionResult<IReadOnlyList<TreatmentPlan>>> GetAll(CancellationToken ct)
    {
        var plans = await _dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Therapist)
            .Include(x => x.TreatmentPlanTherapies)
                .ThenInclude(x => x.TherapyType)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return Ok(plans);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TreatmentPlan>> GetById(int id, CancellationToken ct)
    {
        var plan = await _dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Therapist)
            .Include(x => x.TreatmentPlanTherapies)
                .ThenInclude(x => x.TherapyType)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return plan is null ? NotFound() : Ok(plan);
    }

    [HttpPost]
    public async Task<ActionResult<TreatmentPlan>> Create(TreatmentPlan treatmentPlan, CancellationToken ct)
    {
        var validationResult = await ValidateTreatmentPlanAsync(treatmentPlan, ct);
        if (validationResult is not null)
        {
            return validationResult;
        }

        treatmentPlan.Id = 0;
        treatmentPlan.CreatedAt = DateTime.UtcNow;
        treatmentPlan.UpdatedAt = DateTime.UtcNow;

        var requestedTherapyTypeIds = treatmentPlan.TreatmentPlanTherapies
            .Select(x => x.TherapyTypeId)
            .Distinct()
            .ToList();

        treatmentPlan.TreatmentPlanTherapies.Clear();

        foreach (var therapyTypeId in requestedTherapyTypeIds)
        {
            treatmentPlan.TreatmentPlanTherapies.Add(new TreatmentPlanTherapy
            {
                TherapyTypeId = therapyTypeId
            });
        }

        _dbContext.TreatmentPlans.Add(treatmentPlan);
        await _dbContext.SaveChangesAsync(ct);

        var created = await _dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Therapist)
            .Include(x => x.TreatmentPlanTherapies)
                .ThenInclude(x => x.TherapyType)
            .FirstAsync(x => x.Id == treatmentPlan.Id, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TreatmentPlan treatmentPlan, CancellationToken ct)
    {
        if (id != treatmentPlan.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var existing = await _dbContext.TreatmentPlans
            .Include(x => x.TreatmentPlanTherapies)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (existing is null)
        {
            return NotFound();
        }

        var validationResult = await ValidateTreatmentPlanAsync(treatmentPlan, ct);
        if (validationResult is not null)
        {
            return validationResult;
        }

        existing.PatientId = treatmentPlan.PatientId;
        existing.TherapistId = treatmentPlan.TherapistId;
        existing.FrequencyPerWeek = treatmentPlan.FrequencyPerWeek;
        existing.TotalDays = treatmentPlan.TotalDays;
        existing.StartDate = treatmentPlan.StartDate;
        existing.EndDate = treatmentPlan.EndDate;
        existing.UpdatedAt = DateTime.UtcNow;

        _dbContext.TreatmentPlanTherapies.RemoveRange(existing.TreatmentPlanTherapies);

        var requestedTherapyTypeIds = treatmentPlan.TreatmentPlanTherapies
            .Select(x => x.TherapyTypeId)
            .Distinct()
            .ToList();

        foreach (var therapyTypeId in requestedTherapyTypeIds)
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

    private async Task<ActionResult?> ValidateTreatmentPlanAsync(TreatmentPlan treatmentPlan, CancellationToken ct)
    {
        if (!AllowedFrequencies.Contains(treatmentPlan.FrequencyPerWeek))
        {
            return BadRequest("FrequencyPerWeek must be 2, 3, or 4.");
        }

        if (!AllowedTotalDays.Contains(treatmentPlan.TotalDays))
        {
            return BadRequest("TotalDays must be 20, 30, or 50.");
        }

        if (treatmentPlan.EndDate.HasValue && treatmentPlan.EndDate.Value < treatmentPlan.StartDate)
        {
            return BadRequest("EndDate cannot be earlier than StartDate.");
        }

        var patientExists = await _dbContext.Patients.AnyAsync(x => x.Id == treatmentPlan.PatientId, ct);
        if (!patientExists)
        {
            return BadRequest("Invalid PatientId.");
        }

        var therapistExists = await _dbContext.Therapists.AnyAsync(x => x.Id == treatmentPlan.TherapistId, ct);
        if (!therapistExists)
        {
            return BadRequest("Invalid TherapistId.");
        }

        var therapyTypeIds = treatmentPlan.TreatmentPlanTherapies
            .Select(x => x.TherapyTypeId)
            .Distinct()
            .ToList();

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