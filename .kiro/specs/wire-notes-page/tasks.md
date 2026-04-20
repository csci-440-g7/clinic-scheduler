# Implementation Plan: Wire Notes Page

## Overview

Replace the hardcoded mock data in `Notes.razor` with real database queries using Entity Framework Core. The implementation extracts filtering, grouping, formatting, and truncation logic into static helper methods for testability, then wires the Razor component to `ClinicDbContext` following the same pattern as `PatientDashboard.razor`. Property-based tests validate correctness properties with FsCheck, and example-based unit tests cover edge cases.

## Tasks

- [x] 1. Create static helper class with filtering, grouping, formatting, and truncation methods
  - Create a new file `NotesPageHelpers.cs` in `ClinicScheduler.Shared/Pages/`
  - Implement `FilterNotes(List<Appointment> appointments, string searchText)` — returns appointments where note text, patient full name, or therapist full name contains the search text (case-insensitive); returns all when search text is empty/null
  - Implement `GroupByYearDescending(IEnumerable<Appointment> appointments)` — groups appointments by `StartTime.Year` descending, with appointments within each group ordered by `StartTime` descending
  - Implement `FormatListEntryDate(DateTime dateTime)` — returns abbreviated month and day (e.g., "Mar 1")
  - Implement `FormatPreviewDate(DateTime dateTime)` — returns human-readable format including day, month name, and four-digit year
  - Implement `TruncateNoteText(string noteText, int maxLength)` — returns text unchanged if within limit, otherwise truncates and appends ellipsis
  - All methods must be `public static` for direct unit and property testing without Blazor infrastructure
  - _Requirements: 1.4, 2.1, 2.2, 2.3, 3.2, 3.3, 3.5, 4.4, 5.1, 5.2_

- [x] 2. Write property-based tests for helper methods
  - [x] 2.1 Create test file `NotesPageHelpersPropertyTests.cs` in `ClinicScheduler.Core.Tests/`
    - Add necessary using statements for FsCheck.Xunit, the Shared project, and entity types
    - Create FsCheck Arbitrary generators for `Appointment` objects with non-null `Notes`, `Patient`, and `Therapist`
    - _Requirements: 1.4, 2.1, 2.2, 2.3, 3.2, 3.3, 4.4, 5.1, 5.2_

  - [x] 2.2 Write property test: Year-grouped ordering preserves descending chronological order
    - **Property 1: Year-grouped ordering preserves descending chronological order**
    - For any list of appointments with non-null notes, verify year groups are in descending order and appointments within each group are ordered by StartTime descending
    - **Validates: Requirements 1.4, 2.1, 2.2, 2.3**

  - [x] 2.3 Write property test: Search filter returns exactly the matching notes
    - **Property 2: Search filter returns exactly the matching notes**
    - For any search text and list of appointments, verify the filtered result contains every appointment where note text, patient full name, or therapist full name contains the search text (case-insensitive), and excludes all others
    - **Validates: Requirements 3.2, 3.3**

  - [x] 2.4 Write property test: Note preview contains all required fields
    - **Property 3: Note preview contains all required fields**
    - For any appointment with non-null note, patient, and therapist, verify that the appointment object exposes full note text, patient full name, therapist full name, and appointment date
    - **Validates: Requirements 4.1**

  - [x] 2.5 Write property test: Preview date format includes day, month, and year
    - **Property 4: Preview date format includes day, month, and year**
    - For any DateTime, verify `FormatPreviewDate` output contains the numeric day, month name or abbreviation, and four-digit year
    - **Validates: Requirements 4.4**

  - [x] 2.6 Write property test: List entry date format matches abbreviated month and day
    - **Property 5: List entry date format matches abbreviated month and day**
    - For any DateTime, verify `FormatListEntryDate` output matches the abbreviated month name followed by the day number
    - **Validates: Requirements 5.1**

  - [x] 2.7 Write property test: Text truncation respects maximum length
    - **Property 6: Text truncation respects maximum length**
    - For any string, verify truncated output never exceeds max length, strings at or under the limit are unchanged, and strings over the limit end with an ellipsis
    - **Validates: Requirements 5.2**

