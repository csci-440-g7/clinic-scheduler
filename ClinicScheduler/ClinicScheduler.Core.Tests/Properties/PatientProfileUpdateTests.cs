using ClinicScheduler.Core.Entities;
using ClinicScheduler.Infrastructure.Data;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;

namespace ClinicScheduler.Core.Tests.Properties;

/// <summary>
/// Property-based tests for patient profile update persistence.
/// **Validates: Requirements 2.4**
/// </summary>
public class PatientProfileUpdateTests
{
    private static ClinicDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ClinicDbContext(options);
    }

    /// <summary>
    /// Property 2: Patient profile update persistence
    /// For any valid phone number and email string, updating a patient's contact information
    /// via UpdateContactInfo() and then reading the patient back should yield the same phone
    /// and email values that were submitted.
    /// **Validates: Requirements 2.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool PatientProfileUpdate_PersistsContactInfo(
        NonEmptyString initialFirstName,
        NonEmptyString initialLastName,
        NonEmptyString initialEmail,
        NonEmptyString updatedEmail,
        NonNull<string> updatedPhone)
    {
        var dbName = $"ProfileUpdate_{Guid.NewGuid()}";

        int patientId;

        // Arrange: create and persist a patient
        using (var arrangeCtx = CreateInMemoryContext(dbName))
        {
            var patient = new Patient(
                initialFirstName.Get,
                initialLastName.Get,
                initialEmail.Get,
                new DateOnly(1990, 1, 1));
            arrangeCtx.Patients.Add(patient);
            arrangeCtx.SaveChanges();
            patientId = patient.Id;
        }

        // Act: update contact info and persist
        using (var actCtx = CreateInMemoryContext(dbName))
        {
            var patient = actCtx.Patients.Single(p => p.Id == patientId);
            patient.UpdateContactInfo(updatedEmail.Get, updatedPhone.Get);
            actCtx.SaveChanges();
        }

        // Assert: read back from a fresh context and verify fields match
        using (var assertCtx = CreateInMemoryContext(dbName))
        {
            var savedPatient = assertCtx.Patients.Single(p => p.Id == patientId);

            return savedPatient.Email == updatedEmail.Get
                && savedPatient.Phone == updatedPhone.Get;
        }
    }
}
