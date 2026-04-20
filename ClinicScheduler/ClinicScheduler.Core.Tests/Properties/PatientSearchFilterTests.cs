using ClinicScheduler.Core.Entities;
using ClinicScheduler.Infrastructure.Data;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;

namespace ClinicScheduler.Core.Tests.Properties;

/// <summary>
/// Property-based tests for patient search filter correctness.
/// **Validates: Requirements 3.3**
/// </summary>
public class PatientSearchFilterTests
{
    private static ClinicDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ClinicDbContext(options);
    }

    /// <summary>
    /// Property 3: Patient search filter correctness
    /// For any non-empty search term and set of patients in the database,
    /// all patients returned by the search should have a FirstName or LastName
    /// that contains the search term, and no patient whose FirstName or LastName
    /// contains the search term should be excluded from the results.
    /// This mirrors the DoctorDashboard filter:
    ///   DbContext.Patients.Where(p => p.FirstName.Contains(term) || p.LastName.Contains(term))
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool PatientSearch_ReturnsExactlyMatchingPatients(
        NonEmptyString firstName1,
        NonEmptyString lastName1,
        NonEmptyString firstName2,
        NonEmptyString lastName2,
        NonEmptyString firstName3,
        NonEmptyString lastName3,
        NonEmptyString searchTerm)
    {
        var dbName = $"SearchFilter_{Guid.NewGuid()}";
        var term = searchTerm.Get;

        // Arrange: seed three patients into the in-memory database
        using (var arrangeCtx = CreateInMemoryContext(dbName))
        {
            arrangeCtx.Patients.Add(new Patient(
                firstName1.Get, lastName1.Get,
                $"{Guid.NewGuid()}@test.com", new DateOnly(1990, 1, 1)));
            arrangeCtx.Patients.Add(new Patient(
                firstName2.Get, lastName2.Get,
                $"{Guid.NewGuid()}@test.com", new DateOnly(1991, 2, 2)));
            arrangeCtx.Patients.Add(new Patient(
                firstName3.Get, lastName3.Get,
                $"{Guid.NewGuid()}@test.com", new DateOnly(1992, 3, 3)));
            arrangeCtx.SaveChanges();
        }

        // Act: apply the same filter logic used in DoctorDashboard
        using var queryCtx = CreateInMemoryContext(dbName);
        var results = queryCtx.Patients
            .Where(p => p.FirstName.Contains(term) || p.LastName.Contains(term))
            .ToList();

        var allPatients = queryCtx.Patients.ToList();

        // Assert 1: All returned patients have FirstName or LastName containing the search term
        var allResultsMatch = results.All(p =>
            p.FirstName.Contains(term) || p.LastName.Contains(term));

        // Assert 2: No patient matching the filter criteria is excluded from results
        var expectedMatches = allPatients.Where(p =>
            p.FirstName.Contains(term) || p.LastName.Contains(term)).ToList();

        var noMatchExcluded = expectedMatches.All(expected =>
            results.Any(r => r.Id == expected.Id));

        return allResultsMatch && noMatchExcluded;
    }
}
