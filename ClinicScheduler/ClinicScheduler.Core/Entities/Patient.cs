namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Represents a patient registered in the clinic system.
/// </summary>
public class Patient
{
    public int Id { get; set; }
    
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public DateOnly DateOfBirth { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TreatmentPlan> TreatmentPlans { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
}