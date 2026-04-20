using ClinicScheduler.Core.Entities;
using ClinicScheduler.Shared.Pages;
using FluentAssertions;

namespace ClinicScheduler.Core.Tests;

/// <summary>
/// Example-based unit tests for NotesPageHelpers covering edge cases.
/// Validates: Requirements 1.5, 3.2, 3.3, 3.4, 5.1, 5.2
/// </summary>
public class NotesPageHelpersTests
{
    // ---------------------------------------------------------------
    // Helper: create an Appointment with non-null Notes, Patient, Therapist
    // ---------------------------------------------------------------

    private static Appointment MakeAppointment(
        string patientFirst, string patientLast,
        string therapistFirst, string therapistLast,
        string notes, DateTime startTime)
    {
        var patient = new Patient(patientFirst, patientLast, $"{Guid.NewGuid()}@test.com", new DateOnly(1990, 1, 1));
        var therapist = new Therapist(therapistFirst, therapistLast, $"{Guid.NewGuid()}@test.com");
        var location = new Location("Clinic", "123 Main St");
        var room = new Room("Room A", 1, location);
        var appointment = new Appointment(patient, therapist, room, startTime, TimeSpan.FromMinutes(60));
        appointment.Notes = notes;
        return appointment;
    }

    // ===============================================================
    // FilterNotes tests
    // ===============================================================

