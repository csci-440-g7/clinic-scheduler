using System.ComponentModel.DataAnnotations;

namespace ClinicScheduler.Web.Contracts.CancelAppointmentRequests;

/// <summary>
/// Request model for creating a new cancellation request.
/// </summary>
public sealed class CreateCancelAppointmentRequestDto
{
    /// <summary>
    /// Identifier of the appointment to request cancellation for.
    /// </summary>
    /// <example>12345</example>
    [Required]
    [Range(1, int.MaxValue)]
    public int AppointmentId { get; init; }

    /// <summary>
    /// Reason for requesting the cancellation.
    /// </summary>
    /// <example>I have a scheduling conflict and cannot attend.</example>
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Reason { get; init; } = string.Empty;
}
