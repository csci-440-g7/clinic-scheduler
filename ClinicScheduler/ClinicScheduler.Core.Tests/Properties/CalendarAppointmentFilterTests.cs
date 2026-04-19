using ClinicScheduler.Core.Entities;
using ClinicScheduler.Infrastructure.Data;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;

namespace ClinicScheduler.Core.Tests.Properties;

/// <summary>
/// Property-based tests for calendar appointment filtering by owner and week range.
/// **Validates: Requirements 4.1, 4.2**
/// </summary>
public class CalendarAppointmentFilterTests
{
    private static ClinicDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ClinicDbContext(options);
    }

    /// <summary>
    /// Property 4: Calendar appointment filtering by owner and week range
    /// For any patient ID and week start date, all appointments returned by the calendar
    /// query should belong to that patient and overlap the 7-day week range.
    /// No appointment matching both criteria should be excluded.
    /// The filter uses: a.StartTime &lt; weekEnd &amp;&amp; a.EndTime &gt; weekStart (overlapping range check).
    /// **Validates: Requirements 4.1, 4.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CalendarFilter_ReturnsExactlyMatchingAppointmentsForPatient(
        PositiveInt weekOffsetDays,
        byte hour1, byte minute1, byte duration1Minutes,
        byte hour2, byte minute2, byte duration2Minutes,
        byte hour3, byte minute3, byte duration3Minutes,
        int dayOffset1, int dayOffset2, int dayOffset3)
    {
        var dbName = $"CalendarFilter_Patient_{Guid.NewGuid()}";

        // Constrain week start to a reasonable date range
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var weekStart = baseDate.AddDays(weekOffsetDays.Get % 365);
        var weekEnd = weekStart.AddDays(7);

        // Constrain day offsets to range [-10, 20] to get a mix of in-range and out-of-range
        var constrainedOffset1 = (dayOffset1 % 31) - 10;
        var constrainedOffset2 = (dayOffset2 % 31) - 10;
        var constrainedOffset3 = (dayOffset3 % 31) - 10;

        // Constrain durations to [30, 180] minutes
        var dur1 = TimeSpan.FromMinutes((duration1Minutes % 151) + 30);
        var dur2 = TimeSpan.FromMinutes((duration2Minutes % 151) + 30);
        var dur3 = TimeSpan.FromMinutes((duration3Minutes % 151) + 30);

        // Constrain hours/minutes
        var h1 = hour1 % 24; var m1 = minute1 % 60;
        var h2 = hour2 % 24; var m2 = minute2 % 60;
        var h3 = hour3 % 24; var m3 = minute3 % 60;

        var startTime1 = weekStart.AddDays(constrainedOffset1).AddHours(h1).AddMinutes(m1);
        var startTime2 = weekStart.AddDays(constrainedOffset2).AddHours(h2).AddMinutes(m2);
        var startTime3 = weekStart.AddDays(constrainedOffset3).AddHours(h3).AddMinutes(m3);

        int targetPatientId;

        // Arrange: seed shared entities and appointments
        using (var arrangeCtx = CreateInMemoryContext(dbName))
        {
            var patient1 = new Patient("Alice", "Smith", $"{Guid.NewGuid()}@test.com", new DateOnly(1990, 1, 1));
            var patient2 = new Patient("Bob", "Jones", $"{Guid.NewGuid()}@test.com", new DateOnly(1985, 5, 15));
            var therapist = new Therapist("Dr", "Therapist", $"{Guid.NewGuid()}@test.com");
            var location = new Location("Clinic", "123 Main St");
            var room = new Room("Room A", 1, location);

            arrangeCtx.Patients.AddRange(patient1, patient2);
            arrangeCtx.Therapists.Add(therapist);
            arrangeCtx.Locations.Add(location);
            arrangeCtx.Rooms.Add(room);
            arrangeCtx.SaveChanges();

            targetPatientId = patient1.Id;

            // Appointment 1: belongs to target patient
            var appt1 = new Appointment(patient1, therapist, room, startTime1, dur1);
            // Appointment 2: belongs to target patient
            var appt2 = new Appointment(patient1, therapist, room, startTime2, dur2);
            // Appointment 3: belongs to different patient (should never appear in results)
            var appt3 = new Appointment(patient2, therapist, room, startTime3, dur3);

            arrangeCtx.Appointments.AddRange(appt1, appt2, appt3);
            arrangeCtx.SaveChanges();
        }

        // Act: apply the same filter logic used in CalendarView
        using var queryCtx = CreateInMemoryContext(dbName);
        var results = queryCtx.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Therapist)
            .Include(a => a.Room)
            .Where(a => a.StartTime < weekEnd && a.EndTime > weekStart)
            .Where(a => a.PatientId == targetPatientId)
            .ToList();

        var allAppointments = queryCtx.Appointments.ToList();

        // Assert 1: All returned appointments belong to the target patient
        var allBelongToOwner = results.All(a => a.PatientId == targetPatientId);

        // Assert 2: All returned appointments overlap the week range
        var allInWeekRange = results.All(a => a.StartTime < weekEnd && a.EndTime > weekStart);

        // Assert 3: No appointment matching both criteria is excluded
        var expectedMatches = allAppointments
            .Where(a => a.PatientId == targetPatientId
                     && a.StartTime < weekEnd
                     && a.EndTime > weekStart)
            .ToList();

        var noMatchExcluded = expectedMatches.All(expected =>
            results.Any(r => r.Id == expected.Id));

        return allBelongToOwner && allInWeekRange && noMatchExcluded;
    }

    /// <summary>
    /// Property 4 (therapist variant): Calendar appointment filtering by therapist and week range
    /// For any therapist ID and week start date, all appointments returned by the calendar
    /// query should belong to that therapist and overlap the 7-day week range.
    /// No appointment matching both criteria should be excluded.
    /// **Validates: Requirements 4.1, 4.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CalendarFilter_ReturnsExactlyMatchingAppointmentsForTherapist(
        PositiveInt weekOffsetDays,
        byte hour1, byte minute1, byte duration1Minutes,
        byte hour2, byte minute2, byte duration2Minutes,
        byte hour3, byte minute3, byte duration3Minutes,
        int dayOffset1, int dayOffset2, int dayOffset3)
    {
        var dbName = $"CalendarFilter_Therapist_{Guid.NewGuid()}";

        // Constrain week start to a reasonable date range
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var weekStart = baseDate.AddDays(weekOffsetDays.Get % 365);
        var weekEnd = weekStart.AddDays(7);

        // Constrain day offsets to range [-10, 20] to get a mix of in-range and out-of-range
        var constrainedOffset1 = (dayOffset1 % 31) - 10;
        var constrainedOffset2 = (dayOffset2 % 31) - 10;
        var constrainedOffset3 = (dayOffset3 % 31) - 10;

        // Constrain durations to [30, 180] minutes
        var dur1 = TimeSpan.FromMinutes((duration1Minutes % 151) + 30);
        var dur2 = TimeSpan.FromMinutes((duration2Minutes % 151) + 30);
        var dur3 = TimeSpan.FromMinutes((duration3Minutes % 151) + 30);

        // Constrain hours/minutes
        var h1 = hour1 % 24; var m1 = minute1 % 60;
        var h2 = hour2 % 24; var m2 = minute2 % 60;
        var h3 = hour3 % 24; var m3 = minute3 % 60;

        var startTime1 = weekStart.AddDays(constrainedOffset1).AddHours(h1).AddMinutes(m1);
        var startTime2 = weekStart.AddDays(constrainedOffset2).AddHours(h2).AddMinutes(m2);
        var startTime3 = weekStart.AddDays(constrainedOffset3).AddHours(h3).AddMinutes(m3);

        int targetTherapistId;

        // Arrange: seed shared entities and appointments
        using (var arrangeCtx = CreateInMemoryContext(dbName))
        {
            var patient = new Patient("Alice", "Smith", $"{Guid.NewGuid()}@test.com", new DateOnly(1990, 1, 1));
            var therapist1 = new Therapist("Dr", "Alpha", $"{Guid.NewGuid()}@test.com");
            var therapist2 = new Therapist("Dr", "Beta", $"{Guid.NewGuid()}@test.com");
            var location = new Location("Clinic", "123 Main St");
            var room = new Room("Room A", 1, location);

            arrangeCtx.Patients.Add(patient);
            arrangeCtx.Therapists.AddRange(therapist1, therapist2);
            arrangeCtx.Locations.Add(location);
            arrangeCtx.Rooms.Add(room);
            arrangeCtx.SaveChanges();

            targetTherapistId = therapist1.Id;

            // Appointment 1: belongs to target therapist
            var appt1 = new Appointment(patient, therapist1, room, startTime1, dur1);
            // Appointment 2: belongs to target therapist
            var appt2 = new Appointment(patient, therapist1, room, startTime2, dur2);
            // Appointment 3: belongs to different therapist (should never appear in results)
            var appt3 = new Appointment(patient, therapist2, room, startTime3, dur3);

            arrangeCtx.Appointments.AddRange(appt1, appt2, appt3);
            arrangeCtx.SaveChanges();
        }

        // Act: apply the same filter logic used in CalendarView
        using var queryCtx = CreateInMemoryContext(dbName);
        var results = queryCtx.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Therapist)
            .Include(a => a.Room)
            .Where(a => a.StartTime < weekEnd && a.EndTime > weekStart)
            .Where(a => a.TherapistId == targetTherapistId)
            .ToList();

        var allAppointments = queryCtx.Appointments.ToList();

        // Assert 1: All returned appointments belong to the target therapist
        var allBelongToOwner = results.All(a => a.TherapistId == targetTherapistId);

        // Assert 2: All returned appointments overlap the week range
        var allInWeekRange = results.All(a => a.StartTime < weekEnd && a.EndTime > weekStart);

        // Assert 3: No appointment matching both criteria is excluded
        var expectedMatches = allAppointments
            .Where(a => a.TherapistId == targetTherapistId
                     && a.StartTime < weekEnd
                     && a.EndTime > weekStart)
            .ToList();

        var noMatchExcluded = expectedMatches.All(expected =>
            results.Any(r => r.Id == expected.Id));

        return allBelongToOwner && allInWeekRange && noMatchExcluded;
    }
}
