using ClinicScheduler.Core.Entities;
using FluentAssertions;

namespace ClinicScheduler.Core.Tests.Entities;

public class AppointmentTests
{
    private Patient CreateTestPatient() => 
        new("John", "Doe", "john@example.com", new DateOnly(1980, 1, 1));

    private Therapist CreateTestTherapist() => 
        new("Dr. Smith", "smith@clinic.com", "Physical Therapy");

    private Room CreateTestRoom() => 
        new("Room 101", 1, new Location("Main Clinic", "123 Main St"));

    [Fact]
    public void Constructor_ShouldCreateAppointmentWithCorrectProperties()
    {
        // Arrange
        var patient = CreateTestPatient();
        var therapist = CreateTestTherapist();
        var room = CreateTestRoom();
        var startTime = DateTime.UtcNow.AddHours(1);
        var duration = TimeSpan.FromMinutes(60);

        // Act
        var appointment = new Appointment(patient, therapist, room, startTime, duration);

        // Assert
        appointment.Patient.Should().Be(patient);
        appointment.Therapist.Should().Be(therapist);
        appointment.Room.Should().Be(room);
        appointment.StartTime.Should().Be(startTime);
        appointment.EndTime.Should().Be(startTime.Add(duration));
        appointment.Status.Should().Be(AppointmentStatus.Scheduled);
        appointment.HasConflict.Should().BeFalse();
    }

    [Fact]
    public void SetDuration_WithPositiveDuration_ShouldUpdateEndTime()
    {
        // Arrange
        var appointment = new Appointment(
            CreateTestPatient(), 
            CreateTestTherapist(), 
            CreateTestRoom(), 
            DateTime.UtcNow, 
            TimeSpan.FromMinutes(30));
        var newDuration = TimeSpan.FromMinutes(90);

        // Act
        appointment.SetDuration(newDuration);

        // Assert
        appointment.EndTime.Should().Be(appointment.StartTime.Add(newDuration));
    }

    [Fact]
    public void SetDuration_WithZeroOrNegativeDuration_ShouldThrowException()
    {
        // Arrange
        var appointment = new Appointment(
            CreateTestPatient(), 
            CreateTestTherapist(), 
            CreateTestRoom(), 
            DateTime.UtcNow, 
            TimeSpan.FromMinutes(30));

        // Act & Assert
        var act = () => appointment.SetDuration(TimeSpan.Zero);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Cancel_OnScheduledAppointment_ShouldUpdateStatus()
    {
        // Arrange
        var appointment = new Appointment(
            CreateTestPatient(), 
            CreateTestTherapist(), 
            CreateTestRoom(), 
            DateTime.UtcNow, 
            TimeSpan.FromMinutes(30));

        // Act
        appointment.Cancel();

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Canceled);
    }

    [Fact]
    public void Cancel_OnCompletedAppointment_ShouldThrowException()
    {
        // Arrange
        var appointment = new Appointment(
            CreateTestPatient(), 
            CreateTestTherapist(), 
            CreateTestRoom(), 
            DateTime.UtcNow.AddHours(-2), 
            TimeSpan.FromMinutes(30));
        appointment.Complete();

        // Act & Assert
        var act = () => appointment.Cancel();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already Completed*");
    }

    [Fact]
    public void Complete_OnScheduledAppointment_ShouldUpdateStatus()
    {
        // Arrange
        var appointment = new Appointment(
            CreateTestPatient(), 
            CreateTestTherapist(), 
            CreateTestRoom(), 
            DateTime.UtcNow.AddHours(-1), 
            TimeSpan.FromMinutes(30));

        // Act
        appointment.Complete();

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Completed);
    }

    [Fact]
    public void Complete_OnCanceledAppointment_ShouldThrowException()
    {
        // Arrange
        var appointment = new Appointment(
            CreateTestPatient(), 
            CreateTestTherapist(), 
            CreateTestRoom(), 
            DateTime.UtcNow, 
            TimeSpan.FromMinutes(30));
        appointment.Cancel();

        // Act & Assert
        var act = () => appointment.Complete();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only a scheduled appointment can be marked as completed.");
    }

    [Fact]
    public void Reschedule_OnScheduledAppointment_ShouldUpdateTimeAndStatus()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var duration = TimeSpan.FromMinutes(60);
        var appointment = new Appointment(
            CreateTestPatient(), 
            CreateTestTherapist(), 
            CreateTestRoom(), 
            startTime, 
            duration);
        var newStartTime = DateTime.UtcNow.AddDays(1);

        // Act
        appointment.Reschedule(newStartTime);

        // Assert
        appointment.StartTime.Should().Be(newStartTime);
        appointment.EndTime.Should().Be(newStartTime.Add(duration));
        appointment.Status.Should().Be(AppointmentStatus.Rescheduled);
    }

    [Fact]
    public void Reschedule_OnCompletedAppointment_ShouldThrowException()
    {
        // Arrange
        var appointment = new Appointment(
            CreateTestPatient(), 
            CreateTestTherapist(), 
            CreateTestRoom(), 
            DateTime.UtcNow.AddHours(-2), 
            TimeSpan.FromMinutes(30));
        appointment.Complete();

        // Act & Assert
        var act = () => appointment.Reschedule(DateTime.UtcNow.AddDays(1));
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Completed*");
    }

    [Fact]
    public void MarkAsMissed_OnScheduledAppointment_ShouldUpdateStatus()
    {
        // Arrange
        var appointment = new Appointment(
            CreateTestPatient(), 
            CreateTestTherapist(), 
            CreateTestRoom(), 
            DateTime.UtcNow.AddHours(-1), 
            TimeSpan.FromMinutes(30));

        // Act
        appointment.MarkAsMissed();

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Missed);
    }

    [Fact]
    public void MarkAsMissed_OnCompletedAppointment_ShouldThrowException()
    {
        // Arrange
        var appointment = new Appointment(
            CreateTestPatient(), 
            CreateTestTherapist(), 
            CreateTestRoom(), 
            DateTime.UtcNow.AddHours(-2), 
            TimeSpan.FromMinutes(30));
        appointment.Complete();

        // Act & Assert
        var act = () => appointment.MarkAsMissed();
        act.Should().Throw<InvalidOperationException>();
    }
}
