using Microsoft.AspNetCore.Authorization;
using ClinicScheduler.Core.Entities;
using ClinicScheduler.Infrastructure.Data;
using ClinicScheduler.Web.Contracts.Appointments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicScheduler.Web.Api;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly ClinicDbContext _dbContext;

    public AppointmentsController(ClinicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetAll(CancellationToken ct)
    {
        var appointments = await _dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Therapist)
            .Include(x => x.Room)
            .Include(x => x.TreatmentPlan)
            .OrderBy(x => x.StartTime)
            .ToListAsync(ct);

        return Ok(appointments.Select(static x => MapToDto(x)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppointmentDto>> GetById(int id, CancellationToken ct)
    {
        var appointment = await _dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Therapist)
            .Include(x => x.Room)
            .Include(x => x.TreatmentPlan)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return appointment is null ? NotFound() : Ok(MapToDto(appointment));
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentRequest request, CancellationToken ct)
    {
        var appointment = new Appointment
        {
            PatientId = request.PatientId,
            TherapistId = request.TherapistId,
            RoomId = request.RoomId,
            TreatmentPlanId = request.TreatmentPlanId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Notes = request.Notes,
            Status = AppointmentStatus.Scheduled,
            HasConflict = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var validationResult = await ValidateAppointmentAsync(appointment, ct);
        if (validationResult is not null)
        {
            return validationResult;
        }

        _dbContext.Appointments.Add(appointment);
        await _dbContext.SaveChangesAsync(ct);

        var created = await _dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Therapist)
            .Include(x => x.Room)
            .Include(x => x.TreatmentPlan)
            .FirstAsync(x => x.Id == appointment.Id, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateAppointmentRequest request, CancellationToken ct)
    {
        var existing = await _dbContext.Appointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null)
        {
            return NotFound();
        }

        var candidate = new Appointment
        {
            Id = id,
            PatientId = request.PatientId,
            TherapistId = request.TherapistId,
            RoomId = request.RoomId,
            TreatmentPlanId = request.TreatmentPlanId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = request.Status,
            Notes = request.Notes
        };

        var validationResult = await ValidateAppointmentAsync(candidate, ct, excludeAppointmentId: id);
        if (validationResult is not null)
        {
            return validationResult;
        }

        existing.PatientId = request.PatientId;
        existing.TherapistId = request.TherapistId;
        existing.RoomId = request.RoomId;
        existing.TreatmentPlanId = request.TreatmentPlanId;
        existing.StartTime = request.StartTime;
        existing.EndTime = request.EndTime;
        existing.Status = request.Status;
        existing.Notes = request.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

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

    private static AppointmentDto MapToDto(Appointment appointment) => new()
    {
        Id = appointment.Id,
        PatientId = appointment.PatientId,
        PatientName = $"{appointment.Patient.FirstName} {appointment.Patient.LastName}".Trim(),
        TherapistId = appointment.TherapistId,
        TherapistName = $"{appointment.Therapist.FirstName} {appointment.Therapist.LastName}".Trim(),
        RoomId = appointment.RoomId,
        RoomName = appointment.Room.Name,
        TreatmentPlanId = appointment.TreatmentPlanId,
        StartTime = appointment.StartTime,
        EndTime = appointment.EndTime,
        Status = appointment.Status,
        HasConflict = appointment.HasConflict,
        Notes = appointment.Notes,
        CreatedAt = appointment.CreatedAt,
        UpdatedAt = appointment.UpdatedAt
    };

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