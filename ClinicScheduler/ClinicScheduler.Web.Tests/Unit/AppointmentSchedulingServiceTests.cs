using System.Linq.Expressions;
using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using ClinicScheduler.Core.Services;
using FluentAssertions;
using Moq;

namespace ClinicScheduler.Web.Tests.Unit;

public class AppointmentSchedulingServiceTests
{
    // ── Slot validation (location-aware, default fallback) ─────────────────

    [Theory]
    [InlineData(DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Sunday)]
    public async Task ValidateSlotForLocation_OnWeekend_ThrowsArgumentException(DayOfWeek day)
    {
        var (_, _, _, _, timeSlotRepo, locationRepo, _) = BuildMocks();
        timeSlotRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<TimeSlot, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TimeSlot>());
        var sut = BuildSut(new(), new(), new(), new(), timeSlotRepo, locationRepo, new());

        var date = NextOccurrenceOf(day, hour: 9);
        var act = async () => await sut.ValidateSlotForLocation(date, 1);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*outside the configured schedule*");
    }

    [Theory]
    [InlineData(9, 15)]
    [InlineData(10, 45)]
    [InlineData(14, 1)]
    public async Task ValidateSlotForLocation_NonHalfHourBoundary_ThrowsArgumentException(int hour, int minute)
    {
        var (_, _, _, _, timeSlotRepo, locationRepo, _) = BuildMocks();
        timeSlotRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<TimeSlot, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TimeSlot>());
        var sut = BuildSut(new(), new(), new(), new(), timeSlotRepo, locationRepo, new());

        var slot = new DateTime(2030, 6, 3, hour, minute, 0, DateTimeKind.Utc); // Monday
        var act = async () => await sut.ValidateSlotForLocation(slot, 1);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*30-minute*");
    }

    [Fact]
    public async Task ValidateSlotForLocation_Before8am_ThrowsArgumentException()
    {
        var (_, _, _, _, timeSlotRepo, locationRepo, _) = BuildMocks();
        timeSlotRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<TimeSlot, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TimeSlot>());
        var sut = BuildSut(new(), new(), new(), new(), timeSlotRepo, locationRepo, new());

        var slot = new DateTime(2030, 6, 3, 7, 30, 0, DateTimeKind.Utc);
        var act = async () => await sut.ValidateSlotForLocation(slot, 1);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*outside the configured schedule*");
    }

    [Fact]
    public async Task ValidateSlotForLocation_At5pm_ThrowsArgumentException()
    {
        var (_, _, _, _, timeSlotRepo, locationRepo, _) = BuildMocks();
        timeSlotRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<TimeSlot, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TimeSlot>());
        var sut = BuildSut(new(), new(), new(), new(), timeSlotRepo, locationRepo, new());

        var slot = new DateTime(2030, 6, 3, 17, 0, 0, DateTimeKind.Utc);
        var act = async () => await sut.ValidateSlotForLocation(slot, 1);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*outside the configured schedule*");
    }

    [Theory]
    [InlineData(8, 0)]
    [InlineData(8, 30)]
    [InlineData(12, 0)]
    [InlineData(16, 0)]
    [InlineData(16, 30)]
    public async Task ValidateSlotForLocation_ValidWeekdaySlot_DoesNotThrow(int hour, int minute)
    {
        var (_, _, _, _, timeSlotRepo, locationRepo, _) = BuildMocks();
        timeSlotRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<TimeSlot, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TimeSlot>());
        var sut = BuildSut(new(), new(), new(), new(), timeSlotRepo, locationRepo, new());

        var slot = new DateTime(2030, 6, 3, hour, minute, 0, DateTimeKind.Utc); // Monday
        var act = async () => await sut.ValidateSlotForLocation(slot, 1);
        await act.Should().NotThrowAsync();
    }

    // ── CreateAppointmentAsync — entity-not-found guards ────────────────────

    [Fact]
    public async Task CreateAppointmentAsync_PatientNotFound_ThrowsArgumentException()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo, _, _, _) = BuildMocks();
        patientRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo);

        var act = async () => await sut.CreateAppointmentAsync(99, 1, 1, ValidSlot, Thirty);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Patient*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_TherapistNotFound_ThrowsArgumentException()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo, _, _, _) = BuildMocks();
        patientRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Patient1);
        therapistRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Therapist?)null);
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo);

        var act = async () => await sut.CreateAppointmentAsync(1, 99, 1, ValidSlot, Thirty);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Therapist*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_RoomNotFound_ThrowsArgumentException()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo, _, _, _) = BuildMocks();
        patientRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Patient1);
        therapistRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Therapist1);
        roomRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo);

        var act = async () => await sut.CreateAppointmentAsync(1, 1, 99, ValidSlot, Thirty);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Room*");
    }

    // ── CreateAppointmentAsync — conflict detection ──────────────────────────

    [Fact]
    public async Task CreateAppointmentAsync_TherapistConflict_ThrowsInvalidOperationException()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo) = BuildMocks();
        SetupCoreEntities(patientRepo, therapistRepo, roomRepo);
        SetupLocationDeps(timeSlotRepo, locationRepo, roomRepo, scheduleConflictRepo);

        // Same therapist, different patient/room
        var blocking = new Appointment(MakePatient(2), Therapist1, MakeRoom(2), ValidSlot, Thirty);
        apptRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { blocking });
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo);

        var act = async () => await sut.CreateAppointmentAsync(1, 1, 1, ValidSlot, Thirty);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*therapist*unavailable*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_RoomConflict_ThrowsInvalidOperationException()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo) = BuildMocks();
        SetupCoreEntities(patientRepo, therapistRepo, roomRepo);
        SetupLocationDeps(timeSlotRepo, locationRepo, roomRepo, scheduleConflictRepo);

        // Same room, different therapist/patient
        var blocking = new Appointment(MakePatient(2), MakeTherapist(2), Room1, ValidSlot, Thirty);
        apptRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { blocking });
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo);

        var act = async () => await sut.CreateAppointmentAsync(1, 1, 1, ValidSlot, Thirty);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*room*unavailable*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_PatientDoubleBooked_ThrowsInvalidOperationException()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo) = BuildMocks();
        SetupCoreEntities(patientRepo, therapistRepo, roomRepo);
        SetupLocationDeps(timeSlotRepo, locationRepo, roomRepo, scheduleConflictRepo);

        // Same patient, different therapist/room
        var blocking = new Appointment(Patient1, MakeTherapist(2), MakeRoom(2), ValidSlot, Thirty);
        apptRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { blocking });
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo);

        var act = async () => await sut.CreateAppointmentAsync(1, 1, 1, ValidSlot, Thirty);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*patient*already scheduled*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_CapacityExceeded_ThrowsInvalidOperationException()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo) = BuildMocks();
        SetupCoreEntities(patientRepo, therapistRepo, roomRepo);
        SetupLocationDeps(timeSlotRepo, locationRepo, roomRepo, scheduleConflictRepo);

        // 12 distinct patients already booked at the location on the same day
        var existing = Enumerable.Range(2, 12)
            .Select(i => new Appointment(MakePatient(i), MakeTherapist(i), MakeRoom(i), ValidSlot, Thirty))
            .ToList();
        apptRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        apptRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment a, CancellationToken _) => a);
        apptRepo.Setup(r => r.UpdateAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // All rooms belong to the same location
        var allRooms = existing.Select(a => MakeRoom(a.RoomId)).Append(Room1).ToList();
        roomRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allRooms);

        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo);

        var act = async () => await sut.CreateAppointmentAsync(1, 1, 1, ValidSlot, Thirty);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Location daily capacity reached*12*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_ElevenConcurrentPatients_Succeeds()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo) = BuildMocks();
        SetupCoreEntities(patientRepo, therapistRepo, roomRepo);
        SetupLocationDeps(timeSlotRepo, locationRepo, roomRepo, scheduleConflictRepo);

        // 11 distinct patients — one slot remaining
        var existing = Enumerable.Range(2, 11)
            .Select(i => new Appointment(MakePatient(i), MakeTherapist(i), MakeRoom(i), ValidSlot, Thirty))
            .ToList();
        apptRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        apptRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment a, CancellationToken _) => a);

        // All rooms belong to the same location
        var allRooms = existing.Select(a => MakeRoom(a.RoomId)).Append(Room1).ToList();
        roomRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allRooms);

        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo);

        var result = await sut.CreateAppointmentAsync(1, 1, 1, ValidSlot, Thirty);
        result.Should().NotBeNull();
        result.Status.Should().Be(AppointmentStatus.Scheduled);
    }

    [Fact]
    public async Task CreateAppointmentAsync_NoConflicts_ReturnsScheduledAppointment()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo) = BuildMocks();
        SetupCoreEntities(patientRepo, therapistRepo, roomRepo);
        SetupLocationDeps(timeSlotRepo, locationRepo, roomRepo, scheduleConflictRepo);
        apptRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Appointment>());
        apptRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment a, CancellationToken _) => a);
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo);

        var result = await sut.CreateAppointmentAsync(1, 1, 1, ValidSlot, Thirty);

        result.Should().NotBeNull();
        result.Status.Should().Be(AppointmentStatus.Scheduled);
        result.StartTime.Should().Be(ValidSlot);
        result.EndTime.Should().Be(ValidSlot.Add(Thirty));
    }

    // ── RescheduleAfterMissedAsync ───────────────────────────────────────────

    [Fact]
    public async Task RescheduleAfterMissedAsync_NotMissedStatus_ThrowsArgumentException()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo) = BuildMocks();
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo);

        var appointment = new Appointment(Patient1, Therapist1, Room1, ValidSlot, Thirty);
        // Status is Scheduled (default), not Missed

        var act = async () => await sut.RescheduleAfterMissedAsync(appointment);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Missed*");
    }

    [Fact]
    public async Task RescheduleAfterMissedAsync_NoConflicts_ReturnsAppointmentAfterMissedSlot()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo) = BuildMocks();
        patientRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Patient1);
        therapistRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Therapist1);
        roomRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Room1);
        apptRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Appointment>());
        apptRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment a, CancellationToken _) => a);
        SetupLocationDeps(timeSlotRepo, locationRepo, roomRepo, scheduleConflictRepo);

        var missed = new Appointment(Patient1, Therapist1, Room1, ValidSlot, Thirty);
        missed.MarkAsMissed();

        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo);
        var result = await sut.RescheduleAfterMissedAsync(missed);

        result.PatientId.Should().Be(1);
        result.TherapistId.Should().Be(1);
        result.RoomId.Should().Be(1);
        result.StartTime.Should().BeAfter(ValidSlot);
        // Same time-of-day preferred: first candidate tried is the next weekday at 09:00
        result.StartTime.TimeOfDay.Should().Be(ValidSlot.TimeOfDay);
    }

    [Fact]
    public async Task RescheduleAfterMissedAsync_NoSlotIn30Days_ThrowsInvalidOperationException()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo) = BuildMocks();
        patientRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Patient1);
        therapistRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Therapist1);
        roomRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Room1);
        SetupLocationDeps(timeSlotRepo, locationRepo, roomRepo, scheduleConflictRepo);
        // Therapist booked solid — every FindAsync returns a conflicting appointment
        var blocking = new Appointment(MakePatient(2), Therapist1, MakeRoom(2), ValidSlot, Thirty);
        apptRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { blocking });

        var missed = new Appointment(Patient1, Therapist1, Room1, ValidSlot, Thirty);
        missed.MarkAsMissed();

        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo, timeSlotRepo, locationRepo, scheduleConflictRepo);
        var act = async () => await sut.RescheduleAfterMissedAsync(missed);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*30 days*");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static readonly DateTime ValidSlot = new(2030, 6, 3, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Thirty = TimeSpan.FromMinutes(30);

    private static readonly Location Loc = new("Main", "123 St") { Id = 1 };
    private static readonly Patient Patient1 = MakePatient(1);
    private static readonly Therapist Therapist1 = MakeTherapist(1);
    private static readonly Room Room1 = MakeRoom(1);

    private static Patient MakePatient(int id) =>
        new("First", "Last", $"p{id}@test.com", new DateOnly(1990, 1, 1)) { Id = id };

    private static Therapist MakeTherapist(int id) =>
        new("Dr", $"T{id}", $"t{id}@clinic.com") { Id = id };

    private static Room MakeRoom(int id) =>
        new($"Room{id}", 1, Loc) { Id = id };

    private static (
        Mock<IRepository<Appointment>>,
        Mock<IRepository<Patient>>,
        Mock<IRepository<Therapist>>,
        Mock<IRepository<Room>>,
        Mock<IRepository<TimeSlot>>,
        Mock<IRepository<Location>>,
        Mock<IRepository<ScheduleConflict>>) BuildMocks() =>
        (new(), new(), new(), new(), new(), new(), new());

    private static AppointmentSchedulingService BuildSut(
        Mock<IRepository<Appointment>> apptRepo,
        Mock<IRepository<Patient>> patientRepo,
        Mock<IRepository<Therapist>> therapistRepo,
        Mock<IRepository<Room>> roomRepo) =>
        BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo, new(), new(), new());

    private static AppointmentSchedulingService BuildSut(
        Mock<IRepository<Appointment>> apptRepo,
        Mock<IRepository<Patient>> patientRepo,
        Mock<IRepository<Therapist>> therapistRepo,
        Mock<IRepository<Room>> roomRepo,
        Mock<IRepository<TimeSlot>> timeSlotRepo,
        Mock<IRepository<Location>> locationRepo,
        Mock<IRepository<ScheduleConflict>> scheduleConflictRepo) =>
        new(apptRepo.Object, patientRepo.Object, therapistRepo.Object, roomRepo.Object,
            timeSlotRepo.Object, locationRepo.Object, scheduleConflictRepo.Object);

    private static void SetupCoreEntities(
        Mock<IRepository<Patient>> patientRepo,
        Mock<IRepository<Therapist>> therapistRepo,
        Mock<IRepository<Room>> roomRepo)
    {
        patientRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Patient1);
        therapistRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Therapist1);
        roomRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Room1);
    }

    private static void SetupLocationDeps(
        Mock<IRepository<TimeSlot>> timeSlotRepo,
        Mock<IRepository<Location>> locationRepo,
        Mock<IRepository<Room>> roomRepo,
        Mock<IRepository<ScheduleConflict>> scheduleConflictRepo)
    {
        // No configured time slots — fall back to default 8–5 weekday schedule
        timeSlotRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<TimeSlot, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TimeSlot>());

        locationRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Loc);

        roomRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Room> { Room1 });

        scheduleConflictRepo.Setup(r => r.AddAsync(It.IsAny<ScheduleConflict>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduleConflict sc, CancellationToken _) => sc);
    }

    private static DateTime NextOccurrenceOf(DayOfWeek day, int hour)
    {
        var d = new DateTime(2030, 6, 1, hour, 0, 0, DateTimeKind.Utc);
        while (d.DayOfWeek != day) d = d.AddDays(1);
        return d;
    }
}
