using System.Linq.Expressions;
using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using ClinicScheduler.Core.Services;
using FluentAssertions;
using Moq;

namespace ClinicScheduler.Core.Tests.Services;

public class AppointmentSchedulingServiceTests
{
    private readonly Mock<IRepository<Appointment>> _appointmentRepo = new();
    private readonly Mock<IRepository<Patient>> _patientRepo = new();
    private readonly Mock<IRepository<Therapist>> _therapistRepo = new();
    private readonly Mock<IRepository<Room>> _roomRepo = new();
    private readonly Mock<IRepository<TimeSlot>> _timeSlotRepo = new();
    private readonly Mock<IRepository<Location>> _locationRepo = new();
    private readonly Mock<IRepository<ScheduleConflict>> _scheduleConflictRepo = new();

    private AppointmentSchedulingService CreateService() =>
        new(_appointmentRepo.Object, _patientRepo.Object, _therapistRepo.Object, _roomRepo.Object,
            _timeSlotRepo.Object, _locationRepo.Object, _scheduleConflictRepo.Object);

    private static Patient MakePatient() =>
        new("John", "Doe", "john@example.com", new DateOnly(1985, 1, 1));

    private static Therapist MakeTherapist() =>
        new("Jane", "Smith", "jane@clinic.com");

    private static Room MakeRoom() =>
        new("Room A", 1, new Location("Main", "123 Main St"));

    private void SetupEntities(Patient patient, Therapist therapist, Room room)
    {
        _patientRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _therapistRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(therapist);
        _roomRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);
        _appointmentRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Appointment>());
        _appointmentRepo
            .Setup(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment a, CancellationToken _) => a);

        // Setup location-aware dependencies
        _timeSlotRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<TimeSlot, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TimeSlot>());

        var location = new Location("Main", "123 Main St") { Id = 1 };
        location.SetDailyCapacity(12);
        _locationRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        _roomRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Room> { room });

        _scheduleConflictRepo
            .Setup(r => r.AddAsync(It.IsAny<ScheduleConflict>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduleConflict sc, CancellationToken _) => sc);
    }

    // Next Monday at 9:00 AM — a guaranteed valid weekday business-hours slot
    private static DateTime NextMonday9am()
    {
        var today = DateTime.UtcNow.Date;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7;
        return today.AddDays(daysUntilMonday).AddHours(9);
    }

    [Fact]
    public async Task CreateAppointment_With60MinDuration_ShouldThrow()
    {
        // Arrange
        SetupEntities(MakePatient(), MakeTherapist(), MakeRoom());
        var service = CreateService();

        // Act
        var act = async () => await service.CreateAppointmentAsync(1, 1, 1, NextMonday9am(), TimeSpan.FromMinutes(60));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*30 minutes*");
    }

    [Fact]
    public async Task CreateAppointment_OnSaturday_ShouldThrow()
    {
        // Arrange
        SetupEntities(MakePatient(), MakeTherapist(), MakeRoom());
        var service = CreateService();

        // Find the next Saturday
        var today = DateTime.UtcNow.Date;
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilSaturday == 0) daysUntilSaturday = 7;
        var saturday9am = today.AddDays(daysUntilSaturday).AddHours(9);

        // Act
        var act = async () => await service.CreateAppointmentAsync(1, 1, 1, saturday9am, TimeSpan.FromMinutes(30));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*outside the configured schedule*");
    }

    [Fact]
    public async Task CreateAppointment_OnSunday_ShouldThrow()
    {
        // Arrange
        SetupEntities(MakePatient(), MakeTherapist(), MakeRoom());
        var service = CreateService();

        var today = DateTime.UtcNow.Date;
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilSunday == 0) daysUntilSunday = 7;
        var sunday9am = today.AddDays(daysUntilSunday).AddHours(9);

        // Act
        var act = async () => await service.CreateAppointmentAsync(1, 1, 1, sunday9am, TimeSpan.FromMinutes(30));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*outside the configured schedule*");
    }

    [Fact]
    public async Task CreateAppointment_Before8am_ShouldThrow()
    {
        // Arrange
        SetupEntities(MakePatient(), MakeTherapist(), MakeRoom());
        var service = CreateService();
        var monday7am = NextMonday9am().AddHours(-2); // 7:00 AM

        // Act
        var act = async () => await service.CreateAppointmentAsync(1, 1, 1, monday7am, TimeSpan.FromMinutes(30));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*outside the configured schedule*");
    }

    [Fact]
    public async Task CreateAppointment_EndingAfter5pm_ShouldThrow()
    {
        // Arrange
        SetupEntities(MakePatient(), MakeTherapist(), MakeRoom());
        var service = CreateService();
        // Slot starting at 17:00 ends at 17:30 — past 5 PM close
        var monday5pm = NextMonday9am().Date.AddHours(17);

        // Act
        var act = async () => await service.CreateAppointmentAsync(1, 1, 1, monday5pm, TimeSpan.FromMinutes(30));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*outside the configured schedule*");
    }

    [Fact]
    public async Task CreateAppointment_When12PatientsAlreadyScheduled_ShouldThrow()
    {
        // Arrange
        SetupEntities(MakePatient(), MakeTherapist(), MakeRoom());
        var service = CreateService();

        // The overlapping check (time-based) returns no conflicts for therapist/room/patient
        // but the daily capacity check finds 12 distinct patients at the location
        var location = new Location("Main", "123 Main St") { Id = 1 };
        location.SetDailyCapacity(12);
        _locationRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        // Create 12 existing appointments at the same location on the same day
        var existingAppointments = Enumerable.Range(100, 12)
            .Select(i =>
            {
                var p = MakePatient();
                p.Id = i;
                var t = MakeTherapist();
                t.Id = i + 200;
                var r = MakeRoom();
                r.Id = i + 300;
                return new Appointment(p, t, r, NextMonday9am(), TimeSpan.FromMinutes(30));
            })
            .ToList();

        // First FindAsync call (overlapping time check) returns empty — no time conflicts
        _appointmentRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Appointment, bool>> predicate, CancellationToken _) =>
            {
                // Return the existing appointments for the daily capacity check
                return existingAppointments;
            });

        // Room.FindAsync for location rooms — all rooms belong to the same location
        var locationRooms = existingAppointments.Select(a =>
        {
            var rm = MakeRoom();
            rm.Id = a.RoomId;
            return rm;
        }).Append(MakeRoom()).ToList();
        _roomRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(locationRooms);

        // Act
        var act = async () => await service.CreateAppointmentAsync(1, 1, 1, NextMonday9am(), TimeSpan.FromMinutes(30));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Location daily capacity reached*12*");
    }

    [Fact]
    public async Task CreateAppointment_WithValidSlot_ShouldSucceed()
    {
        // Arrange
        var patient = MakePatient();
        var therapist = MakeTherapist();
        var room = MakeRoom();
        SetupEntities(patient, therapist, room);
        var service = CreateService();

        // Act
        var result = await service.CreateAppointmentAsync(1, 1, 1, NextMonday9am(), TimeSpan.FromMinutes(30));

        // Assert
        result.Should().NotBeNull();
        result.StartTime.Should().Be(NextMonday9am());
        result.EndTime.Should().Be(NextMonday9am().AddMinutes(30));
        result.Status.Should().Be(AppointmentStatus.Scheduled);
    }
}
