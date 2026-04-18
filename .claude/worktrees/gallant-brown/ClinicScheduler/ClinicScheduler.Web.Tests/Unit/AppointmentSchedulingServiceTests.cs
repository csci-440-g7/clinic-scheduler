using System.Linq.Expressions;
using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;
using ClinicScheduler.Core.Services;
using FluentAssertions;
using Moq;

namespace ClinicScheduler.Web.Tests.Unit;

public class AppointmentSchedulingServiceTests
{
    // ── Slot validation (static method) ─────────────────────────────────────

    [Theory]
    [InlineData(DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Sunday)]
    public void ValidateSlot_OnWeekend_ThrowsArgumentException(DayOfWeek day)
    {
        var date = NextOccurrenceOf(day, hour: 9);
        var act = () => AppointmentSchedulingService.ValidateSlot(date);
        act.Should().Throw<ArgumentException>().WithMessage("*weekday*");
    }

    [Theory]
    [InlineData(9, 15)]
    [InlineData(10, 45)]
    [InlineData(14, 1)]
    public void ValidateSlot_NonHalfHourBoundary_ThrowsArgumentException(int hour, int minute)
    {
        var slot = new DateTime(2030, 6, 3, hour, minute, 0, DateTimeKind.Utc); // Monday
        var act = () => AppointmentSchedulingService.ValidateSlot(slot);
        act.Should().Throw<ArgumentException>().WithMessage("*30-minute*");
    }

    [Fact]
    public void ValidateSlot_Before8am_ThrowsArgumentException()
    {
        var slot = new DateTime(2030, 6, 3, 7, 30, 0, DateTimeKind.Utc);
        var act = () => AppointmentSchedulingService.ValidateSlot(slot);
        act.Should().Throw<ArgumentException>().WithMessage("*8:00 AM*");
    }

    [Fact]
    public void ValidateSlot_At5pm_ThrowsArgumentException()
    {
        var slot = new DateTime(2030, 6, 3, 17, 0, 0, DateTimeKind.Utc);
        var act = () => AppointmentSchedulingService.ValidateSlot(slot);
        act.Should().Throw<ArgumentException>().WithMessage("*5:00 PM*");
    }

    [Theory]
    [InlineData(8, 0)]
    [InlineData(8, 30)]
    [InlineData(12, 0)]
    [InlineData(16, 0)]
    [InlineData(16, 30)]
    public void ValidateSlot_ValidWeekdaySlot_DoesNotThrow(int hour, int minute)
    {
        var slot = new DateTime(2030, 6, 3, hour, minute, 0, DateTimeKind.Utc); // Monday
        var act = () => AppointmentSchedulingService.ValidateSlot(slot);
        act.Should().NotThrow();
    }

    // ── CreateAppointmentAsync — entity-not-found guards ────────────────────

