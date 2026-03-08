using ClinicScheduler.Core.Entities;

namespace ClinicScheduler.Web.Contracts.Appointments;

public sealed class AppointmentDto
{
    public int Id { get; init; }

    public int PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    
    public int TherapistId { get; init; }
    public string TherapistName { get; init; } = string.Empty;
    
    public int RoomId { get; init; }
    public string RoomName { get; init; } = string.Empty;
    
    public int? TreatmentPlanId { get; init; }
    
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    
    public AppointmentStatus Status { get; init; }
    public bool HasConflict { get; init; }
    public string? Notes { get; init; }
    
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}