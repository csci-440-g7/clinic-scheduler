namespace ClinicScheduler.Core.Entities;

public class AppointmentRequest
{
    public int Id { get; private set; }
    public int PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public DateTime PreferredDateTime { get; private set; }
    public string? Notes { get; private set; }
    public AppointmentRequestStatus Status { get; private set; } = AppointmentRequestStatus.Pending;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public int? PreferredTherapistId { get; private set; }
    public Therapist? PreferredTherapist { get; private set; }
    public int? AppointmentId { get; private set; }
    public Appointment? Appointment { get; private set; }

    private AppointmentRequest() { }

    public AppointmentRequest(Patient patient, string? notes = null, Therapist? preferredTherapist = null)
    {
        Patient = patient;
        PatientId = patient.Id;
        PreferredDateTime = DateTime.UtcNow;
        Notes = notes;
        PreferredTherapist = preferredTherapist;
        PreferredTherapistId = preferredTherapist?.Id;
        Status = AppointmentRequestStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public string? DenialReason { get; private set; }

    public void SetPreferredDateTime(DateTime preferredDateTime)
    {
        PreferredDateTime = preferredDateTime;
    }

    public void Approve(Appointment appointment)
    {
        Status = AppointmentRequestStatus.Approved;
        Appointment = appointment;
        AppointmentId = appointment.Id;
    }

    public void Deny(string? reason = null)
    {
        Status = AppointmentRequestStatus.Denied;
        DenialReason = reason;
    }
}
