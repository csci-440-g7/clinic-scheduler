namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Join entity linking a TreatmentPlan to its assigned TherapyTypes.
/// </summary>
public class TreatmentPlanTherapy
{
    public int TreatmentPlanId { get; set; }
    public TreatmentPlan TreatmentPlan { get; set; } = null!;
    
    public int TherapyTypeId { get; set; }
    public TherapyType TherapyType { get; set; } = null!;
}