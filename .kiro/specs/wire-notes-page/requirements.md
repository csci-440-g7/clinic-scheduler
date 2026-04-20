# Requirements Document

## Introduction

The Notes page (`/notes`) currently displays hardcoded mock data — fake clinical notes, a non-functional search box, and a static note preview with a placeholder patient name. This feature replaces all mock data with real database queries so the page loads actual appointment notes, supports search and filtering, displays a proper note preview on selection, and groups notes by year.

The Appointment entity already has a `Notes` text field, and the existing PatientDashboard page demonstrates the pattern for querying appointment notes with EF Core. This feature extends that pattern to the standalone Notes page, making it a useful clinical notes browser for all users.

## Glossary

- **Notes_Page**: The Blazor page at `/notes` that displays clinical notes from appointments.
- **Appointment**: A scheduled therapy session that may contain a clinical note in its Notes field.
- **Patient**: A registered patient in the clinic system, linked to appointments.
- **Therapist**: A clinician who authors notes on appointments.
- **Note_Entry**: A single appointment record that has a non-null Notes field, displayed as an item in the notes list.
- **Note_List**: The scrollable list of Note_Entry items shown on the Notes_Page, grouped by year.
- **Note_Preview**: The panel on the Notes_Page that displays the full details of a selected Note_Entry.
- **Search_Box**: The text input on the Notes_Page used to filter notes by search text.
- **Year_Group**: A visual grouping of Note_Entry items that share the same appointment year.
- **ClinicDbContext**: The Entity Framework Core database context used for data access.

## Requirements

### Requirement 1: Load Notes from Database

**User Story:** As a clinic user, I want the Notes page to display actual clinical notes from the database, so that I can review real appointment notes instead of placeholder data.

#### Acceptance Criteria

1. WHEN the Notes_Page is loaded, THE Notes_Page SHALL query the ClinicDbContext for all Appointments that have a non-null Notes field.
2. WHEN the Notes_Page queries Appointments, THE Notes_Page SHALL include the related Patient and Therapist entities in the query.
3. THE Notes_Page SHALL display each Note_Entry with the appointment date and a truncated preview of the note text.
4. THE Notes_Page SHALL order Note_Entry items by appointment start time in descending order (most recent first).
5. IF no Appointments with notes exist in the database, THEN THE Notes_Page SHALL display a message indicating that no clinical notes are available.

### Requirement 2: Group Notes by Year

**User Story:** As a clinic user, I want notes grouped by year, so that I can quickly locate notes from a specific time period.

#### Acceptance Criteria

1. THE Notes_Page SHALL group Note_Entry items into Year_Group sections based on the appointment start time year.
2. THE Notes_Page SHALL display Year_Group headings in descending order (most recent year first).
3. WITHIN each Year_Group, THE Notes_Page SHALL order Note_Entry items by appointment start time in descending order.

### Requirement 3: Search and Filter Notes

**User Story:** As a clinic user, I want to search notes by text, patient name, or therapist name, so that I can quickly find relevant clinical notes.

#### Acceptance Criteria

1. THE Search_Box SHALL accept free-text input from the user.
2. WHEN the user enters text in the Search_Box, THE Notes_Page SHALL filter Note_Entry items to those where the note text, Patient full name, or Therapist full name contains the search text (case-insensitive).
3. WHEN the Search_Box is cleared, THE Notes_Page SHALL display all Note_Entry items.
4. WHEN the search filter produces no matching Note_Entry items, THE Notes_Page SHALL display a message indicating that no notes match the search criteria.
5. THE Notes_Page SHALL apply the search filter on the client side against the already-loaded notes without making additional database queries.

### Requirement 4: Note Preview on Selection

**User Story:** As a clinic user, I want to select a note and see its full details in a preview panel, so that I can read the complete note content without navigating away.

#### Acceptance Criteria

1. WHEN the user clicks a Note_Entry in the Note_List, THE Note_Preview SHALL display the full note text, patient full name, therapist full name, and appointment date.
2. WHEN the Notes_Page is first loaded, THE Note_Preview SHALL display a placeholder message prompting the user to select a note.
3. WHEN a different Note_Entry is selected, THE Note_Preview SHALL update to display the newly selected note details.
4. THE Note_Preview SHALL display the appointment date in a human-readable format including the day, month, and year.

### Requirement 5: Note Entry Display Format

**User Story:** As a clinic user, I want each note in the list to show enough context to identify it, so that I can find the right note without opening each one.

#### Acceptance Criteria

1. THE Note_Entry SHALL display the appointment date formatted as abbreviated month and day (e.g., "Mar 1").
2. THE Note_Entry SHALL display a truncated preview of the note text, limited to a reasonable length to prevent layout overflow.
3. THE Note_Entry SHALL display the Patient full name alongside the date.
4. WHEN a Note_Entry is selected, THE Note_Entry SHALL be visually highlighted to indicate the active selection.

### Requirement 6: Data Access Pattern Consistency

**User Story:** As a developer, I want the Notes page to follow the same data access patterns as other pages in the application, so that the codebase remains consistent and maintainable.

#### Acceptance Criteria

1. THE Notes_Page SHALL inject ClinicDbContext using the same pattern as the PatientDashboard page.
2. THE Notes_Page SHALL use Entity Framework Core Include methods to eagerly load related Patient and Therapist data.
3. THE Notes_Page SHALL perform data loading in the OnInitializedAsync lifecycle method.
4. THE Notes_Page SHALL handle the loading state by displaying appropriate feedback while data is being fetched.