    [Fact]
    public async Task CreateAppointmentAsync_PatientNotFound_ThrowsArgumentException()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo) = BuildMocks();
        patientRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo);

        var act = async () => await sut.CreateAppointmentAsync(99, 1, 1, ValidSlot, Thirty);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Patient*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_TherapistNotFound_ThrowsArgumentException()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo) = BuildMocks();
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
        var (apptRepo, patientRepo, therapistRepo, roomRepo) = BuildMocks();
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
        var (apptRepo, patientRepo, therapistRepo, roomRepo) = BuildMocks();
        SetupCoreEntities(patientRepo, therapistRepo, roomRepo);

        // Same therapist, different patient/room
        var blocking = new Appointment(MakePatient(2), Therapist1, MakeRoom(2), ValidSlot, Thirty);
        apptRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { blocking });
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo);

        var act = async () => await sut.CreateAppointmentAsync(1, 1, 1, ValidSlot, Thirty);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*therapist*unavailable*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_RoomConflict_ThrowsInvalidOperationException()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo) = BuildMocks();
        SetupCoreEntities(patientRepo, therapistRepo, roomRepo);

        // Same room, different therapist/patient
        var blocking = new Appointment(MakePatient(2), MakeTherapist(2), Room1, ValidSlot, Thirty);
        apptRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { blocking });
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo);

        var act = async () => await sut.CreateAppointmentAsync(1, 1, 1, ValidSlot, Thirty);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*room*unavailable*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_PatientDoubleBooked_ThrowsInvalidOperationException()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo) = BuildMocks();
        SetupCoreEntities(patientRepo, therapistRepo, roomRepo);

        // Same patient, different therapist/room
        var blocking = new Appointment(Patient1, MakeTherapist(2), MakeRoom(2), ValidSlot, Thirty);
        apptRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { blocking });
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo);

        var act = async () => await sut.CreateAppointmentAsync(1, 1, 1, ValidSlot, Thirty);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*patient*already scheduled*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_CapacityExceeded_ThrowsInvalidOperationException()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo) = BuildMocks();
        SetupCoreEntities(patientRepo, therapistRepo, roomRepo);

        // 12 distinct patients already booked in the same slot
        var existing = Enumerable.Range(2, 12)
            .Select(i => new Appointment(MakePatient(i), MakeTherapist(i), MakeRoom(i), ValidSlot, Thirty))
            .ToList();
        apptRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo);

        var act = async () => await sut.CreateAppointmentAsync(1, 1, 1, ValidSlot, Thirty);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*12 concurrent patients*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_ElevenConcurrentPatients_Succeeds()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo) = BuildMocks();
        SetupCoreEntities(patientRepo, therapistRepo, roomRepo);

        // 11 distinct patients — one slot remaining
        var existing = Enumerable.Range(2, 11)
            .Select(i => new Appointment(MakePatient(i), MakeTherapist(i), MakeRoom(i), ValidSlot, Thirty))
            .ToList();
        apptRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        apptRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment a, CancellationToken _) => a);
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo);

        var result = await sut.CreateAppointmentAsync(1, 1, 1, ValidSlot, Thirty);
        result.Should().NotBeNull();
        result.Status.Should().Be(AppointmentStatus.Scheduled);
    }

    [Fact]
    public async Task CreateAppointmentAsync_NoConflicts_ReturnsScheduledAppointment()
    {
        var (apptRepo, patientRepo, therapistRepo, roomRepo) = BuildMocks();
        SetupCoreEntities(patientRepo, therapistRepo, roomRepo);
        apptRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Appointment>());
        apptRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment a, CancellationToken _) => a);
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo);

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
        var (apptRepo, patientRepo, therapistRepo, roomRepo) = BuildMocks();
        var sut = BuildSut(apptRepo, patientRepo, therapistRepo, roomRepo);

        var appointment = new Appointment(Patient1, Therapist1, Room1, ValidSlot, Thirty);
        // Status is Scheduled (default), not Missed

        var act = async () => await sut.RescheduleAfterMissedAsync(appointment);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Missed*");
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
        Mock<IRepository<Room>>) BuildMocks() =>
        (new(), new(), new(), new());

    private static AppointmentSchedulingService BuildSut(
        Mock<IRepository<Appointment>> apptRepo,
        Mock<IRepository<Patient>> patientRepo,
        Mock<IRepository<Therapist>> therapistRepo,
        Mock<IRepository<Room>> roomRepo) =>
        new(apptRepo.Object, patientRepo.Object, therapistRepo.Object, roomRepo.Object);

    private static void SetupCoreEntities(
        Mock<IRepository<Patient>> patientRepo,
        Mock<IRepository<Therapist>> therapistRepo,
        Mock<IRepository<Room>> roomRepo)
    {
        patientRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Patient1);
        therapistRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Therapist1);
        roomRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Room1);
    }

    private static DateTime NextOccurrenceOf(DayOfWeek day, int hour)
    {
        var d = new DateTime(2030, 6, 1, hour, 0, 0, DateTimeKind.Utc);
        while (d.DayOfWeek != day) d = d.AddDays(1);
        return d;
    }
}
