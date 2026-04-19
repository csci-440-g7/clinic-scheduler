using ClinicScheduler.Core.Entities;
using ClinicScheduler.Infrastructure.Data;
using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;

namespace ClinicScheduler.Core.Tests.Properties;

/// <summary>
/// Property-based tests for AppointmentRequest creation round-trip.
/// **Validates: Requirements 1.2, 1.3, 1.5, 1.6**
/// </summary>
public class AppointmentRequestRoundTripTests
{
    private static ClinicDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ClinicDbContext(options);
    }

    /// <summary>
    /// Property 1: Appointment request creation round-trip
    /// For any valid combination of patient, optional preferred therapist, preferred date/time,
    /// and notes text, creating an AppointmentRequest and then reading it back from the database
    /// should yield an entity where PatientId matches the authenticated patient,
    /// PreferredTherapistId matches the selected therapist (or null), PreferredDateTime matches
    /// the selected date/time, and Notes matches the submitted text.
    /// **Validates: Requirements 1.2, 1.3, 1.5, 1.6**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool AppointmentRequest_RoundTrip_PreservesAllFields(
        NonEmptyString patientFirstName,
        NonEmptyString patientLastName,
        NonEmptyString patientEmail,
        bool hasTherapist,
        NonEmptyString therapistFirstName,
        NonEmptyString therapistLastName,
        NonEmptyString therapistEmail,
        NonNull<string> notes,
        PositiveInt yearOffset,
        byte month,
        byte day,
        byte hour,
        byte minute)
    {
        // Constrain generated values to valid date/time ranges
        var year = 2000 + (yearOffset.Get % 100);
        var validMonth = (month % 12) + 1;
        var maxDay = DateTime.DaysInMonth(year, validMonth);
        var validDay = (day % maxDay) + 1;
        var validHour = hour % 24;
        var validMinute = minute % 60;
        var preferredDateTime = new DateTime(year, validMonth, validDay, validHour, validMinute, 0, DateTimeKind.Utc);

        var dbName = $"RoundTrip_{Guid.NewGuid()}";

        // Arrange: create and persist prerequisite entities
        using (var arrangeCtx = CreateInMemoryContext(dbName))
        {
            var patient = new Patient(
                patientFirstName.Get,
                patientLastName.Get,
                patientEmail.Get,
                new DateOnly(1990, 1, 1));
            arrangeCtx.Patients.Add(patient);

            Therapist? therapist = null;
            if (hasTherapist)
            {
                therapist = new Therapist(
                    therapistFirstName.Get,
                    therapistLastName.Get,
                    therapistEmail.Get);
                arrangeCtx.Therapists.Add(therapist);
            }

            arrangeCtx.SaveChanges();

            // Act: create AppointmentRequest and persist
            var request = new AppointmentRequest(patient, notes.Get, therapist);
            request.SetPreferredDateTime(preferredDateTime);
            arrangeCtx.AppointmentRequests.Add(request);
            arrangeCtx.SaveChanges();
        }

        // Assert: read back from a fresh context and verify all fields
        using (var assertCtx = CreateInMemoryContext(dbName))
        {
            var savedRequest = assertCtx.AppointmentRequests
                .Include(r => r.Patient)
                .Include(r => r.PreferredTherapist)
                .Single();

            var expectedPatient = assertCtx.Patients.Single();
            Therapist? expectedTherapist = hasTherapist
                ? assertCtx.Therapists.Single()
                : null;

            return savedRequest.PatientId == expectedPatient.Id
                && savedRequest.PreferredTherapistId == expectedTherapist?.Id
                && savedRequest.PreferredDateTime == preferredDateTime
                && savedRequest.Notes == notes.Get
                && savedRequest.Status == AppointmentRequestStatus.Pending;
        }
    }
}
