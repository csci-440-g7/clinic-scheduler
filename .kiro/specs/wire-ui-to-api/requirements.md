# Requirements Document

## Introduction

The Pain Management Clinic Scheduling System has a complete backend (EF Core + PostgreSQL, 8 REST API controllers, business logic services) and a Blazor Server frontend with 13+ pages. However, three UI components — RequestAppointmentModal, PatientDashboard, and DoctorDashboard — still use an in-memory mock data store (`ClinicDataStore`/`MockClinicData`) instead of the real database services. Additionally, the CalendarView component (embedded in both dashboards) also depends on mock data.

This feature replaces all mock data dependencies in these components with real service injections (`IRepository<T>`, `ClinicDbContext`, `AuthenticationStateProvider`), following the same patterns already established by the working pages (Home.razor, Schedule.razor, Patients.razor, etc.). Once complete, the mock data classes are removed from the codebase.

## Glossary

- **Scheduler_App**: The Blazor Server application (ClinicScheduler.Web) that renders all pages using InteractiveServer render mode
- **RequestAppointmentModal**: The Razor component (`RequestAppointmentModal.razor`) that allows patients to submit appointment requests
- **PatientDashboard**: The Razor page (`PatientDashboard.razor`) at `/dashboard/patient` that shows a patient their appointments, profile, and notes
- **DoctorDashboard**: The Razor page (`DoctorDashboard.razor`) at `/dashboard/doctor` that shows a therapist their appointments, patient list, and notes
- **CalendarView**: The Razor component (`CalendarView.razor`) embedded in both dashboards that displays a weekly calendar of appointments
- **ClinicDataStore**: The in-memory mock data service (`ClinicDataStore.cs`) that stores demo patients, appointments, and notes per session
- **MockClinicData**: The static class (`MockClinicData.cs`) that provides hardcoded demo data records
- **ClinicDbContext**: The EF Core database context that provides access to all database tables (Patients, Therapists, Appointments, AppointmentRequests, Notifications, etc.)
- **IRepository**: The generic repository interface (`IRepository<T>`) that provides CRUD operations for entity types
- **AuthenticationStateProvider**: The ASP.NET Core service that provides the current authenticated user's identity and roles
- **AppointmentRequest**: The database entity representing a patient's request for an appointment, with status tracking (Pending, Approved, Denied)
- **Notification**: The database entity representing an alert sent to a user (missed appointment, upcoming appointment, request approved/denied, etc.)
- **SessionState**: The mock authentication service that uses browser localStorage to track login state — to be replaced by real ASP.NET Core Identity authentication

## Requirements

### Requirement 1: Wire RequestAppointmentModal to Real Database Services

**User Story:** As a patient, I want my appointment requests to be saved to the database, so that clinic staff can review and approve them through the existing staff dashboard workflow.

#### Acceptance Criteria

1. WHEN the RequestAppointmentModal is rendered, THE Scheduler_App SHALL load the list of therapists from IRepository<Therapist> instead of MockClinicData.Doctors
2. WHEN a patient submits an appointment request, THE RequestAppointmentModal SHALL create an AppointmentRequest entity in the database via ClinicDbContext instead of adding a mock Appointment to ClinicDataStore
3. WHEN a patient submits an appointment request, THE RequestAppointmentModal SHALL associate the request with the currently authenticated patient by resolving the patient record from AuthenticationStateProvider
4. IF the patient is not authenticated or the patient record cannot be found in the database, THEN THE RequestAppointmentModal SHALL display an error message and prevent submission
5. WHEN a patient submits an appointment request with a preferred therapist, THE RequestAppointmentModal SHALL store the preferred therapist reference on the AppointmentRequest entity
6. WHEN a patient submits an appointment request with a preferred date and time, THE RequestAppointmentModal SHALL store the preferred date and time on the AppointmentRequest entity
7. THE RequestAppointmentModal SHALL retain the existing form fields (therapist selection, preferred date, preferred time, reason for visit) and validation rules (reason is required, end time must be after start time)
8. THE RequestAppointmentModal SHALL have zero references to ClinicDataStore or MockClinicData after the change

### Requirement 2: Wire PatientDashboard to Real Database Services

**User Story:** As a patient, I want my dashboard to show my real appointments, profile, and notes from the database, so that I see accurate and up-to-date information about my care.

#### Acceptance Criteria

1. WHEN the PatientDashboard is loaded, THE Scheduler_App SHALL identify the current patient by querying the Patient table using the authenticated user's email from AuthenticationStateProvider
2. WHEN the PatientDashboard calendar tab is active, THE Scheduler_App SHALL display the patient's real appointments from the Appointments table in ClinicDbContext, including therapist and room details
3. WHEN the PatientDashboard profile tab is active, THE Scheduler_App SHALL display the patient's real profile data (name, phone, email, date of birth) from the Patient entity in the database
4. WHEN a patient saves profile changes on the PatientDashboard, THE Scheduler_App SHALL persist the updated contact information to the Patient entity in the database via ClinicDbContext
5. WHEN the PatientDashboard notes tab is active, THE Scheduler_App SHALL display notes associated with the patient's appointments from the database
6. THE PatientDashboard SHALL use AuthenticationStateProvider for authentication instead of the mock SessionState service
7. THE PatientDashboard SHALL have zero references to ClinicDataStore, MockClinicData, or SessionState after the change
8. IF the authenticated user does not have a corresponding Patient record in the database, THEN THE PatientDashboard SHALL display a message indicating the patient record was not found

