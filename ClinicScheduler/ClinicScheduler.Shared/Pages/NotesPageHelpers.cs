using ClinicScheduler.Core.Entities;

namespace ClinicScheduler.Shared.Pages;

/// <summary>
/// Static helper methods for the Notes page — filtering, grouping, formatting, and truncation.
/// Extracted from the Razor component for direct unit and property testing.
/// </summary>
public static class NotesPageHelpers
{
    /// <summary>
    /// Filters appointments by search text against note text, patient full name, or therapist full name (case-insensitive).
    /// Returns all appointments when search text is null or empty.
    /// </summary>
    public static List<Appointment> FilterNotes(List<Appointment> appointments, string? searchText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            return appointments;
        }

        return appointments
            .Where(a =>
                (a.Notes != null && a.Notes.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (a.Patient?.FullName != null && a.Patient.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (a.Therapist?.FullName != null && a.Therapist.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>
    /// Groups appointments by StartTime.Year descending, with appointments within each group ordered by StartTime descending.
    /// </summary>
    public static IEnumerable<IGrouping<int, Appointment>> GroupByYearDescending(IEnumerable<Appointment> appointments)
    {
        return appointments
            .GroupBy(a => a.StartTime.Year)
            .OrderByDescending(g => g.Key)
            .Select(g => new OrderedGrouping<int, Appointment>(
                g.Key,
                g.OrderByDescending(a => a.StartTime)));
    }

    /// <summary>
    /// Formats a date as abbreviated month and day (e.g., "Mar 1").
    /// </summary>
    public static string FormatListEntryDate(DateTime dateTime)
    {
        return dateTime.ToString("MMM d");
    }

    /// <summary>
    /// Formats a date in a human-readable format including day, month name, and four-digit year.
    /// </summary>
    public static string FormatPreviewDate(DateTime dateTime)
    {
        return dateTime.ToString("MMMM d, yyyy");
    }

    /// <summary>
    /// Returns text unchanged if within the max length limit, otherwise truncates and appends "...".
    /// </summary>
    public static string TruncateNoteText(string noteText, int maxLength)
    {
        if (noteText.Length <= maxLength)
        {
            return noteText;
        }

        return noteText[..(maxLength - 3)] + "...";
    }

    /// <summary>
    /// Helper class to represent an ordered grouping that preserves the internal ordering.
    /// </summary>
    private sealed class OrderedGrouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        private readonly IEnumerable<TElement> _elements;

        public OrderedGrouping(TKey key, IEnumerable<TElement> elements)
        {
            Key = key;
            _elements = elements;
        }

        public TKey Key { get; }

        public IEnumerator<TElement> GetEnumerator() => _elements.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
