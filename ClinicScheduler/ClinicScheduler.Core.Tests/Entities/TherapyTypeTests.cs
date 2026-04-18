using ClinicScheduler.Core.Entities;
using FluentAssertions;

namespace ClinicScheduler.Core.Tests.Entities;

public class TherapyTypeTests
{
    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var tt = new TherapyType("Manual Therapy", "Hands-on soft tissue work", "Physical Therapy", "#4CAF50");

        // Assert
        tt.Name.Should().Be("Manual Therapy");
        tt.Description.Should().Be("Hands-on soft tissue work");
        tt.Specialty.Should().Be("Physical Therapy");
        tt.ColorCode.Should().Be("#4CAF50");
    }

    [Fact]
    public void Constructor_WithNullOptionalFields_ShouldLeaveThemNull()
    {
        // Arrange & Act
        var tt = new TherapyType("Exercise Therapy", null, null, null);

        // Assert
        tt.Description.Should().BeNull();
        tt.Specialty.Should().BeNull();
        tt.ColorCode.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_ShouldThrow(string? badName)
    {
        // Act
        var act = () => new TherapyType(badName!, null, null, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    // ── Color validation ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("#4CAF50")]
    [InlineData("#fff")]
    [InlineData("#ABC")]
    [InlineData("#aabbcc")]
    public void Constructor_WithValidHexColor_ShouldAcceptIt(string color)
    {
        // Act
        var act = () => new TherapyType("Therapy", null, null, color);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("4CAF50")]    // missing #
    [InlineData("#GGGGGG")]   // invalid hex digits
    [InlineData("#12345")]    // wrong length
    [InlineData("red")]       // named color
    [InlineData("#1234567")]  // too long
    public void Constructor_WithInvalidHexColor_ShouldThrow(string badColor)
    {
        // Act
        var act = () => new TherapyType("Therapy", null, null, badColor);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("colorCode");
    }

    // ── UpdateDetails ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDetails_ShouldUpdateAllFields()
    {
        // Arrange
        var tt = new TherapyType("Old Name", "Old desc", "Old specialty", "#000");
        var before = tt.UpdatedAt;

        // Act
        await Task.Delay(10);
        tt.UpdateDetails("New Name", "New desc", "New specialty", "#ffffff");

        // Assert
        tt.Name.Should().Be("New Name");
        tt.Description.Should().Be("New desc");
        tt.Specialty.Should().Be("New specialty");
        tt.ColorCode.Should().Be("#ffffff");
        tt.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task UpdateDetails_WithNullOptionals_ShouldClearThem()
    {
        // Arrange
        var tt = new TherapyType("Manual Therapy", "desc", "specialty", "#4CAF50");

        // Act
        await Task.Delay(10);
        tt.UpdateDetails("Manual Therapy", null, null, null);

        // Assert
        tt.Description.Should().BeNull();
        tt.Specialty.Should().BeNull();
        tt.ColorCode.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_WithBlankName_ShouldThrow(string? badName)
    {
        // Arrange
        var tt = new TherapyType("Manual Therapy", null, null, null);

        // Act
        var act = () => tt.UpdateDetails(badName!, null, null, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDetails_WithInvalidHexColor_ShouldThrow()
    {
        // Arrange
        var tt = new TherapyType("Manual Therapy", null, null, null);

        // Act
        var act = () => tt.UpdateDetails("Manual Therapy", null, null, "notacolor");

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("colorCode");
    }

    [Fact]
    public void Constructor_ShouldInitializeEmptyTherapyCollection()
    {
        // Arrange & Act
        var tt = new TherapyType("Exercise", null, null, null);

        // Assert
        tt.TreatmentPlanTherapies.Should().BeEmpty();
    }
}
