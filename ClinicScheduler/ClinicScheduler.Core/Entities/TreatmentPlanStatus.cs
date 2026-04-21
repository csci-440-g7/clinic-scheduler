namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Represents the lifecycle state of a treatment plan.
/// </summary>
public enum TreatmentPlanStatus
{
    /// <summary>
    /// The treatment plan is currently active and in progress.
    /// </summary>
    Active,
    /// <summary>
    /// The treatment plan has been temporarily suspended.
    /// </summary>
    Suspended,
    /// <summary>
    /// The treatment plan has been permanently ended.
    /// </summary>
    Ended
}
