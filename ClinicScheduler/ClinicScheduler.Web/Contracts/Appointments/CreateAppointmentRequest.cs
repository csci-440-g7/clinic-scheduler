using System.ComponentModel.DataAnnotations;

namespace ClinicScheduler.Web.Contracts.Appointments;

public sealed class CreateAppointmentRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int PatientId { get; init; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int TherapistId { get; init; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int RoomId { get; init; }
    
    [Range(1, int.MaxValue)]
    public int? TreatmentPlanId { get; init; }
    
    [Required]
    public DateTime StartTime { get; init; }
    
    [Required]
    public DateTime EndTime { get; init; }
    
    [StringLength(2000)]
    public string? Notes { get; init; }
}