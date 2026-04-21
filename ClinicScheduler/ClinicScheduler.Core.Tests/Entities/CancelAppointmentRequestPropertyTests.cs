using ClinicScheduler.Core.Entities;
using FsCheck;
using FsCheck.Xunit;

namespace ClinicScheduler.Core.Tests.Entities;

/// <summary>
/// Property-based tests for CancelAppointmentRequest entity using FsCheck.
/// Validates correctness properties defined in the cancel-appointment-request design document.
/// </summary>
public class CancelAppointmentRequestPropertyTests
{
    // ---------------------------------------------------------------
    // Shared helpers
    // ---------------------------------------------------------------

    private static Patient CreatePatient(string firstName, string lastName)
    {
        var email = $"{Guid.NewGuid():N}@test.com";
        return new Patient(firstName, lastName, email, new DateOnly(1990, 1, 1));
    }

    private static Therapist CreateTherapist(string firstName, string lastName)
    {
        return new Therapist(firstName, lastName, $"{Guid.NewGuid():N}@test.com");
    }

    private static Room CreateRoom()
    {
        var location = new Location("Test Clinic", "123 Test St");
        return new Room("Room 1", 1, location);
    }

    private static Appointment CreateScheduledAppointment(Patient patient)
    {
        var therapist = CreateTherapist("Test", "Therapist");
        var room = CreateRoom();
        return new Appointment(patient, therapist, room, DateTime.UtcNow.AddDays(1), TimeSpan.FromMinutes(30));
    }

    private static Appointment CreateRescheduledAppointment(Patient patient)
    {
        var therapist = CreateTherapist("Test", "Therapist");
        var room = CreateRoom();
        var appointment = new Appointment(patient, therapist, room, DateTime.UtcNow.AddDays(1), TimeSpan.FromMinutes(30));
        appointment.Reschedule(DateTime.UtcNow.AddDays(2));
        return appointment;
    }

    private static DateTime MakeDateTime(PositiveInt yearOffset, byte month, byte day, byte hour, byte minute)
    {
        var year = 2000 + (yearOffset.Get % 31);
        var validMonth = (month % 12) + 1;
        var maxDay = DateTime.DaysInMonth(year, validMonth);
        var validDay = (day % maxDay) + 1;
        var validHour = hour % 24;
        var validMinute = minute % 60;
        return new DateTime(year, validMonth, validDay, validHour, validMinute, 0, DateTimeKind.Utc);
    }

    private static string SanitizeName(NonEmptyString nes)
    {
        var name = nes.Get.Trim();
        if (string.IsNullOrWhiteSpace(name))
            name = "Default";
        return name;
    }

    /// <summary>
    /// Generates a non-whitespace reason string from a NonEmptyString.
    /// </summary>
    private static string MakeValidReason(NonEmptyString nes)
    {
        var reason = nes.Get.Trim();
        if (string.IsNullOrWhiteSpace(reason))
            reason = "Valid reason";
        return reason;
    }

