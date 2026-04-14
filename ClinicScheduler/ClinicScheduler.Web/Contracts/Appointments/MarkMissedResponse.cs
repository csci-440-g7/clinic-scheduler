namespace ClinicScheduler.Web.Contracts.Appointments;

/// <summary>
/// Response returned when an appointment is marked as missed and rescheduled.
/// </summary>
public sealed class MarkMissedResponse
{
    /// <summary>The appointment that was marked as missed.</summary>
    public AppointmentDto MissedAppointment { get; init; } = null!;

    /// <summary>The newly created rescheduled appointment.</summary>
    public AppointmentDto RescheduledAppointment { get; init; } = null!;
}