    [Fact]
    public void FilterNotes_EmptyList_ReturnsEmpty()
    {
        var result = NotesPageHelpers.FilterNotes(new List<Appointment>(), "anything");

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FilterNotes_EmptyOrNullSearchText_ReturnsAllAppointments(string? searchText)
    {
        var appointments = new List<Appointment>
        {
            MakeAppointment("Alice", "Smith", "Dr", "Jones", "Knee pain noted", new DateTime(2024, 3, 1, 10, 0, 0)),
            MakeAppointment("Bob", "Brown", "Dr", "Lee", "Follow-up visit", new DateTime(2024, 4, 15, 14, 0, 0))
        };

        var result = NotesPageHelpers.FilterNotes(appointments, searchText);

        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(appointments);
    }

    [Fact]
    public void FilterNotes_MatchesOnNoteTextOnly()
    {
        var matching = MakeAppointment("Alice", "Smith", "Dr", "Jones", "Shoulder injury observed", new DateTime(2024, 3, 1, 10, 0, 0));
        var nonMatching = MakeAppointment("Bob", "Brown", "Dr", "Lee", "Follow-up visit", new DateTime(2024, 4, 15, 14, 0, 0));
        var appointments = new List<Appointment> { matching, nonMatching };

        var result = NotesPageHelpers.FilterNotes(appointments, "Shoulder");

        result.Should().ContainSingle().Which.Should().BeSameAs(matching);
    }

    [Fact]
    public void FilterNotes_MatchesOnPatientNameOnly()
    {
        var matching = MakeAppointment("Alice", "Smith", "Dr", "Jones", "General checkup", new DateTime(2024, 3, 1, 10, 0, 0));
        var nonMatching = MakeAppointment("Bob", "Brown", "Dr", "Lee", "General checkup", new DateTime(2024, 4, 15, 14, 0, 0));
        var appointments = new List<Appointment> { matching, nonMatching };

        var result = NotesPageHelpers.FilterNotes(appointments, "Alice");

        result.Should().ContainSingle().Which.Should().BeSameAs(matching);
    }

    [Fact]
    public void FilterNotes_MatchesOnTherapistNameOnly()
    {
        var matching = MakeAppointment("Alice", "Smith", "Dr", "Jones", "General checkup", new DateTime(2024, 3, 1, 10, 0, 0));
        var nonMatching = MakeAppointment("Bob", "Brown", "Dr", "Lee", "General checkup", new DateTime(2024, 4, 15, 14, 0, 0));
        var appointments = new List<Appointment> { matching, nonMatching };

        var result = NotesPageHelpers.FilterNotes(appointments, "Jones");

        result.Should().ContainSingle().Which.Should().BeSameAs(matching);
    }

    [Fact]
    public void FilterNotes_CaseInsensitiveSearch()
    {
        var appointment = MakeAppointment("Alice", "Smith", "Dr", "Jones", "Knee pain noted", new DateTime(2024, 3, 1, 10, 0, 0));
        var appointments = new List<Appointment> { appointment };

        var result = NotesPageHelpers.FilterNotes(appointments, "knee PAIN");

        result.Should().ContainSingle().Which.Should().BeSameAs(appointment);
    }

    // ===============================================================
    // GroupByYearDescending tests
    // ===============================================================

    [Fact]
    public void GroupByYearDescending_MultipleYears_GroupsAndOrdersCorrectly()
    {
        var a2022 = MakeAppointment("A", "P", "A", "T", "Note 2022", new DateTime(2022, 6, 15, 10, 0, 0));
        var a2024a = MakeAppointment("B", "P", "B", "T", "Note 2024 early", new DateTime(2024, 2, 1, 9, 0, 0));
        var a2024b = MakeAppointment("C", "P", "C", "T", "Note 2024 late", new DateTime(2024, 11, 20, 14, 0, 0));
        var a2023 = MakeAppointment("D", "P", "D", "T", "Note 2023", new DateTime(2023, 8, 10, 11, 0, 0));

        var grouped = NotesPageHelpers.GroupByYearDescending(new[] { a2022, a2024a, a2024b, a2023 }).ToList();

        // Year groups in descending order
        grouped.Select(g => g.Key).Should().BeInDescendingOrder();
        grouped.Select(g => g.Key).Should().ContainInOrder(2024, 2023, 2022);

        // Within 2024 group, appointments ordered by StartTime descending
        var year2024 = grouped.First(g => g.Key == 2024).ToList();
        year2024.Should().HaveCount(2);
        year2024[0].Should().BeSameAs(a2024b); // Nov 2024 before Feb 2024
        year2024[1].Should().BeSameAs(a2024a);
    }

    [Fact]
    public void GroupByYearDescending_SingleYear_ReturnsSingleGroupOrderedDescending()
    {
        var early = MakeAppointment("A", "P", "A", "T", "Early note", new DateTime(2024, 1, 5, 8, 0, 0));
        var late = MakeAppointment("B", "P", "B", "T", "Late note", new DateTime(2024, 12, 20, 16, 0, 0));

        var grouped = NotesPageHelpers.GroupByYearDescending(new[] { early, late }).ToList();

        grouped.Should().ContainSingle();
        grouped[0].Key.Should().Be(2024);

        var items = grouped[0].ToList();
        items.Should().HaveCount(2);
        items[0].Should().BeSameAs(late);  // Dec before Jan
        items[1].Should().BeSameAs(early);
    }

    // ===============================================================
    // TruncateNoteText tests
    // ===============================================================

    [Fact]
    public void TruncateNoteText_ExactlyAtMaxLength_ReturnsUnchanged()
    {
        var text = "Hello World"; // 11 chars
        var result = NotesPageHelpers.TruncateNoteText(text, 11);

        result.Should().Be(text);
    }

    [Fact]
    public void TruncateNoteText_OneCharOverMaxLength_TruncatesWithEllipsis()
    {
        var text = "Hello World!"; // 12 chars
        var result = NotesPageHelpers.TruncateNoteText(text, 11);

        result.Should().HaveLength(11);
        result.Should().EndWith("...");
        result.Should().Be("Hello Wo...");
    }

    [Fact]
    public void TruncateNoteText_EmptyString_ReturnsEmpty()
    {
        var result = NotesPageHelpers.TruncateNoteText("", 10);

        result.Should().BeEmpty();
    }

    // ===============================================================
    // FormatListEntryDate tests
    // ===============================================================

    [Fact]
    public void FormatListEntryDate_ProducesExpectedFormat()
    {
        var date = new DateTime(2024, 3, 1, 10, 30, 0);

        var result = NotesPageHelpers.FormatListEntryDate(date);

        result.Should().Be("Mar 1");
    }

    // ===============================================================
    // FormatPreviewDate tests
    // ===============================================================

    [Fact]
    public void FormatPreviewDate_ProducesExpectedFormat()
    {
        var date = new DateTime(2024, 12, 25, 14, 0, 0);

        var result = NotesPageHelpers.FormatPreviewDate(date);

        result.Should().Be("December 25, 2024");
    }
}
