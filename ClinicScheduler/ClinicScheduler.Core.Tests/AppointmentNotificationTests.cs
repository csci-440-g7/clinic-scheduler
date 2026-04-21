using ClinicScheduler.Core.Entities;
using ClinicScheduler.Infrastructure.Data;
using ClinicScheduler.Web.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClinicScheduler.Core.Tests;

/// <summary>
/// Example-based unit tests for the appointment notification feature.
/// Validates: Requirements 5.1, 5.2, 5.3, 8.1, 8.2, 8.3, 8.4
/// </summary>
public class AppointmentNotificationTests
{
    // ---------------------------------------------------------------
    // Enum backward compatibility tests (Requirements 5.1, 5.2, 5.3)
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(NotificationType.MissedAppointment, 0)]
    [InlineData(NotificationType.UpcomingAppointment, 1)]
    [InlineData(NotificationType.RequestApproved, 2)]
    [InlineData(NotificationType.RequestDenied, 3)]
    [InlineData(NotificationType.SchedulingConflict, 4)]
    [InlineData(NotificationType.AppointmentRescheduled, 5)]
    [InlineData(NotificationType.CancellationRequested, 6)]
    [InlineData(NotificationType.CancellationApproved, 7)]
    [InlineData(NotificationType.CancellationDenied, 8)]
    public void NotificationType_ExistingValues_HaveUnchangedOrdinals(NotificationType type, int expectedOrdinal)
    {
        ((int)type).Should().Be(expectedOrdinal);
    }

    [Fact]
    public void NotificationType_AppointmentCreated_HasOrdinal9()
    {
        ((int)NotificationType.AppointmentCreated).Should().Be(9);
    }

    [Fact]
    public void NotificationType_AppointmentUpdated_HasOrdinal10()
    {
        ((int)NotificationType.AppointmentUpdated).Should().Be(10);
    }

    [Fact]
    public void NotificationType_HasExactly11Values()
    {
        Enum.GetValues<NotificationType>().Should().HaveCount(11);
    }

    // ---------------------------------------------------------------
    // Notification defaults tests (Requirements 8.2, 8.3)
    // ---------------------------------------------------------------

    [Fact]
    public void Notification_WhenCreated_IsReadDefaultsToFalse()
    {
        var notification = new Notification(
            "user-1",
            NotificationType.AppointmentCreated,
            "Test Title",
            "Test Message",
            42);

        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public void Notification_WhenCreated_CreatedAtIsApproximatelyUtcNow()
    {
        var before = DateTime.UtcNow;

        var notification = new Notification(
            "user-1",
            NotificationType.AppointmentCreated,
            "Test Title",
            "Test Message",
            42);

        var after = DateTime.UtcNow;

        notification.CreatedAt.Should().BeOnOrAfter(before);
        notification.CreatedAt.Should().BeOnOrBefore(after);
    }

    // ---------------------------------------------------------------
    // Database error resilience test (Requirement 8.4)
    // ---------------------------------------------------------------

    [Fact]
    public async Task NotifyAppointmentCreatedAsync_WhenSaveChangesFails_LogsErrorWithoutThrowing()
    {
        // Arrange — use a real in-memory db to seed data, then create a
        // second context backed by a mock that throws on SaveChangesAsync.
        await using var seedDb = AppointmentNotificationPropertyTests.CreateInMemoryDb();
        var patient = AppointmentNotificationPropertyTests.CreatePatient("Jane", "Doe");
        var therapist = AppointmentNotificationPropertyTests.CreateTherapist("Dr", "Smith");
        var room = AppointmentNotificationPropertyTests.CreateRoom("Room A");
        var startTime = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var appointment = AppointmentNotificationPropertyTests.CreateAppointment(patient, therapist, room, startTime);
        await AppointmentNotificationPropertyTests.SeedDatabaseAsync(seedDb, patient, therapist, room, appointment);

        // Build a mock DbContext that delegates reads to the real in-memory db
        // but throws on SaveChangesAsync
        var mockLogger = new Mock<ILogger<AppointmentNotificationService>>();

        // We'll use a wrapper approach: create a new in-memory db, seed it,
        // then use Moq to create a partial mock of ClinicDbContext that throws on SaveChangesAsync
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        // Seed the throwing db with the same data
        await using (var setupDb = new ClinicDbContext(options))
        {
            var p = AppointmentNotificationPropertyTests.CreatePatient("Jane", "Doe");
            var t = AppointmentNotificationPropertyTests.CreateTherapist("Dr", "Smith");
            var r = AppointmentNotificationPropertyTests.CreateRoom("Room A");
            var a = AppointmentNotificationPropertyTests.CreateAppointment(p, t, r, startTime);

            setupDb.Patients.Add(p);
            setupDb.Therapists.Add(t);
            setupDb.Rooms.Add(r);
            setupDb.Appointments.Add(a);
            setupDb.Users.Add(new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = p.Email,
                NormalizedUserName = p.Email.ToUpperInvariant(),
                Email = p.Email,
                NormalizedEmail = p.Email.ToUpperInvariant(),
                DisplayName = p.FullName,
                SecurityStamp = Guid.NewGuid().ToString()
            });
            await setupDb.SaveChangesAsync();

            // Now create a mock context that uses the same in-memory store but throws on SaveChangesAsync
            var mockOptions = new DbContextOptionsBuilder<ClinicDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            var throwingDb = new ThrowingClinicDbContext(mockOptions);

            var service = new AppointmentNotificationService(throwingDb, mockLogger.Object);

            // Re-load the appointment with navigation properties from the throwing context
            var loadedAppointment = await throwingDb.Appointments
                .Include(x => x.Patient)
                .Include(x => x.Therapist)
                .FirstAsync();

            // Act — should NOT throw
            var act = async () => await service.NotifyAppointmentCreatedAsync(loadedAppointment);

            // Assert
            await act.Should().NotThrowAsync();

            // Verify that an error was logged
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }

    // ---------------------------------------------------------------
    // Null/empty change description fallback tests (Requirement 8.4)
    // ---------------------------------------------------------------

    [Fact]
    public async Task NotifyAppointmentUpdatedAsync_WhenChangeDescriptionIsNull_UsesFallbackMessage()
    {
        // Arrange
        await using var db = AppointmentNotificationPropertyTests.CreateInMemoryDb();
        var patient = AppointmentNotificationPropertyTests.CreatePatient("Alice", "Wonder");
        var therapist = AppointmentNotificationPropertyTests.CreateTherapist("Dr", "Jones");
        var room = AppointmentNotificationPropertyTests.CreateRoom("Room B");
        var startTime = new DateTime(2025, 7, 1, 14, 30, 0, DateTimeKind.Utc);
        var appointment = AppointmentNotificationPropertyTests.CreateAppointment(patient, therapist, room, startTime);
        await AppointmentNotificationPropertyTests.SeedDatabaseAsync(db, patient, therapist, room, appointment);

        var service = AppointmentNotificationPropertyTests.CreateService(db);

        // Act
        await service.NotifyAppointmentUpdatedAsync(appointment, null!);

        // Assert
        var notification = await db.Notifications.SingleAsync();
        notification.Message.Should().Be("Your appointment details have been updated.");
    }

    [Fact]
    public async Task NotifyAppointmentUpdatedAsync_WhenChangeDescriptionIsEmpty_UsesFallbackMessage()
    {
        // Arrange
        await using var db = AppointmentNotificationPropertyTests.CreateInMemoryDb();
        var patient = AppointmentNotificationPropertyTests.CreatePatient("Bob", "Builder");
        var therapist = AppointmentNotificationPropertyTests.CreateTherapist("Dr", "Lee");
        var room = AppointmentNotificationPropertyTests.CreateRoom("Room C");
        var startTime = new DateTime(2025, 8, 10, 9, 0, 0, DateTimeKind.Utc);
        var appointment = AppointmentNotificationPropertyTests.CreateAppointment(patient, therapist, room, startTime);
        await AppointmentNotificationPropertyTests.SeedDatabaseAsync(db, patient, therapist, room, appointment);

        var service = AppointmentNotificationPropertyTests.CreateService(db);

        // Act
        await service.NotifyAppointmentUpdatedAsync(appointment, "");

        // Assert
        var notification = await db.Notifications.SingleAsync();
        notification.Message.Should().Be("Your appointment details have been updated.");
    }

    [Fact]
    public async Task NotifyAppointmentUpdatedAsync_WhenChangeDescriptionIsWhitespace_UsesFallbackMessage()
    {
        // Arrange
        await using var db = AppointmentNotificationPropertyTests.CreateInMemoryDb();
        var patient = AppointmentNotificationPropertyTests.CreatePatient("Carol", "Danvers");
        var therapist = AppointmentNotificationPropertyTests.CreateTherapist("Dr", "Strange");
        var room = AppointmentNotificationPropertyTests.CreateRoom("Room D");
        var startTime = new DateTime(2025, 9, 5, 11, 0, 0, DateTimeKind.Utc);
        var appointment = AppointmentNotificationPropertyTests.CreateAppointment(patient, therapist, room, startTime);
        await AppointmentNotificationPropertyTests.SeedDatabaseAsync(db, patient, therapist, room, appointment);

        var service = AppointmentNotificationPropertyTests.CreateService(db);

        // Act
        await service.NotifyAppointmentUpdatedAsync(appointment, "   ");

        // Assert
        var notification = await db.Notifications.SingleAsync();
        notification.Message.Should().Be("Your appointment details have been updated.");
    }

    // ---------------------------------------------------------------
    // Helper: A ClinicDbContext subclass that throws on SaveChangesAsync
    // ---------------------------------------------------------------

    private class ThrowingClinicDbContext : ClinicDbContext
    {
        public ThrowingClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options) { }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new DbUpdateException("Simulated database failure");
        }
    }
}
