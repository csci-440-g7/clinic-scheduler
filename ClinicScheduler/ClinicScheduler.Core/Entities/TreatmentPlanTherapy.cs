namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Join entity linking a TreatmentPlan to its assigned TherapyTypes.
/// </summary>
public class TreatmentPlanTherapy
{
    public int TreatmentPlanId { get; private set; }
    public TreatmentPlan TreatmentPlan { get; private set; } = null!;
    
    public int TherapyTypeId { get; private set; }
    public TherapyType TherapyType { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private TreatmentPlanTherapy() { }

    // This constructor should be called from within the TreatmentPlan entity.
    internal TreatmentPlanTherapy(TreatmentPlan treatmentPlan, TherapyType therapyType)
    {
        TreatmentPlan = treatmentPlan;
        TherapyType = therapyType;
    }
}