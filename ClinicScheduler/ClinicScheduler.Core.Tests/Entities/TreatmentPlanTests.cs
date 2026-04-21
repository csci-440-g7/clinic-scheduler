using ClinicScheduler.Core.Entities;
using FluentAssertions;

namespace ClinicScheduler.Core.Tests.Entities;

public class TreatmentPlanTests
{
    private Patient CreateTestPatient() =>
        new("Jane", "Smith", "jane@example.com", new DateOnly(1990, 5, 15));

    private Therapist CreateTestTherapist() =>
        new("Dr. Jones", "jones@clinic.com", "Physical Therapy");

    [Theory]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(50)]
    public void Constructor_WithValidDuration_ShouldSucceed(int totalDays)
    {
        // Arrange
        var patient = CreateTestPatient();
        var therapist = CreateTestTherapist();

        // Act
        var act = () => new TreatmentPlan(patient, therapist, 3, totalDays, new DateOnly(2026, 6, 1));

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(35)]
    [InlineData(40)]
    [InlineData(45)]
    [InlineData(100)]
    public void Constructor_WithInvalidDuration_ShouldThrow(int totalDays)
    {
        // Arrange
        var patient = CreateTestPatient();
        var therapist = CreateTestTherapist();

        // Act
        var act = () => new TreatmentPlan(patient, therapist, 3, totalDays, new DateOnly(2026, 6, 1));

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Total days*");
    }

    [Fact]
    public void ExtendForMissedSession_ShouldAdd7DaysToEndDate()
    {
        // Arrange
        var patient = CreateTestPatient();
        var therapist = CreateTestTherapist();
        var plan = new TreatmentPlan(patient, therapist, 3, 30, new DateOnly(2026, 6, 1));
        var originalEndDate = plan.EndDate;

        // Act
        plan.ExtendForMissedSession();

        // Assert
        plan.EndDate.Should().Be(originalEndDate.AddDays(7));
    }

    [Fact]
    public void ExtendForMissedSession_CalledTwice_ShouldAdd14DaysTotal()
    {
        // Arrange
        var patient = CreateTestPatient();
        var therapist = CreateTestTherapist();
        var plan = new TreatmentPlan(patient, therapist, 3, 30, new DateOnly(2026, 6, 1));
        var originalEndDate = plan.EndDate;

        // Act
        plan.ExtendForMissedSession();
        plan.ExtendForMissedSession();

        // Assert
        plan.EndDate.Should().Be(originalEndDate.AddDays(14));
    }
}