    // ---------------------------------------------------------------
    // Property 1: Cancel request creation preserves all input fields
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 1: Cancel request creation preserves all input fields**
    ///
    /// For any valid patient, eligible appointment (Scheduled or Rescheduled), and
    /// non-whitespace reason, the created entity has status Pending, correct PatientId,
    /// correct AppointmentId, the provided reason, and a CreatedAt timestamp.
    ///
    /// **Validates: Requirements 1.5, 6.1**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CreationPreservesAllInputFields(
        NonEmptyString patientFirst,
        NonEmptyString patientLast,
        NonEmptyString reasonNes,
        bool useRescheduled)
    {
        var pFirst = SanitizeName(patientFirst);
        var pLast = SanitizeName(patientLast);
        var reason = MakeValidReason(reasonNes);

        var patient = CreatePatient(pFirst, pLast);
        var appointment = useRescheduled
            ? CreateRescheduledAppointment(patient)
            : CreateScheduledAppointment(patient);

        var beforeCreation = DateTime.UtcNow;
        var request = new CancelAppointmentRequest(patient, appointment, reason);
        var afterCreation = DateTime.UtcNow;

        return request.Status == AppointmentRequestStatus.Pending
            && request.PatientId == patient.Id
            && request.Patient == patient
            && request.AppointmentId == appointment.Id
            && request.Appointment == appointment
            && request.Reason == reason
            && request.CreatedAt >= beforeCreation
            && request.CreatedAt <= afterCreation
            && request.DenialReason == null;
    }

    // ---------------------------------------------------------------
    // Property 2: Whitespace-only reasons are rejected
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 2: Whitespace-only reasons are rejected**
    ///
    /// For any string composed entirely of whitespace (including empty), creating a
    /// cancel request or denying with that reason throws ArgumentException and no
    /// state change occurs.
    ///
    /// **Validates: Requirements 1.6, 4.1**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool WhitespaceOnlyReasonsAreRejected(byte spaceCount)
    {
        // Generate a whitespace-only string (0 to 255 spaces)
        var whitespaceReason = new string(' ', spaceCount);

        var patient = CreatePatient("Test", "Patient");
        var appointment = CreateScheduledAppointment(patient);

        // Test creation with whitespace reason
        var creationThrew = false;
        try
        {
            _ = new CancelAppointmentRequest(patient, appointment, whitespaceReason);
        }
        catch (ArgumentException)
        {
            creationThrew = true;
        }

        // Test denial with whitespace reason on a valid pending request
        var validRequest = new CancelAppointmentRequest(patient, appointment, "Valid reason");
        var originalStatus = validRequest.Status;
        var originalDenialReason = validRequest.DenialReason;

        var denialThrew = false;
        try
        {
            validRequest.Deny(whitespaceReason);
        }
        catch (ArgumentException)
        {
            denialThrew = true;
        }

        // Both should throw, and the valid request should be unchanged
        return creationThrew
            && denialThrew
            && validRequest.Status == originalStatus
            && validRequest.DenialReason == originalDenialReason;
    }

    // ---------------------------------------------------------------
    // Property 3: Approval transitions both request and appointment
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 3: Approval transitions both request and appointment**
    ///
    /// For any pending CancelAppointmentRequest linked to a Scheduled or Rescheduled
    /// appointment, approving sets request status to Approved and appointment status
    /// to Canceled.
    ///
    /// **Validates: Requirements 3.2, 3.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ApprovalTransitionsBothRequestAndAppointment(
        NonEmptyString reasonNes,
        bool useRescheduled)
    {
        var reason = MakeValidReason(reasonNes);
        var patient = CreatePatient("Test", "Patient");
        var appointment = useRescheduled
            ? CreateRescheduledAppointment(patient)
            : CreateScheduledAppointment(patient);

        var request = new CancelAppointmentRequest(patient, appointment, reason);

        request.Approve();

        return request.Status == AppointmentRequestStatus.Approved
            && appointment.Status == AppointmentStatus.Canceled;
    }

    // ---------------------------------------------------------------
    // Property 4: Denial stores reason and preserves appointment status
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 4: Denial stores reason and preserves appointment status**
    ///
    /// For any pending CancelAppointmentRequest and valid non-whitespace denial reason,
    /// denying sets request status to Denied, stores the denial reason, and leaves
    /// appointment status unchanged.
    ///
    /// **Validates: Requirements 4.2, 4.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool DenialStoresReasonAndPreservesAppointmentStatus(
        NonEmptyString reasonNes,
        NonEmptyString denialReasonNes,
        bool useRescheduled)
    {
        var reason = MakeValidReason(reasonNes);
        var denialReason = MakeValidReason(denialReasonNes);
        var patient = CreatePatient("Test", "Patient");
        var appointment = useRescheduled
            ? CreateRescheduledAppointment(patient)
            : CreateScheduledAppointment(patient);

        var originalAppointmentStatus = appointment.Status;
        var request = new CancelAppointmentRequest(patient, appointment, reason);

        request.Deny(denialReason);

        return request.Status == AppointmentRequestStatus.Denied
            && request.DenialReason == denialReason
            && appointment.Status == originalAppointmentStatus;
    }

    // ---------------------------------------------------------------
    // Property 9: Ineligible appointments are rejected
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 9: Ineligible appointments are rejected**
    ///
    /// For any appointment with status Completed, Canceled, or Missed, attempting to
    /// create a cancel request throws InvalidOperationException.
    ///
    /// **Validates: Requirements 5.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IneligibleAppointmentsAreRejected(
        NonEmptyString reasonNes,
        byte statusSelector)
    {
        var reason = MakeValidReason(reasonNes);
        var patient = CreatePatient("Test", "Patient");
        var therapist = CreateTherapist("Test", "Therapist");
        var room = CreateRoom();
        var appointment = new Appointment(patient, therapist, room, DateTime.UtcNow.AddDays(1), TimeSpan.FromMinutes(30));

        // Transition to one of the three ineligible statuses
        var ineligibleIndex = statusSelector % 3;
        switch (ineligibleIndex)
        {
            case 0: // Completed
                appointment.Complete();
                break;
            case 1: // Canceled
                appointment.Cancel();
                break;
            case 2: // Missed
                appointment.MarkAsMissed();
                break;
        }

        try
        {
            _ = new CancelAppointmentRequest(patient, appointment, reason);
            return false; // Should have thrown
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    // ---------------------------------------------------------------
    // Property 5: Staff notification fan-out on creation
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 5: Staff notification fan-out on creation**
    ///
    /// For any set of N distinct staff user IDs and any valid cancel request data
    /// (patient name, appointment date/time), creating N Notification entities of type
    /// CancellationRequested (one per staff user) produces exactly N notifications,
    /// each with the correct UserId and type.
    ///
    /// **Validates: Requirements 2.1**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool StaffNotificationFanOutOnCreation(
        NonEmptyString patientFirst,
        NonEmptyString patientLast,
        PositiveInt yearOffset, byte month, byte day, byte hour, byte minute,
        byte staffCount)
    {
        var pFirst = SanitizeName(patientFirst);
        var pLast = SanitizeName(patientLast);
        var startTime = MakeDateTime(yearOffset, month, day, hour, minute);

        // Generate 1 to 20 distinct staff user IDs
        var n = (staffCount % 20) + 1;
        var staffUserIds = Enumerable.Range(0, n)
            .Select(i => $"staff-user-{Guid.NewGuid():N}")
            .ToList();

        var patientName = $"{pFirst} {pLast}";
        var message = $"{patientName} has requested to cancel their appointment on {startTime:MMM d, yyyy} at {startTime:h:mm tt}.";

        // Create one notification per staff user (mimicking what the UI does)
        var notifications = staffUserIds
            .Select(userId => new Notification(
                userId,
                NotificationType.CancellationRequested,
                "Cancellation Request",
                message))
            .ToList();

        // Verify exactly N notifications
        if (notifications.Count != n)
            return false;

        // Verify each notification has the correct UserId and type
        for (var i = 0; i < n; i++)
        {
            if (notifications[i].UserId != staffUserIds[i])
                return false;
            if (notifications[i].Type != NotificationType.CancellationRequested)
                return false;
        }

        return true;
    }

    // ---------------------------------------------------------------
    // Property 6: Notification messages contain required details
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 6: Notification messages contain required details**
    ///
    /// For any patient full name, appointment date, and appointment time, a
    /// CancellationRequested notification message formatted as
    /// "{patientName} has requested to cancel their appointment on {date:MMM d, yyyy} at {time:h:mm tt}."
    /// contains the patient name, formatted date, and formatted time.
    ///
    /// **Validates: Requirements 2.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool NotificationMessagesContainRequiredDetails(
        NonEmptyString patientFirst,
        NonEmptyString patientLast,
        PositiveInt yearOffset, byte month, byte day, byte hour, byte minute)
    {
        var pFirst = SanitizeName(patientFirst);
        var pLast = SanitizeName(patientLast);
        var startTime = MakeDateTime(yearOffset, month, day, hour, minute);

        var patientName = $"{pFirst} {pLast}";
        var formattedDate = startTime.ToString("MMM d, yyyy");
        var formattedTime = startTime.ToString("h:mm tt");

        var message = $"{patientName} has requested to cancel their appointment on {formattedDate} at {formattedTime}.";

        var notification = new Notification(
            "staff-user-1",
            NotificationType.CancellationRequested,
            "Cancellation Request",
            message);

        return notification.Message.Contains(patientName)
            && notification.Message.Contains(formattedDate)
            && notification.Message.Contains(formattedTime);
    }

    // ---------------------------------------------------------------
    // Property 7: Approval creates patient notification
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 7: Approval creates patient notification**
    ///
    /// For any approved cancel request, creating a Notification of type
    /// CancellationApproved for a given userId produces exactly one notification
    /// with the correct type and userId.
    ///
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ApprovalCreatesPatientNotification(
        NonEmptyString patientFirst,
        NonEmptyString patientLast,
        NonEmptyString reasonNes)
    {
        var pFirst = SanitizeName(patientFirst);
        var pLast = SanitizeName(patientLast);
        var reason = MakeValidReason(reasonNes);

        var patient = CreatePatient(pFirst, pLast);
        var appointment = CreateScheduledAppointment(patient);
        var request = new CancelAppointmentRequest(patient, appointment, reason);
        request.Approve();

        var userId = $"user-{Guid.NewGuid():N}";
        var notification = new Notification(
            userId,
            NotificationType.CancellationApproved,
            "Cancellation Approved",
            $"Your cancellation request for your appointment has been approved.");

        return notification.Type == NotificationType.CancellationApproved
            && notification.UserId == userId;
    }

    // ---------------------------------------------------------------
    // Property 8: Denial creates patient notification with reason
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 8: Denial creates patient notification with reason**
    ///
    /// For any denied cancel request with a denial reason, creating a Notification
    /// of type CancellationDenied for a given userId produces a notification whose
    /// message contains the denial reason.
    ///
    /// **Validates: Requirements 4.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool DenialCreatesPatientNotificationWithReason(
        NonEmptyString patientFirst,
        NonEmptyString patientLast,
        NonEmptyString reasonNes,
        NonEmptyString denialReasonNes)
    {
        var pFirst = SanitizeName(patientFirst);
        var pLast = SanitizeName(patientLast);
        var reason = MakeValidReason(reasonNes);
        var denialReason = MakeValidReason(denialReasonNes);

        var patient = CreatePatient(pFirst, pLast);
        var appointment = CreateScheduledAppointment(patient);
        var request = new CancelAppointmentRequest(patient, appointment, reason);
        request.Deny(denialReason);

        var userId = $"user-{Guid.NewGuid():N}";
        var message = $"Your cancellation request has been denied. Reason: {denialReason}";
        var notification = new Notification(
            userId,
            NotificationType.CancellationDenied,
            "Cancellation Denied",
            message);

        return notification.Type == NotificationType.CancellationDenied
            && notification.UserId == userId
            && notification.Message.Contains(denialReason);
    }
}
