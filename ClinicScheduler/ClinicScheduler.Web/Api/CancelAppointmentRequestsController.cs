using ClinicScheduler.Core.Entities;
using ClinicScheduler.Infrastructure.Data;
using ClinicScheduler.Web.Contracts.CancelAppointmentRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicScheduler.Web.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CancelAppointmentRequestsController : ControllerBase
{
    private readonly ClinicDbContext _dbContext;

    public CancelAppointmentRequestsController(ClinicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<ActionResult<CancelAppointmentRequestDto>> Create(
        CreateCancelAppointmentRequestDto request,
        CancellationToken ct)
    {
        var appointment = await _dbContext.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Therapist)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct);

        if (appointment is null)
            return NotFound();

        if (appointment.Status is not (AppointmentStatus.Scheduled or AppointmentStatus.Rescheduled))
            return Conflict(new ProblemDetails
            {
                Detail = $"Cannot request cancellation for an appointment with status {appointment.Status}. Only Scheduled or Rescheduled appointments can be canceled."
            });

        var hasPendingRequest = await _dbContext.CancelAppointmentRequests
            .AnyAsync(r => r.AppointmentId == request.AppointmentId
                        && r.Status == AppointmentRequestStatus.Pending, ct);

        if (hasPendingRequest)
            return Conflict(new ProblemDetails
            {
                Detail = "A pending cancellation request already exists for this appointment."
            });

        var cancelRequest = new CancelAppointmentRequest(
            appointment.Patient,
            appointment,
            request.Reason);

        _dbContext.CancelAppointmentRequests.Add(cancelRequest);
        await _dbContext.SaveChangesAsync(ct);

        var created = await _dbContext.CancelAppointmentRequests
            .AsNoTracking()
            .Include(r => r.Patient)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Therapist)
            .FirstAsync(r => r.Id == cancelRequest.Id, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CancelAppointmentRequestDto>>> GetAll(CancellationToken ct)
    {
        var isStaffOrAbove = User.IsInRole(RoleNames.Admin)
            || User.IsInRole(RoleNames.ClinicManager)
            || User.IsInRole(RoleNames.Staff)
            || User.IsInRole(RoleNames.Therapist);

        if (!isStaffOrAbove && !User.IsInRole(RoleNames.Patient))
            return Forbid();

        var query = _dbContext.CancelAppointmentRequests
            .AsNoTracking()
            .Include(r => r.Patient)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Therapist)
            .AsQueryable();

        if (User.IsInRole(RoleNames.Patient) && !isStaffOrAbove)
        {
            var userEmail = User.Identity?.Name;
            query = query.Where(r => r.Patient.Email == userEmail);
        }

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return Ok(requests.Select(static r => MapToDto(r)).ToList());
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = RoleNames.StaffOrAbove)]
    public async Task<ActionResult<CancelAppointmentRequestDto>> GetById(int id, CancellationToken ct)
    {
        var request = await _dbContext.CancelAppointmentRequests
            .AsNoTracking()
            .Include(r => r.Patient)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Therapist)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        return request is null ? NotFound() : Ok(MapToDto(request));
    }

    private static CancelAppointmentRequestDto MapToDto(CancelAppointmentRequest request) => new()
    {
        Id = request.Id,
        PatientId = request.PatientId,
        PatientName = request.Patient.FullName,
        AppointmentId = request.AppointmentId,
        Reason = request.Reason,
        Status = request.Status,
        CreatedAt = request.CreatedAt,
        DenialReason = request.DenialReason
    };
}
