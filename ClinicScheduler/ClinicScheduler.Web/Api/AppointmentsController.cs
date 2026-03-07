using ClinicScheduler.Core.Entities;
using ClinicScheduler.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicScheduler.Web.Api;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly ClinicDbContext _dbContext;

    public AppointmentsController(ClinicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Appointment>>> GetAll(CancellationToken ct)
    {
        var appointments = await _dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Therapist)
            .Include(x => x.Room)
            .Include(x => x.TreatmentPlan)
            .OrderBy(x => x.StartTime)
            .ToListAsync(ct);

        return Ok(appointments);
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Appointment>> GetById(int id, CancellationToken ct)
    {
        var appointment = await _dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Therapist)
            .Include(x => x.Room)
            .Include(x => x.TreatmentPlan)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return appointment is null ? NotFound() : Ok(appointment);
    }

    [HttpPost]
    public async Task<ActionResult<Appointment>> Create(Appointment appointment, CancellationToken ct)
    {
        var validationResult = await ValidateAppointmentAsync(appointment, ct);
        if (validationResult is not null)
        {
            return validationResult;
        }

        appointment.Id = 0;
        appointment.CreatedAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;

        _dbContext.Appointments.Add(appointment);
        await _dbContext.SaveChangesAsync(ct);

        var created = await _dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Therapist)
            .Include(x => x.Room)
            .Include(x => x.TreatmentPlan)
            .FirstAsync(x => x.Id == appointment.Id, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Appointment appointment, CancellationToken ct)
    {
        if (id != appointment.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var existing = await _dbContext.Appointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null)
        {
            return NotFound();
        }

        var validationResult = await ValidateAppointmentAsync(appointment, ct, excludeAppointmentId: id);
        if (validationResult is not null)
        {
            return validationResult;
        }

        existing.PatientId = appointment.PatientId;
        existing.TherapistId = appointment.TherapistId;
        existing.RoomId = appointment.RoomId;
        existing.TreatmentPlanId = appointment.TreatmentPlanId;
        existing.StartTime = appointment.StartTime;
        existing.EndTime = appointment.EndTime;
        existing.Status = appointment.Status;
        existing.HasConflict = appointment.HasConflict;
        existing.Notes = appointment.Notes;

        await _dbContext.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var appointment = await _dbContext.Appointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (appointment is null)
        {
            return NotFound();
        }

        _dbContext.Appointments.Remove(appointment);
        await _dbContext.SaveChangesAsync(ct);

        return NoContent();
    }

    private async Task<ActionResult?> ValidateAppointmentAsync(
        Appointment appointment,
        CancellationToken ct,
        int? excludeAppointmentId = null)
    {
        if (appointment.EndTime <= appointment.StartTime)
        {
            return BadRequest("EndTime must be later than StartTime.");
        }

        var patientExists = await _dbContext.Patients.AnyAsync(x => x.Id == appointment.PatientId, ct);
        if (!patientExists)
        {
            return BadRequest("Invalid PatientId.");
        }

        var therapistExists = await _dbContext.Therapists.AnyAsync(x => x.Id == appointment.TherapistId, ct);
        if (!therapistExists)
        {
            return BadRequest("Invalid TherapistId.");
        }

        var roomExists = await _dbContext.Rooms.AnyAsync(x => x.Id == appointment.RoomId, ct);
        if (!roomExists)
        {
            return BadRequest("Invalid RoomId.");
        }

        if (appointment.TreatmentPlanId.HasValue)
        {
            var treatmentPlanExists = await _dbContext.TreatmentPlans
                .AnyAsync(x => x.Id == appointment.TreatmentPlanId.Value, ct);

            if (!treatmentPlanExists)
            {
                return BadRequest("Invalid TreatmentPlanId.");
            }
        }

        var therapistConflict = await _dbContext.Appointments.AnyAsync(x =>
            x.Id != excludeAppointmentId &&
            x.TherapistId == appointment.TherapistId &&
            appointment.StartTime < x.EndTime &&
            appointment.EndTime > x.StartTime, ct);

        if (therapistConflict)
        {
            return BadRequest("Therapist already has an appointment during that time.");
        }

        var roomConflict = await _dbContext.Appointments.AnyAsync(x =>
            x.Id != excludeAppointmentId &&
            x.RoomId == appointment.RoomId &&
            appointment.StartTime < x.EndTime &&
            appointment.EndTime > x.StartTime, ct);

        if (roomConflict)
        {
            return BadRequest("Room already has an appointment during that time.");
        }

        return null;
    }
}