- [x] 3. Checkpoint - Verify helper methods and property tests
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Rewrite Notes.razor to use ClinicDbContext and helper methods
  - [x] 4.1 Replace mock HTML with data-bound Blazor component
    - Inject `ClinicDbContext DbContext` using `@inject` directive (same pattern as PatientDashboard)
    - Add component state fields: `_appointments` (List<Appointment>), `_searchText` (string), `_selectedAppointment` (Appointment?), `_isLoading` (bool)
    - Implement `OnInitializedAsync` to query `DbContext.Appointments` with `.Include(a => a.Patient).Include(a => a.Therapist).Where(a => a.Notes != null).OrderByDescending(a => a.StartTime).ToListAsync()`
    - Add `FilteredNotes` computed property using `NotesPageHelpers.FilterNotes`
    - Add `GroupedByYear` computed property using `NotesPageHelpers.GroupByYearDescending`
    - Add `SelectNote(Appointment)` method to set `_selectedAppointment`
    - Wrap DB call in try/catch, set error flag on failure
    - Set `_isLoading = true` before query, `false` after
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 6.1, 6.2, 6.3, 6.4_

  - [x] 4.2 Implement the note list UI with year grouping
    - Show loading indicator while `_isLoading` is true
    - Show "No clinical notes are available." when `_appointments` is empty and not loading
    - Render year group headings from `GroupedByYear` in descending order
    - Render each note entry with `FormatListEntryDate`, patient full name, and `TruncateNoteText` for note preview
    - Add click handler on each note entry to call `SelectNote`
    - Visually highlight the selected note entry (active CSS class)
    - _Requirements: 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 5.1, 5.2, 5.3, 5.4_

  - [x] 4.3 Implement search box and filter behavior
    - Bind `MudTextField` (or `<input>`) to `_searchText` with immediate update
    - Filter the displayed notes using `FilteredNotes` which calls `NotesPageHelpers.FilterNotes`
    - Show "No notes match the search criteria." when filter returns empty results
    - Clearing the search box restores the full note list
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

  - [x] 4.4 Implement note preview panel
    - Show placeholder message ("Select a note to view details") when no note is selected
    - On note selection, display full note text, patient full name, therapist full name, and formatted appointment date using `FormatPreviewDate`
    - Update preview when a different note is selected
    - Add defensive null checks — display "Unknown Patient" or "Unknown Therapist" if navigation properties are unexpectedly null
    - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [x] 5. Write example-based unit tests for edge cases
  - [x] 5.1 Create test file `NotesPageHelpersTests.cs` in `ClinicScheduler.Core.Tests/`
    - Test empty appointment list returns empty from `FilterNotes`
    - Test empty/null search text returns all appointments from `FilterNotes`
    - Test search matching on note text only, patient name only, therapist name only
    - Test case-insensitive search matching
    - Test `GroupByYearDescending` with appointments spanning multiple years
    - Test `GroupByYearDescending` with single year
    - Test `TruncateNoteText` with text exactly at max length (unchanged)
    - Test `TruncateNoteText` with text one character over max length (truncated with ellipsis)
    - Test `TruncateNoteText` with empty string
    - Test `FormatListEntryDate` produces expected format for known dates
    - Test `FormatPreviewDate` produces expected format for known dates
    - _Requirements: 1.5, 3.2, 3.3, 3.4, 5.1, 5.2_

- [x] 6. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- The static helper approach avoids needing bUnit or Blazor test host infrastructure
- FsCheck.Xunit and xUnit are already configured in the `ClinicScheduler.Core.Tests` project
- The `ClinicScheduler.Core.Tests` project will need a project reference to `ClinicScheduler.Shared` for testing the helper methods
- Property tests validate universal correctness properties; unit tests validate specific examples and edge cases
