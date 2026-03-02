namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Represents an entry in the "Therapy Description Bank" — a standardized therapy session type.
/// </summary>
public class TherapyType
{
    public int Id { get; set; }
    
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Specialty { get; set; }
    
    /// <summary>
    /// Hex color code for dashboard color-coding (e.g., "#4CAF50").
    /// </summary>
    public string? ColorCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TreatmentPlanTherapy> TreatmentPlanTherapies { get; set; } = [];
}