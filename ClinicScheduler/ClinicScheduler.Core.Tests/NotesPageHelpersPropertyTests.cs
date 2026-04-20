using System.Globalization;
using ClinicScheduler.Core.Entities;
using ClinicScheduler.Shared.Pages;
using FsCheck;
using FsCheck.Xunit;

namespace ClinicScheduler.Core.Tests;

/// <summary>
/// Property-based tests for NotesPageHelpers using FsCheck.
/// Validates correctness properties defined in the wire-notes-page design document.
/// </summary>
public class NotesPageHelpersPropertyTests
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

    /// <summary>
    /// Constrains FsCheck-generated values into a valid DateTime.
    /// </summary>
    private static DateTime MakeDateTime(PositiveInt yearOffset, byte month, byte day, byte hour, byte minute)
    {
        var year = 2000 + (yearOffset.Get % 31); // 2000-2030
        var validMonth = (month % 12) + 1;
        var maxDay = DateTime.DaysInMonth(year, validMonth);
        var validDay = (day % maxDay) + 1;
        var validHour = hour % 24;
        var validMinute = minute % 60;
        return new DateTime(year, validMonth, validDay, validHour, validMinute, 0);
    }

    // ---------------------------------------------------------------
    // Property 1: Year-grouped ordering preserves descending chronological order
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 1: Year-grouped ordering preserves descending chronological order**
    ///
    /// For any list of appointments with non-null notes, grouping by StartTime.Year
    /// shall produce year groups in descending order, and within each year group,
    /// appointments shall be ordered by StartTime descending.
    ///
    /// **Validates: Requirements 1.4, 2.1, 2.2, 2.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool YearGroupedOrdering_PreservesDescendingChronologicalOrder(
        NonEmptyString notes1, NonEmptyString notes2, NonEmptyString notes3,
        PositiveInt y1, byte mo1, byte d1, byte h1, byte mi1,
        PositiveInt y2, byte mo2, byte d2, byte h2, byte mi2,
        PositiveInt y3, byte mo3, byte d3, byte h3, byte mi3)
    {
        var dt1 = MakeDateTime(y1, mo1, d1, h1, mi1);
        var dt2 = MakeDateTime(y2, mo2, d2, h2, mi2);
        var dt3 = MakeDateTime(y3, mo3, d3, h3, mi3);

        var appointments = new List<Appointment>
        {
            MakeAppointment("A", "Patient", "A", "Therapist", notes1.Get, dt1),
            MakeAppointment("B", "Patient", "B", "Therapist", notes2.Get, dt2),
            MakeAppointment("C", "Patient", "C", "Therapist", notes3.Get, dt3)
        };

        var grouped = NotesPageHelpers.GroupByYearDescending(appointments).ToList();

        // Year groups are in descending order
        var years = grouped.Select(g => g.Key).ToList();
        for (int i = 0; i < years.Count - 1; i++)
        {
            if (years[i] < years[i + 1])
                return false;
        }

        // Within each group, appointments are ordered by StartTime descending
        foreach (var group in grouped)
        {
            var startTimes = group.Select(a => a.StartTime).ToList();
            for (int i = 0; i < startTimes.Count - 1; i++)
            {
                if (startTimes[i] < startTimes[i + 1])
                    return false;
            }
        }

        return true;
    }

    // ---------------------------------------------------------------
    // Property 2: Search filter returns exactly the matching notes
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 2: Search filter returns exactly the matching notes**
    ///
    /// For any search text and list of appointments, the filtered result contains
    /// every appointment where note text, patient full name, or therapist full name
    /// contains the search text (case-insensitive), and excludes all others.
    ///
    /// **Validates: Requirements 3.2, 3.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SearchFilter_ReturnsExactlyMatchingNotes(
        NonEmptyString searchText,
        NonEmptyString pFirst1, NonEmptyString pLast1, NonEmptyString tFirst1, NonEmptyString tLast1, NonEmptyString note1,
        NonEmptyString pFirst2, NonEmptyString pLast2, NonEmptyString tFirst2, NonEmptyString tLast2, NonEmptyString note2)
    {
        var search = searchText.Get;
        var dt = new DateTime(2024, 6, 15, 10, 0, 0);

        var appointments = new List<Appointment>
        {
            MakeAppointment(pFirst1.Get, pLast1.Get, tFirst1.Get, tLast1.Get, note1.Get, dt),
            MakeAppointment(pFirst2.Get, pLast2.Get, tFirst2.Get, tLast2.Get, note2.Get, dt.AddDays(1))
        };

        var filtered = NotesPageHelpers.FilterNotes(appointments, search);

        // Compute expected matches independently
        var expected = appointments.Where(a =>
            (a.Notes != null && a.Notes.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
            (a.Patient?.FullName != null && a.Patient.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
            (a.Therapist?.FullName != null && a.Therapist.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        // Same count
        if (filtered.Count != expected.Count)
            return false;

        // Every expected appointment is in the filtered result
        if (!expected.All(e => filtered.Contains(e)))
            return false;

        // Every filtered appointment is in the expected result
        if (!filtered.All(f => expected.Contains(f)))
            return false;

        return true;
    }

    // ---------------------------------------------------------------
    // Property 3: Note preview contains all required fields
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 3: Note preview contains all required fields**
    ///
    /// For any appointment with non-null note, patient, and therapist, the appointment
    /// object exposes full note text, patient full name, therapist full name, and
    /// appointment date.
    ///
    /// **Validates: Requirements 4.1**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool NotePreview_ContainsAllRequiredFields(
        NonEmptyString patientFirst, NonEmptyString patientLast,
        NonEmptyString therapistFirst, NonEmptyString therapistLast,
        NonEmptyString notes,
        PositiveInt yearOffset, byte month, byte day, byte hour, byte minute)
    {
        var startTime = MakeDateTime(yearOffset, month, day, hour, minute);
        var appointment = MakeAppointment(
            patientFirst.Get, patientLast.Get,
            therapistFirst.Get, therapistLast.Get,
            notes.Get, startTime);

        // Note text is accessible and non-null
        if (string.IsNullOrEmpty(appointment.Notes))
            return false;

        // Patient full name is accessible and contains first/last name
        if (appointment.Patient == null)
            return false;
        if (!appointment.Patient.FullName.Contains(patientFirst.Get))
            return false;
        if (!appointment.Patient.FullName.Contains(patientLast.Get))
            return false;

        // Therapist full name is accessible and contains first/last name
        if (appointment.Therapist == null)
            return false;
        if (!appointment.Therapist.FullName.Contains(therapistFirst.Get))
            return false;
        if (!appointment.Therapist.FullName.Contains(therapistLast.Get))
            return false;

        // Appointment date is set
        if (appointment.StartTime == default)
            return false;

        return true;
    }

    // ---------------------------------------------------------------
    // Property 4: Preview date format includes day, month, and year
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 4: Preview date format includes day, month, and year**
    ///
    /// For any DateTime, FormatPreviewDate output contains the numeric day,
    /// month name or abbreviation, and four-digit year.
    ///
    /// **Validates: Requirements 4.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool PreviewDateFormat_IncludesDayMonthAndYear(
        PositiveInt yearOffset, byte month, byte day, byte hour, byte minute)
    {
        var dateTime = MakeDateTime(yearOffset, month, day, hour, minute);
        var formatted = NotesPageHelpers.FormatPreviewDate(dateTime);

        // Contains the numeric day
        var dayStr = dateTime.Day.ToString();
        if (!formatted.Contains(dayStr))
            return false;

        // Contains the month name or abbreviation
        var fullMonthName = dateTime.ToString("MMMM", CultureInfo.InvariantCulture);
        var abbrevMonthName = dateTime.ToString("MMM", CultureInfo.InvariantCulture);
        if (!formatted.Contains(fullMonthName, StringComparison.OrdinalIgnoreCase) &&
            !formatted.Contains(abbrevMonthName, StringComparison.OrdinalIgnoreCase))
            return false;

        // Contains the four-digit year
        var yearStr = dateTime.Year.ToString("D4");
        if (!formatted.Contains(yearStr))
            return false;

        return true;
    }

    // ---------------------------------------------------------------
    // Property 5: List entry date format matches abbreviated month and day
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 5: List entry date format matches abbreviated month and day**
    ///
    /// For any DateTime, FormatListEntryDate output matches the abbreviated month
    /// name followed by the day number.
    ///
    /// **Validates: Requirements 5.1**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ListEntryDateFormat_MatchesAbbreviatedMonthAndDay(
        PositiveInt yearOffset, byte month, byte day, byte hour, byte minute)
    {
        var dateTime = MakeDateTime(yearOffset, month, day, hour, minute);
        var formatted = NotesPageHelpers.FormatListEntryDate(dateTime);

        // Should contain abbreviated month name
        var abbrevMonth = dateTime.ToString("MMM", CultureInfo.InvariantCulture);
        if (!formatted.Contains(abbrevMonth))
            return false;

        // Should contain the day number
        var dayStr = dateTime.Day.ToString();
        if (!formatted.Contains(dayStr))
            return false;

        // Should match the exact pattern "MMM d"
        var expected = dateTime.ToString("MMM d", CultureInfo.InvariantCulture);
        return formatted == expected;
    }

    // ---------------------------------------------------------------
    // Property 6: Text truncation respects maximum length
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 6: Text truncation respects maximum length**
    ///
    /// For any string, truncated output never exceeds max length, strings at or
    /// under the limit are unchanged, and strings over the limit end with an ellipsis.
    ///
    /// **Validates: Requirements 5.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool TextTruncation_RespectsMaximumLength(NonEmptyString inputNes, PositiveInt maxLengthPos)
    {
        var input = inputNes.Get;
        // Ensure maxLength is at least 4 so there's room for at least one char + "..."
        var maxLength = Math.Max(4, maxLengthPos.Get);

        var result = NotesPageHelpers.TruncateNoteText(input, maxLength);

        // Result never exceeds max length
        if (result.Length > maxLength)
            return false;

        if (input.Length <= maxLength)
        {
            // Strings at or under the limit are unchanged
            if (result != input)
                return false;
        }
        else
        {
            // Strings over the limit end with ellipsis
            if (!result.EndsWith("..."))
                return false;

            // And the total length equals maxLength
            if (result.Length != maxLength)
                return false;
        }

        return true;
    }
}
