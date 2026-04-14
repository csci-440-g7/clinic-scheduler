using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Services;
using ClinicScheduler.Infrastructure.Data;
using ClinicScheduler.Web.Contracts.Appointments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicScheduler.Web.Api;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly ClinicDbContext _dbContext;
    private readonly AppointmentSchedulingService _schedulingService;
    private readonly MissedAppointmentService _missedAppointmentService;

    public AppointmentsController(
        ClinicDbContext dbContext,
        AppointmentSchedulingService schedulingService,
        MissedAppointmentService missedAppointmentService)
    {
        _dbContext = dbContext;
        _schedulingService = schedulingService;
        _missedAppointmentService = missedAppointmentService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetAll(CancellationToken ct)
    {
        var appointments = await _dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Therapist)
            .Include(x => x.Room)
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
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return appointment is null ? NotFound() : Ok(MapToDto(appointment));
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentRequest request, CancellationToken ct)
    {
        if (request.EndTime <= request.StartTime)
            return BadRequest("EndTime must be later than StartTime.");

        var duration = request.EndTime - request.StartTime;

        try
        {
            var appointment = await _schedulingService.CreateAppointmentAsync(
                request.PatientId,
                request.TherapistId,
                request.RoomId,
                request.StartTime,
                duration,
                ct);

            appointment.TreatmentPlanId = request.TreatmentPlanId;
            appointment.Notes = request.Notes;
            await _dbContext.SaveChangesAsync(ct);

            var created = await _dbContext.Appointments
                .AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.Therapist)
                .Include(x => x.Room)
                .FirstAsync(x => x.Id == appointment.Id, ct);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateAppointmentRequest request, CancellationToken ct)
    {
        var existing = await _dbContext.Appointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null) return NotFound();

        try
        {
            // Reschedule if start time changed (preserves duration, sets status to Rescheduled)
            if (existing.StartTime != request.StartTime)
                existing.Reschedule(request.StartTime);

            // Apply explicit status transitions
            switch (request.Status)
            {
                case AppointmentStatus.Canceled when existing.Status != AppointmentStatus.Canceled:
                    existing.Cancel();
                    break;
                case AppointmentStatus.Completed when existing.Status != AppointmentStatus.Completed:
                    existing.Complete();
                    break;
                case AppointmentStatus.Missed when existing.Status != AppointmentStatus.Missed:
                    existing.MarkAsMissed();
                    break;
            }
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Detail = ex.Message });
        }

        existing.TreatmentPlanId = request.TreatmentPlanId;
        existing.Notes = request.Notes;

        await _dbContext.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Marks an appointment as missed and automatically books the next available slot
    /// for the same patient, therapist, and room. If the appointment has a treatment
    /// plan, its end date is extended by 7 days to account for the missed session.
    /// </summary>
    [HttpPost("{id:int}/mark-missed")]
    public async Task<ActionResult<MarkMissedResponse>> MarkMissed(int id, CancellationToken ct)
    {
        try
        {
            var rescheduled = await _missedAppointmentService.MarkMissedAndRescheduleAsync(id, ct);

            var missedAppointment = await _dbContext.Appointments
                .AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.Therapist)
                .Include(x => x.Room)
                .FirstAsync(x => x.Id == id, ct);

            var rescheduledAppointment = await _dbContext.Appointments
                .AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.Therapist)
                .Include(x => x.Room)
                .FirstAsync(x => x.Id == rescheduled.Id, ct);

            return Ok(new MarkMissedResponse
            {
                MissedAppointment = MapToDto(missedAppointment),
                RescheduledAppointment = MapToDto(rescheduledAppointment)
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var appointment = await _dbContext.Appointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (appointment is null) return NotFound();

        _dbContext.Appointments.Remove(appointment);
        await _dbContext.SaveChangesAsync(ct);
        return NoContent();
    }

    private static AppointmentDto MapToDto(Appointment appointment) => new()
    {
        Id = appointment.Id,
        PatientId = appointment.PatientId,
        PatientName = appointment.Patient.FullName,
        TherapistId = appointment.TherapistId,
        TherapistName = appointment.Therapist.FullName,
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
}
