using ClinicScheduler.Core.Entities;
using ClinicScheduler.Infrastructure.Data;
using ClinicScheduler.Web.Services;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicScheduler.Core.Tests;

/// <summary>
/// Property-based tests for AppointmentNotificationService using FsCheck.
/// Validates correctness properties defined in the appointment-notifications design document.
/// </summary>
public class AppointmentNotificationPropertyTests
{
    // ---------------------------------------------------------------
    // Shared infrastructure: helpers and generators for all property tests
    // ---------------------------------------------------------------

    /// <summary>
    /// Creates a fresh in-memory ClinicDbContext with a unique database name.
    /// </summary>
    internal static ClinicDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ClinicDbContext(options);
    }

    /// <summary>
    /// Creates an AppointmentNotificationService backed by the given context.
    /// </summary>
    internal static AppointmentNotificationService CreateService(ClinicDbContext db)
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<AppointmentNotificationService>();
        return new AppointmentNotificationService(db, logger);
    }

    /// <summary>
    /// Creates a Patient entity with the given names and a unique email.
    /// </summary>
    internal static Patient CreatePatient(string firstName, string lastName)
    {
        var email = $"{Guid.NewGuid():N}@test.com";
        return new Patient(firstName, lastName, email, new DateOnly(1990, 1, 1));
    }

    /// <summary>
    /// Creates a Therapist entity with the given names and a unique email.
    /// </summary>
    internal static Therapist CreateTherapist(string firstName, string lastName)
    {
        return new Therapist(firstName, lastName, $"{Guid.NewGuid():N}@test.com");
    }

    /// <summary>
    /// Creates a Room entity with a Location.
    /// </summary>
    internal static Room CreateRoom(string name)
    {
        var location = new Location("Test Clinic", "123 Test St");
        return new Room(name, 1, location);
    }

    /// <summary>
    /// Creates a valid Appointment with all required navigation properties populated.
    /// </summary>
    internal static Appointment CreateAppointment(
        Patient patient, Therapist therapist, Room room, DateTime startTime)
    {
        return new Appointment(patient, therapist, room, startTime, TimeSpan.FromMinutes(60));
    }

    /// <summary>
    /// Seeds the in-memory database with the given entities and an AppUser whose
    /// UserName matches the patient's email (required for user lookup).
    /// Returns the saved Appointment with its Id set.
    /// </summary>
    internal static async Task<Appointment> SeedDatabaseAsync(
        ClinicDbContext db, Patient patient, Therapist therapist, Room room, Appointment appointment)
    {
        db.Patients.Add(patient);
        db.Therapists.Add(therapist);
        db.Rooms.Add(room);
        db.Appointments.Add(appointment);

        // Create an AppUser with UserName matching the patient's email
        var appUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = patient.Email,
            NormalizedUserName = patient.Email.ToUpperInvariant(),
            Email = patient.Email,
            NormalizedEmail = patient.Email.ToUpperInvariant(),
            DisplayName = patient.FullName,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(appUser);

        await db.SaveChangesAsync();
        return appointment;
    }

    /// <summary>
    /// Constrains FsCheck-generated values into a valid DateTime in a reasonable range.
    /// Uses UTC kind to avoid timezone ambiguity in tests.
    /// </summary>
    internal static DateTime MakeDateTime(PositiveInt yearOffset, byte month, byte day, byte hour, byte minute)
    {
        var year = 2000 + (yearOffset.Get % 31); // 2000-2030
        var validMonth = (month % 12) + 1;
        var maxDay = DateTime.DaysInMonth(year, validMonth);
        var validDay = (day % maxDay) + 1;
        var validHour = hour % 24;
        var validMinute = minute % 60;
        return new DateTime(year, validMonth, validDay, validHour, validMinute, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// Sanitizes a NonEmptyString to produce a valid name (non-whitespace, no special chars that break formatting).
    /// </summary>
    internal static string SanitizeName(NonEmptyString nes)
    {
        // Trim and ensure at least one visible character
        var name = nes.Get.Trim();
        if (string.IsNullOrWhiteSpace(name))
            name = "Default";
        return name;
    }

    // ---------------------------------------------------------------
    // Property 1: Creation notification produces correct type and message content
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 1: Creation notification produces correct type and message content**
    ///
    /// For any valid appointment with a resolvable patient AppUser, calling
    /// NotifyAppointmentCreatedAsync produces exactly one Notification of type
    /// AppointmentCreated whose message contains the therapist full name,
    /// appointment date, and appointment time.
    ///
    /// **Validates: Requirements 1.1, 1.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task<bool> CreationNotification_ProducesCorrectTypeAndMessageContent(
        NonEmptyString patientFirst, NonEmptyString patientLast,
        NonEmptyString therapistFirst, NonEmptyString therapistLast,
        NonEmptyString roomName,
        PositiveInt yearOffset, byte month, byte day, byte hour, byte minute)
    {
        // Arrange
        var pFirst = SanitizeName(patientFirst);
        var pLast = SanitizeName(patientLast);
        var tFirst = SanitizeName(therapistFirst);
        var tLast = SanitizeName(therapistLast);
        var rName = SanitizeName(roomName);

        var startTime = MakeDateTime(yearOffset, month, day, hour, minute);

        await using var db = CreateInMemoryDb();
        var patient = CreatePatient(pFirst, pLast);
        var therapist = CreateTherapist(tFirst, tLast);
        var room = CreateRoom(rName);
        var appointment = CreateAppointment(patient, therapist, room, startTime);
        await SeedDatabaseAsync(db, patient, therapist, room, appointment);

        var service = CreateService(db);

        // Act
        await service.NotifyAppointmentCreatedAsync(appointment);

        // Assert
        var notifications = await db.Notifications.ToListAsync();

        // Exactly one notification produced
        if (notifications.Count != 1)
            return false;

        var notification = notifications[0];

        // Type is AppointmentCreated
        if (notification.Type != NotificationType.AppointmentCreated)
            return false;

        // The service uses appointment.StartTime.ToLocalTime() for formatting
        var localTime = startTime.ToLocalTime();
        var expectedTherapistName = $"{tFirst} {tLast}";
        var expectedDate = localTime.ToString("ddd, MMM d");
        var expectedTime = localTime.ToString("h:mm tt");

        // Message contains therapist full name
        if (!notification.Message.Contains(expectedTherapistName))
            return false;

        // Message contains appointment date
        if (!notification.Message.Contains(expectedDate))
            return false;

        // Message contains appointment time
        if (!notification.Message.Contains(expectedTime))
            return false;

        return true;
    }

    // ---------------------------------------------------------------
    // Property 2: Reschedule notification includes both old and new times
    // ---------------------------------------------------------------


    /// <summary>
    /// **Property 2: Reschedule notification includes both old and new times**
    ///
    /// For any valid appointment reschedule with a resolvable patient AppUser, calling
    /// NotifyAppointmentRescheduledAsync produces exactly one Notification of type
    /// AppointmentRescheduled whose message contains both the original date/time and
    /// the new date/time.
    ///
    /// **Validates: Requirements 2.1, 2.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task<bool> RescheduleNotification_IncludesBothOldAndNewTimes(
        NonEmptyString patientFirst, NonEmptyString patientLast,
        NonEmptyString therapistFirst, NonEmptyString therapistLast,
        NonEmptyString roomName,
        PositiveInt origYearOffset, byte origMonth, byte origDay, byte origHour, byte origMinute,
        PositiveInt newYearOffset, byte newMonth, byte newDay, byte newHour, byte newMinute)
    {
        // Arrange
        var pFirst = SanitizeName(patientFirst);
        var pLast = SanitizeName(patientLast);
        var tFirst = SanitizeName(therapistFirst);
        var tLast = SanitizeName(therapistLast);
        var rName = SanitizeName(roomName);

        var originalStartTime = MakeDateTime(origYearOffset, origMonth, origDay, origHour, origMinute);
        var newStartTime = MakeDateTime(newYearOffset, newMonth, newDay, newHour, newMinute);

        // The appointment entity holds the NEW start time
        await using var db = CreateInMemoryDb();
        var patient = CreatePatient(pFirst, pLast);
        var therapist = CreateTherapist(tFirst, tLast);
        var room = CreateRoom(rName);
        var appointment = CreateAppointment(patient, therapist, room, newStartTime);
        await SeedDatabaseAsync(db, patient, therapist, room, appointment);

        var service = CreateService(db);

        // Act — pass the original start time as a parameter
        var originalEndTime = originalStartTime.AddMinutes(60);
        await service.NotifyAppointmentRescheduledAsync(appointment, originalStartTime, originalEndTime);

        // Assert
        var notifications = await db.Notifications.ToListAsync();

        // Exactly one notification produced
        if (notifications.Count != 1)
            return false;

        var notification = notifications[0];

        // Type is AppointmentRescheduled
        if (notification.Type != NotificationType.AppointmentRescheduled)
            return false;

        // The service formats using .ToLocalTime()
        var oldLocal = originalStartTime.ToLocalTime();
        var newLocal = newStartTime.ToLocalTime();

        var expectedOldDate = oldLocal.ToString("ddd, MMM d");
        var expectedOldTime = oldLocal.ToString("h:mm tt");
        var expectedNewDate = newLocal.ToString("ddd, MMM d");
        var expectedNewTime = newLocal.ToString("h:mm tt");

        // Message contains original date
        if (!notification.Message.Contains(expectedOldDate))
            return false;

        // Message contains original time
        if (!notification.Message.Contains(expectedOldTime))
            return false;

        // Message contains new date
        if (!notification.Message.Contains(expectedNewDate))
            return false;

        // Message contains new time
        if (!notification.Message.Contains(expectedNewTime))
            return false;

        return true;
    }

    // ---------------------------------------------------------------
    // Property 3: Cancellation notification produces correct type and message content
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 3: Cancellation notification produces correct type and message content**
    ///
    /// For any valid appointment cancellation with a resolvable patient AppUser, calling
    /// NotifyAppointmentCancelledAsync produces exactly one Notification of type
    /// CancellationApproved whose message contains the therapist full name and the
    /// original appointment date and time.
    ///
    /// **Validates: Requirements 3.1, 3.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task<bool> CancellationNotification_ProducesCorrectTypeAndMessageContent(
        NonEmptyString patientFirst, NonEmptyString patientLast,
        NonEmptyString therapistFirst, NonEmptyString therapistLast,
        NonEmptyString roomName,
        PositiveInt yearOffset, byte month, byte day, byte hour, byte minute)
    {
        // Arrange
        var pFirst = SanitizeName(patientFirst);
        var pLast = SanitizeName(patientLast);
        var tFirst = SanitizeName(therapistFirst);
        var tLast = SanitizeName(therapistLast);
        var rName = SanitizeName(roomName);

        var startTime = MakeDateTime(yearOffset, month, day, hour, minute);

        await using var db = CreateInMemoryDb();
        var patient = CreatePatient(pFirst, pLast);
        var therapist = CreateTherapist(tFirst, tLast);
        var room = CreateRoom(rName);
        var appointment = CreateAppointment(patient, therapist, room, startTime);
        await SeedDatabaseAsync(db, patient, therapist, room, appointment);

        var service = CreateService(db);

        // Act
        await service.NotifyAppointmentCancelledAsync(appointment);

        // Assert
        var notifications = await db.Notifications.ToListAsync();

        // Exactly one notification produced
        if (notifications.Count != 1)
            return false;

        var notification = notifications[0];

        // Type is CancellationApproved
        if (notification.Type != NotificationType.CancellationApproved)
            return false;

        // The service uses appointment.StartTime.ToLocalTime() for formatting
        var localTime = startTime.ToLocalTime();
        var expectedTherapistName = $"{tFirst} {tLast}";
        var expectedDate = localTime.ToString("ddd, MMM d");
        var expectedTime = localTime.ToString("h:mm tt");

        // Message contains therapist full name
        if (!notification.Message.Contains(expectedTherapistName))
            return false;

        // Message contains appointment date
        if (!notification.Message.Contains(expectedDate))
            return false;

        // Message contains appointment time
        if (!notification.Message.Contains(expectedTime))
            return false;

        return true;
    }

    // ---------------------------------------------------------------
    // Property 4: Update notification describes the change
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 4: Update notification describes the change**
    ///
    /// For any valid appointment detail update with a resolvable patient AppUser,
    /// calling NotifyAppointmentUpdatedAsync with a change description produces
    /// exactly one Notification of type AppointmentUpdated whose message contains
    /// the provided change description.
    ///
    /// **Validates: Requirements 4.1, 4.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task<bool> UpdateNotification_DescribesTheChange(
        NonEmptyString patientFirst, NonEmptyString patientLast,
        NonEmptyString therapistFirst, NonEmptyString therapistLast,
        NonEmptyString roomName,
        PositiveInt yearOffset, byte month, byte day, byte hour, byte minute,
        NonEmptyString changeDesc)
    {
        // Arrange
        var pFirst = SanitizeName(patientFirst);
        var pLast = SanitizeName(patientLast);
        var tFirst = SanitizeName(therapistFirst);
        var tLast = SanitizeName(therapistLast);
        var rName = SanitizeName(roomName);
        var changeDescription = SanitizeName(changeDesc);

        var startTime = MakeDateTime(yearOffset, month, day, hour, minute);

        await using var db = CreateInMemoryDb();
        var patient = CreatePatient(pFirst, pLast);
        var therapist = CreateTherapist(tFirst, tLast);
        var room = CreateRoom(rName);
        var appointment = CreateAppointment(patient, therapist, room, startTime);
        await SeedDatabaseAsync(db, patient, therapist, room, appointment);

        var service = CreateService(db);

        // Act
        await service.NotifyAppointmentUpdatedAsync(appointment, changeDescription);

        // Assert
        var notifications = await db.Notifications.ToListAsync();

        // Exactly one notification produced
        if (notifications.Count != 1)
            return false;

        var notification = notifications[0];

        // Type is AppointmentUpdated
        if (notification.Type != NotificationType.AppointmentUpdated)
            return false;

        // Message contains the provided change description
        if (!notification.Message.Contains(changeDescription))
            return false;

        return true;
    }

    // ---------------------------------------------------------------
    // Property 5: RelatedAppointmentId invariant
    // ---------------------------------------------------------------

    /// <summary>
    /// Helper that invokes one of the four notification methods and returns the
    /// single notification produced, or null if none was created.
    /// </summary>
    private static async Task<Notification?> InvokeNotificationMethodAsync(
        AppointmentNotificationService service,
        ClinicDbContext db,
        Appointment appointment,
        int eventIndex,
        DateTime originalStartTime,
        string changeDescription)
    {
        switch (eventIndex)
        {
            case 0:
                await service.NotifyAppointmentCreatedAsync(appointment);
                break;
            case 1:
                var originalEndTime = originalStartTime.AddMinutes(60);
                await service.NotifyAppointmentRescheduledAsync(appointment, originalStartTime, originalEndTime);
                break;
            case 2:
                await service.NotifyAppointmentCancelledAsync(appointment);
                break;
            case 3:
                await service.NotifyAppointmentUpdatedAsync(appointment, changeDescription);
                break;
        }

        return await db.Notifications.FirstOrDefaultAsync();
    }

    /// <summary>
    /// **Property 5: RelatedAppointmentId invariant**
    ///
    /// For any appointment notification event (created, rescheduled, cancelled, or updated)
    /// where a notification is produced, the notification's RelatedAppointmentId equals the
    /// source appointment's Id.
    ///
    /// Parameterized across all four event types by generating an event index [0..3].
    ///
    /// **Validates: Requirements 1.4, 2.3, 3.3, 4.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task<bool> RelatedAppointmentId_EqualsSourceAppointmentId(
        NonEmptyString patientFirst, NonEmptyString patientLast,
        NonEmptyString therapistFirst, NonEmptyString therapistLast,
        NonEmptyString roomName,
        PositiveInt yearOffset, byte month, byte day, byte hour, byte minute,
        PositiveInt origYearOffset, byte origMonth, byte origDay, byte origHour, byte origMinute,
        NonEmptyString changeDesc,
        byte eventSelector)
    {
        // Arrange
        var pFirst = SanitizeName(patientFirst);
        var pLast = SanitizeName(patientLast);
        var tFirst = SanitizeName(therapistFirst);
        var tLast = SanitizeName(therapistLast);
        var rName = SanitizeName(roomName);
        var changeDescription = SanitizeName(changeDesc);

        // Select one of the four event types
        var eventIndex = eventSelector % 4;

        var startTime = MakeDateTime(yearOffset, month, day, hour, minute);
        var originalStartTime = MakeDateTime(origYearOffset, origMonth, origDay, origHour, origMinute);

        await using var db = CreateInMemoryDb();
        var patient = CreatePatient(pFirst, pLast);
        var therapist = CreateTherapist(tFirst, tLast);
        var room = CreateRoom(rName);
        var appointment = CreateAppointment(patient, therapist, room, startTime);
        await SeedDatabaseAsync(db, patient, therapist, room, appointment);

        var service = CreateService(db);

        // Act
        var notification = await InvokeNotificationMethodAsync(
            service, db, appointment, eventIndex, originalStartTime, changeDescription);

        // Assert — a notification must have been produced
        if (notification is null)
            return false;

        // The notification's RelatedAppointmentId must equal the appointment's Id
        return notification.RelatedAppointmentId == appointment.Id;
    }

    // ---------------------------------------------------------------
    // Property 6: No-user graceful skip
    // ---------------------------------------------------------------

    /// <summary>
    /// Seeds the in-memory database with the given entities but does NOT create
    /// an AppUser. This means the patient's email will not match any user in the
    /// database, simulating the "no user account" scenario.
    /// Returns the saved Appointment with its Id set.
    /// </summary>
    internal static async Task<Appointment> SeedDatabaseWithoutUserAsync(
        ClinicDbContext db, Patient patient, Therapist therapist, Room room, Appointment appointment)
    {
        db.Patients.Add(patient);
        db.Therapists.Add(therapist);
        db.Rooms.Add(room);
        db.Appointments.Add(appointment);

        await db.SaveChangesAsync();
        return appointment;
    }

    /// <summary>
    /// **Property 6: No-user graceful skip**
    ///
    /// For any appointment notification event where the patient's email does not
    /// match any AppUser's UserName, the service produces zero notifications and
    /// does not throw an exception.
    ///
    /// Parameterized across all four event types by generating an event index [0..3].
    ///
    /// **Validates: Requirements 1.5, 2.4, 3.4, 4.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task<bool> NoUserGracefulSkip_ProducesZeroNotificationsAndNoException(
        NonEmptyString patientFirst, NonEmptyString patientLast,
        NonEmptyString therapistFirst, NonEmptyString therapistLast,
        NonEmptyString roomName,
        PositiveInt yearOffset, byte month, byte day, byte hour, byte minute,
        PositiveInt origYearOffset, byte origMonth, byte origDay, byte origHour, byte origMinute,
        NonEmptyString changeDesc,
        byte eventSelector)
    {
        // Arrange
        var pFirst = SanitizeName(patientFirst);
        var pLast = SanitizeName(patientLast);
        var tFirst = SanitizeName(therapistFirst);
        var tLast = SanitizeName(therapistLast);
        var rName = SanitizeName(roomName);
        var changeDescription = SanitizeName(changeDesc);

        // Select one of the four event types
        var eventIndex = eventSelector % 4;

        var startTime = MakeDateTime(yearOffset, month, day, hour, minute);
        var originalStartTime = MakeDateTime(origYearOffset, origMonth, origDay, origHour, origMinute);

        await using var db = CreateInMemoryDb();
        var patient = CreatePatient(pFirst, pLast);
        var therapist = CreateTherapist(tFirst, tLast);
        var room = CreateRoom(rName);
        var appointment = CreateAppointment(patient, therapist, room, startTime);

        // Seed WITHOUT creating an AppUser — patient email won't match any user
        await SeedDatabaseWithoutUserAsync(db, patient, therapist, room, appointment);

        var service = CreateService(db);

        // Act — should not throw
        Exception? caughtException = null;
        try
        {
            await InvokeNotificationMethodAsync(
                service, db, appointment, eventIndex, originalStartTime, changeDescription);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert — no exception was thrown
        if (caughtException is not null)
            return false;

        // Assert — zero notifications were produced
        var notificationCount = await db.Notifications.CountAsync();
        return notificationCount == 0;
    }
}
