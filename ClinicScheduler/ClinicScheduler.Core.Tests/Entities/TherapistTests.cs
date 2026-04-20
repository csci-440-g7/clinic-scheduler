using ClinicScheduler.Core.Entities;
using FluentAssertions;

namespace ClinicScheduler.Core.Tests.Entities;

public class TherapistTests
{
    [Fact]
    public void Constructor_ShouldCreateTherapistWithCorrectProperties()
    {
        // Arrange & Act
        var therapist = new Therapist("Alice", "Chen", "alice@clinic.com", "555-1234", "Physical Therapy");

        // Assert
        therapist.FirstName.Should().Be("Alice");
        therapist.LastName.Should().Be("Chen");
        therapist.Email.Should().Be("alice@clinic.com");
        therapist.Phone.Should().Be("555-1234");
        therapist.Specialty.Should().Be("Physical Therapy");
        therapist.FullName.Should().Be("Alice Chen");
    }

    [Fact]
    public void Constructor_WithoutPhoneOrSpecialty_ShouldLeaveThemNull()
    {
        // Arrange & Act
        var therapist = new Therapist("Bob", "Lee", "bob@clinic.com");

        // Assert
        therapist.Phone.Should().BeNull();
        therapist.Specialty.Should().BeNull();
    }

    [Fact]
    public void FullName_ShouldConcatenateFirstAndLastName()
    {
        // Arrange
        var therapist = new Therapist("Maria", "Gomez", "m@clinic.com");

        // Act & Assert
        therapist.FullName.Should().Be("Maria Gomez");
    }

    [Fact]
    public async Task UpdateContactInfo_ShouldUpdateEmailAndPhone()
    {
        // Arrange
        var therapist = new Therapist("Alice", "Chen", "old@clinic.com", "000-0000");
        var before = therapist.UpdatedAt;

        // Act
        await Task.Delay(10);
        therapist.UpdateContactInfo("new@clinic.com", "555-9999");

        // Assert
        therapist.Email.Should().Be("new@clinic.com");
        therapist.Phone.Should().Be("555-9999");
        therapist.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task UpdateContactInfo_WithNullPhone_ShouldClearPhone()
    {
        // Arrange
        var therapist = new Therapist("Alice", "Chen", "alice@clinic.com", "555-1234");

        // Act
        await Task.Delay(10);
        therapist.UpdateContactInfo("alice@clinic.com", null);

        // Assert
        therapist.Phone.Should().BeNull();
    }

    [Fact]
    public async Task UpdateDetails_ShouldUpdateNameAndSpecialty()
    {
        // Arrange
        var therapist = new Therapist("Alice", "Chen", "alice@clinic.com", specialty: "Physical Therapy");
        var before = therapist.UpdatedAt;

        // Act
        await Task.Delay(10);
        therapist.UpdateDetails("Alicia", "Chen-Park", "Chiropractic");

        // Assert
        therapist.FirstName.Should().Be("Alicia");
        therapist.LastName.Should().Be("Chen-Park");
        therapist.Specialty.Should().Be("Chiropractic");
        therapist.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task UpdateDetails_WithNullSpecialty_ShouldClearSpecialty()
    {
        // Arrange
        var therapist = new Therapist("Alice", "Chen", "alice@clinic.com", specialty: "Physical Therapy");

        // Act
        await Task.Delay(10);
        therapist.UpdateDetails("Alice", "Chen", null);

        // Assert
        therapist.Specialty.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializeEmptyCollections()
    {
        // Arrange & Act
        var therapist = new Therapist("Alice", "Chen", "alice@clinic.com");

        // Assert
        therapist.TreatmentPlans.Should().BeEmpty();
        therapist.Appointments.Should().BeEmpty();
    }
}
