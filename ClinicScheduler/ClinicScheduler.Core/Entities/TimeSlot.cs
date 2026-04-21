namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Represents a configurable scheduling window at a specific location,
/// defined by a start time, end time, and day of week.
/// </summary>
public class TimeSlot
{
    public int Id { get; set; }

    /// <summary>
    /// The start time of the scheduling window.
    /// </summary>
    public TimeOnly StartTime { get; private set; }

    /// <summary>
    /// The end time of the scheduling window. Must be later than <see cref="StartTime"/>.
    /// </summary>
    public TimeOnly EndTime { get; private set; }

    /// <summary>
    /// The day of the week this time slot applies to (Sunday = 0 through Saturday = 6).
    /// </summary>
    public DayOfWeek DayOfWeek { get; private set; }

    /// <summary>
    /// Foreign key reference to the <see cref="Entities.Location"/> this time slot belongs to.
    /// </summary>
    public int LocationId { get; private set; }

    /// <summary>
    /// Navigation property to the parent <see cref="Entities.Location"/>.
    /// </summary>
    public Location Location { get; private set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private TimeSlot() { }

    /// <summary>
    /// Creates a new <see cref="TimeSlot"/> with validated inputs.
    /// </summary>
    /// <param name="startTime">The start time of the scheduling window.</param>
    /// <param name="endTime">The end time of the scheduling window. Must be later than <paramref name="startTime"/>.</param>
    /// <param name="dayOfWeek">The day of the week (Sunday = 0 through Saturday = 6).</param>
    /// <param name="location">The location this time slot belongs to.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="startTime"/> is not earlier than <paramref name="endTime"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="dayOfWeek"/> is not between Sunday (0) and Saturday (6).</exception>
    public TimeSlot(TimeOnly startTime, TimeOnly endTime, DayOfWeek dayOfWeek, Location location)
    {
        if (startTime >= endTime)
        {
            throw new ArgumentException("Start time must be earlier than end time.", nameof(startTime));
        }

        if (!Enum.IsDefined(dayOfWeek))
        {
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "Day of week must be between Sunday (0) and Saturday (6).");
        }

        StartTime = startTime;
        EndTime = endTime;
        DayOfWeek = dayOfWeek;
        Location = location;
        LocationId = location.Id;
    }
}
