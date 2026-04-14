using ClinicScheduler.Core.Entities;
using FluentAssertions;

namespace ClinicScheduler.Core.Tests.Entities;

public class PatientTests
{
    [Fact]
    public void Constructor_ShouldCreatePatientWithCorrectProperties()
    {
        // Arrange
        var firstName = "Jane";
        var lastName = "Smith";
        var email = "jane.smith@example.com";
        var dateOfBirth = new DateOnly(1990, 5, 15);
        var phone = "555-0123";

        // Act
        var patient = new Patient(firstName, lastName, email, dateOfBirth, phone);

        // Assert
        patient.FirstName.Should().Be(firstName);
        patient.LastName.Should().Be(lastName);
        patient.Email.Should().Be(email);
        patient.DateOfBirth.Should().Be(dateOfBirth);
        patient.Phone.Should().Be(phone);
        patient.FullName.Should().Be("Jane Smith");
    }

    [Fact]
    public void Constructor_WithoutPhone_ShouldCreatePatientWithNullPhone()
    {
        // Arrange & Act
        var patient = new Patient("John", "Doe", "john@example.com", new DateOnly(1980, 1, 1));

        // Assert
        patient.Phone.Should().BeNull();
    }

    [Fact]
    public async Task UpdateContactInfo_ShouldUpdateEmailAndPhone()
    {
        // Arrange
        var patient = new Patient("John", "Doe", "john@example.com", new DateOnly(1980, 1, 1));
        var newEmail = "john.doe@newdomain.com";
        var newPhone = "555-9999";
        var beforeUpdate = patient.UpdatedAt;

        // Act
        await Task.Delay(10); // Ensure time difference
        patient.UpdateContactInfo(newEmail, newPhone);

        // Assert
        patient.Email.Should().Be(newEmail);
        patient.Phone.Should().Be(newPhone);
        patient.UpdatedAt.Should().BeAfter(beforeUpdate);
    }

    [Fact]
    public void FullName_ShouldConcatenateFirstAndLastName()
    {
        // Arrange
        var patient = new Patient("Alice", "Wonder", "alice@example.com", new DateOnly(1995, 3, 20));

        // Act
        var fullName = patient.FullName;

        // Assert
        fullName.Should().Be("Alice Wonder");
    }
}