### Requirement 3: Wire DoctorDashboard to Real Database Services

**User Story:** As a therapist, I want my dashboard to show my real patient list, appointments, and notes from the database, so that I can manage my daily schedule with accurate information.

#### Acceptance Criteria

1. WHEN the DoctorDashboard is loaded, THE Scheduler_App SHALL identify the current therapist by querying the Therapist table using the authenticated user's email from AuthenticationStateProvider
2. WHEN the DoctorDashboard calendar tab is active, THE Scheduler_App SHALL display the therapist's real appointments from the Appointments table in ClinicDbContext, including patient and room details
3. WHEN the DoctorDashboard patient search tab is active, THE Scheduler_App SHALL search real Patient records from the database using IRepository<Patient> instead of mock patient data
4. WHEN a therapist selects a patient from the search results, THE DoctorDashboard SHALL display the patient's real profile information (name, date of birth, phone, email, notes) from the Patient entity
5. WHEN the DoctorDashboard notes tab is active, THE Scheduler_App SHALL display appointment notes for the therapist's patients from the database
6. THE DoctorDashboard SHALL use AuthenticationStateProvider for authentication instead of the mock SessionState service
7. THE DoctorDashboard SHALL have zero references to ClinicDataStore, MockClinicData, or SessionState after the change
8. IF the authenticated user does not have a corresponding Therapist record in the database, THEN THE DoctorDashboard SHALL display a message indicating the therapist record was not found

### Requirement 4: Wire CalendarView to Real Database Services

**User Story:** As a patient or therapist, I want the calendar embedded in my dashboard to display real appointments from the database, so that my weekly schedule view is accurate.

#### Acceptance Criteria

1. WHEN the CalendarView is rendered for a patient, THE CalendarView SHALL query the Appointments table filtered by the patient's ID and the displayed week range, including Therapist and Room navigation properties
2. WHEN the CalendarView is rendered for a therapist, THE CalendarView SHALL query the Appointments table filtered by the therapist's ID and the displayed week range, including Patient and Room navigation properties
3. WHEN a user navigates to a different week in the CalendarView, THE CalendarView SHALL reload appointments from the database for the new week range
4. WHEN a user selects an appointment in the CalendarView, THE CalendarView SHALL display the real appointment details from the database
5. THE CalendarView SHALL have zero references to ClinicDataStore or MockClinicData after the change
6. THE CalendarView SHALL preserve the existing weekly grid layout, time slot rendering (7 AM to 7 PM), and color-coding by appointment type

### Requirement 5: Remove Mock Data Dependencies

**User Story:** As a developer, I want all mock data classes removed from the codebase, so that there is a single source of truth (the database) and no confusion about which data store is active.

#### Acceptance Criteria

1. WHEN all UI components have been wired to real services, THE Scheduler_App SHALL remove the ClinicDataStore.cs file from the ClinicScheduler.Shared project
2. WHEN all UI components have been wired to real services, THE Scheduler_App SHALL remove the MockClinicData.cs file from the ClinicScheduler.Shared project
3. WHEN ClinicDataStore is removed, THE Scheduler_App SHALL remove the ClinicDataStore service registration from both Program.cs files (ClinicScheduler.Web and ClinicScheduler.Web.Client)
4. WHEN MockClinicData and ClinicDataStore are removed, THE Scheduler_App SHALL compile without errors and all existing pages SHALL continue to function
5. IF any other component still references ClinicDataStore or MockClinicData, THEN THE Scheduler_App SHALL update that component to use real services before removing the mock files

### Requirement 6: Preserve Existing Working Pages

**User Story:** As a clinic staff member, I want the existing working pages (Home, Schedule, Patients, Therapists, etc.) to remain unchanged, so that current functionality is not disrupted.

#### Acceptance Criteria

1. THE Scheduler_App SHALL make zero modifications to Home.razor, Schedule.razor, Patients.razor, Therapists.razor, Rooms.razor, TherapyTypes.razor, TreatmentPlans.razor, PatientProfile.razor, Notifications.razor, Reports.razor, or UserManagement.razor
2. THE Scheduler_App SHALL make zero modifications to the 8 existing API controllers (Appointments, Patients, Therapists, Rooms, Locations, TherapyTypes, TreatmentPlans, Account)
3. THE Scheduler_App SHALL make zero modifications to the database entity classes or the ClinicDbContext schema
4. THE Scheduler_App SHALL make zero modifications to the database migration files
