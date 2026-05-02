namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Categorization of scheduling conflicts.
/// </summary>
public enum ConflictType
{
    /// <summary>Overlapping appointments for the same resource.</summary>
    DoubleBook,

    /// <summary>Appointment outside location operating hours.</summary>
    OutsideHours,

    /// <summary>Location daily capacity exceeded.</summary>
    Capacity
}

/// <summary>
/// Records a detected scheduling conflict, categorized by type and tracking resolution status.
/// </summary>
public class ScheduleConflict
{
    public int Id { get; set; }

    /// <summary>
    /// Foreign key reference to the <see cref="Entities.Appointment"/> this conflict is associated with.
    /// </summary>
    public int AppointmentId { get; private set; }

    /// <summary>
    /// Navigation property to the parent <see cref="Entities.Appointment"/>.
    /// </summary>
    public Appointment Appointment { get; private set; } = null!;

    /// <summary>
    /// UTC timestamp when the conflict was detected.
    /// </summary>
    public DateTime DetectedAt { get; private set; }

    /// <summary>
    /// The type of scheduling conflict.
    /// </summary>
    public ConflictType ConflictType { get; private set; }

    /// <summary>
    /// Whether this conflict has been resolved.
    /// </summary>
    public bool Resolved { get; private set; }

    /// <summary>
    /// UTC timestamp when the conflict was resolved, or null if unresolved.
    /// </summary>
    public DateTime? ResolvedAt { get; private set; }

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private ScheduleConflict() { }

    /// <summary>
    /// Creates a new <see cref="ScheduleConflict"/> for the given appointment and conflict type.
    /// </summary>
    /// <param name="appointment">The appointment that has a scheduling conflict.</param>
    /// <param name="conflictType">The type of conflict detected.</param>
    public ScheduleConflict(Appointment appointment, ConflictType conflictType)
    {
        Appointment = appointment;
        AppointmentId = appointment.Id;
        ConflictType = conflictType;
        DetectedAt = DateTime.UtcNow;
        Resolved = false;
        ResolvedAt = null;
    }

    /// <summary>
    /// Marks this conflict as resolved.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the conflict is already resolved.</exception>
    public void Resolve()
    {
        if (Resolved)
        {
            throw new InvalidOperationException("Conflict is already resolved.");
        }

        Resolved = true;
        ResolvedAt = DateTime.UtcNow;
    }
}
