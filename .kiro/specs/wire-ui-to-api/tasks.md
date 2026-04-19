# Implementation Plan: Wire UI to API

## Overview

Replace all mock data dependencies (`ClinicDataStore`, `MockClinicData`, `SessionState`) in RequestAppointmentModal, PatientDashboard, DoctorDashboard, CalendarView, and sub-components with real database services (`ClinicDbContext`, `IRepository<T>`, `AuthenticationStateProvider`). Then remove the mock data files and their registrations. Components are wired before mock data is removed to ensure the build stays green throughout.

## Tasks

- [x] 1. Wire RequestAppointmentModal to real services
  - [x] 1.1 Replace mock injections with real services in RequestAppointmentModal.razor
    - Remove `@inject ClinicDataStore Data` and `@using ClinicScheduler.Shared.Services`
    - Add `@inject ClinicDbContext DbContext`, `@inject IRepository<Therapist> TherapistRepo`, `@inject AuthenticationStateProvider AuthState`, `@inject ISnackbar Snackbar`
    - Add `@using ClinicScheduler.Infrastructure.Data`, `@using ClinicScheduler.Core.Entities`, `@using ClinicScheduler.Core.Interfaces`, `@using Microsoft.AspNetCore.Components.Authorization`, `@using MudBlazor`
    - Load therapists from `TherapistRepo.GetAllAsync()` in `OnInitializedAsync` and populate the dropdown (replace `MockClinicData.Doctors`)
    - Align form fields to match `AppointmentRequest` entity: therapist selection (optional), preferred date + time (single DateTime), reason/notes (required)
    - Remove the appointment type dropdown and separate start/end time fields that don't map to `AppointmentRequest`
    - On submit: resolve patient via `AuthState` → email → `DbContext.Patients.FirstOrDefaultAsync(p => p.Email == email)`
    - If patient not found or not authenticated, display error and prevent submission
    - Create `AppointmentRequest` entity: `new AppointmentRequest(patient, notes, preferredTherapist)`, call `SetPreferredDateTime()`, persist via `DbContext.AppointmentRequests.Add()` + `SaveChangesAsync()`
    - Show success via `Snackbar.Add("Appointment request submitted", Severity.Success)`
    - Change `PatientId` parameter from `string` to `int` (or remove it since patient is resolved from auth)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8_

  - [x] 1.2 Write property test for appointment request creation round-trip
    - **Property 1: Appointment request creation round-trip**
    - Generate random patient, optional therapist, date/time, and notes text using FsCheck
    - Create an `AppointmentRequest`, persist to an in-memory database, read back
    - Assert `PatientId`, `PreferredTherapistId`, `PreferredDateTime`, and `Notes` all match the input values
    - Add FsCheck and FsCheck.Xunit NuGet packages to `ClinicScheduler.Core.Tests.csproj`
    - **Validates: Requirements 1.2, 1.3, 1.5, 1.6**

- [x] 2. Wire PatientDashboard to real services
  - [x] 2.1 Replace mock injections with real services in PatientDashboard.razor
    - Remove `@inject ClinicScheduler.Shared.Services.SessionState Session`, `@inject ClinicDataStore Data`, and `@using ClinicScheduler.Shared.Services`
    - Add `@inject ClinicDbContext DbContext`, `@inject IRepository<Therapist> TherapistRepo`, `@inject AuthenticationStateProvider AuthState`, `@inject ISnackbar Snackbar`
    - Add `@attribute [Authorize(Roles = "Patient")]` to replace the `SessionState` authentication check
    - In `OnInitializedAsync`: resolve patient via `AuthState` → email → `DbContext.Patients.FirstOrDefaultAsync(p => p.Email == email)`
    - If patient not found, display "Patient record not found" message and disable all tabs
    - Calendar tab: pass `Patient.Id` (int) to CalendarView instead of mock string ID
    - Profile tab: bind form fields to `Patient` entity properties (`FullName`, `Phone`, `Email`, `DateOfBirth`); save via `Patient.UpdateContactInfo()` + `DbContext.SaveChangesAsync()`
    - Notes tab: query `DbContext.Appointments.Include(a => a.Therapist).Where(a => a.PatientId == patientId && a.Notes != null)` to display appointment notes (notes are on the `Appointment` entity, not separate records)
    - Remove sign-out button (handled by app-level auth)
    - Replace all `MockClinicData.Patient` type references with real `Patient` entity
    - Replace all `MockClinicData.Note` type references with `Appointment` entity (notes are `Appointment.Notes`)
    - Update dashboard title to use `Patient.FullName`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8_

  - [x] 2.2 Write property test for patient profile update persistence
    - **Property 2: Patient profile update persistence**
    - Generate random phone and email strings using FsCheck
    - Create a `Patient`, call `UpdateContactInfo(email, phone)`, persist to in-memory database, read back
    - Assert phone and email match the submitted values
    - **Validates: Requirements 2.4**

- [x] 3. Wire DoctorDashboard to real services
  - [x] 3.1 Replace mock injections with real services in DoctorDashboard.razor
    - Remove `@inject ClinicScheduler.Shared.Services.SessionState Session`, `@inject ClinicDataStore Data`, and `@using ClinicScheduler.Shared.Services`
    - Add `@inject ClinicDbContext DbContext`, `@inject IRepository<Patient> PatientRepo`, `@inject IRepository<Therapist> TherapistRepo`, `@inject AuthenticationStateProvider AuthState`, `@inject ISnackbar Snackbar`
    - Add `@attribute [Authorize(Roles = "Therapist")]` to replace the `SessionState` authentication check
    - In `OnInitializedAsync`: resolve therapist via `AuthState` → email → `DbContext.Therapists.FirstOrDefaultAsync(t => t.Email == email)`
    - If therapist not found, display "Therapist record not found" message and disable all tabs
    - Calendar tab: pass `Therapist.Id` (int) to CalendarView instead of mock string ID
    - Patient search tab: query `DbContext.Patients.Where(p => p.FirstName.Contains(term) || p.LastName.Contains(term))` with case-insensitive search, replacing `Data.Patients` mock queries
    - Patient detail modal: display real `Patient` entity properties (`FullName`, `DateOfBirth`, `Phone`, `Email`, `Notes`)
    - Notes tab: query `DbContext.Appointments.Include(a => a.Patient).Where(a => a.TherapistId == therapistId && a.Notes != null)` to display appointment notes
    - Remove sign-out button and note editing (notes are on `Appointment.Notes`, edited via AppointmentDetailModal)
    - Replace all `MockClinicData.Doctor` / `MockClinicData.Patient` / `MockClinicData.Note` type references with real entities
    - Update dashboard title to use `Therapist.FullName`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

  - [x] 3.2 Write property test for patient search filter correctness
    - **Property 3: Patient search filter correctness**
    - Generate random patient names and search terms using FsCheck
    - Apply the same search filter logic used in DoctorDashboard (case-insensitive `Contains` on `FullName`)
    - Assert all returned patients have a `FullName` containing the search term (case-insensitive)
    - Assert no patient whose `FullName` contains the search term is excluded from results
    - **Validates: Requirements 3.3**

- [x] 4. Checkpoint - Verify dashboards and modal compile
  - Ensure the project builds with `dotnet build ClinicScheduler/ClinicScheduler.Web/ClinicScheduler.Web.csproj`
  - Ensure all tests pass with `dotnet test ClinicScheduler/ClinicScheduler.slnx`
  - Ask the user if questions arise.

- [x] 5. Wire CalendarView and sub-components to real services
  - [x] 5.1 Replace mock injections with real services in CalendarView.razor
    - Remove `@inject ClinicDataStore Data` and `@using ClinicScheduler.Shared.Services`
    - Add `@inject ClinicDbContext DbContext` and necessary `@using` directives for `ClinicScheduler.Core.Entities`, `ClinicScheduler.Infrastructure.Data`, `Microsoft.EntityFrameworkCore`
    - Change parameters from `string? PatientId` / `string? DoctorId` to `int? PatientId` / `int? TherapistId`
    - Replace `ForViewer()` method: query `DbContext.Appointments.Include(a => a.Patient).Include(a => a.Therapist).Include(a => a.Room).Where(...)` filtered by week range and viewer context (patient ID or therapist ID)
    - Replace all `MockClinicData.Appointment` type references with real `Appointment` entity
    - Update `Subtitle()` to use navigation properties (`a.Patient.FullName`, `a.Therapist.FullName`) instead of mock lookups
    - Update `ApptCss()` to use `Appointment.TherapyType` or `AppointmentStatus` instead of mock string type
    - Update `ApptStyle()` to use `Appointment.StartTime` / `Appointment.EndTime` instead of mock `Start` / `End`
    - Preserve the existing weekly grid layout, time slot rendering (7 AM to 7 PM), and color-coding
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

  - [x] 5.2 Replace mock injections with real services in CalendarSidebar.razor
    - Remove `@inject ClinicDataStore Data` and `@using ClinicScheduler.Shared.Services`
    - Add `@inject ClinicDbContext DbContext` and necessary `@using` directives
    - Change parameters from `string? PatientId` / `string? DoctorId` to `int? PatientId` / `int? TherapistId`, and `string? SelectedId` to `int? SelectedId`
    - Replace `ForViewer()` method with async DB queries on `DbContext.Appointments`
    - Replace `PendingRequests` to query `DbContext.AppointmentRequests.Include(r => r.Patient).Include(r => r.PreferredTherapist).Where(r => r.Status == AppointmentRequestStatus.Pending)` for staff view
    - Update Confirm/Decline actions to work with `AppointmentRequest` entities (approve/deny) instead of mock status strings
    - Replace all `MockClinicData.Appointment` type references with real `Appointment` entity
    - Update `Subtitle()` to use navigation properties
    - Update `OnAppointmentSelected` callback parameter type from `MockClinicData.Appointment` to `Appointment`
    - _Requirements: 4.1, 4.2, 4.3, 4.5_

  - [x] 5.3 Replace mock injections with real services in BookAppointmentModal.razor
    - Remove `@inject ClinicDataStore Data` and `@using ClinicScheduler.Shared.Services`
    - Add `@inject ClinicDbContext DbContext`, `@inject IRepository<Patient> PatientRepo`, `@inject IRepository<Therapist> TherapistRepo`, `@inject IRepository<Room> RoomRepo`, `@inject AppointmentSchedulingService SchedulingService`, `@inject ISnackbar Snackbar`
    - Load patients, therapists, and rooms from repositories in `OnInitializedAsync`
    - Replace free-text location/room fields with a `Room` dropdown populated from `RoomRepo.GetAllAsync()`
    - On submit: use `SchedulingService.CreateAppointmentAsync(patientId, therapistId, roomId, startTime, duration)` instead of `Data.AddAppointment()`
    - Handle `InvalidOperationException` from scheduling service (conflicts) via `Snackbar`
    - Replace all `MockClinicData.Doctors` / `Data.Patients` references with real entity lists
    - _Requirements: 4.1, 4.2, 4.5_

  - [x] 5.4 Replace mock injections with real services in AppointmentDetailModal.razor
    - Remove `@inject ClinicDataStore Data` and `@using ClinicScheduler.Shared.Services`
    - Add `@inject ClinicDbContext DbContext` and necessary `@using` directives
    - Change `Appointment` parameter type from `MockClinicData.Appointment` to real `Appointment` entity
    - Replace `Data.GetPatient()` / `MockClinicData.Doctors.FirstOrDefault()` lookups with navigation properties (`Appointment.Patient.FullName`, `Appointment.Therapist.FullName`)
    - Update doctor notes save to use `Appointment.Notes` property + `DbContext.SaveChangesAsync()`
    - Remove staff confirm/decline actions from this modal (the real approval flow is on Home.razor's staff dashboard)
    - Use `Appointment.Room.Name` for room display instead of mock `RoomNumber`
    - _Requirements: 4.4, 4.5_

  - [x] 5.5 Write property test for calendar appointment filtering
    - **Property 4: Calendar appointment filtering by owner and week range**
    - Generate random appointments with varying patient/therapist IDs and dates using FsCheck
    - Apply the calendar filter logic (filter by owner ID and 7-day week range)
    - Assert all returned appointments belong to the specified owner and have `StartTime` within the week range
    - Assert no appointment matching both criteria is excluded
    - **Validates: Requirements 4.1, 4.2**

- [x] 6. Checkpoint - Verify all components compile and tests pass
  - Ensure the project builds with `dotnet build ClinicScheduler/ClinicScheduler.Web/ClinicScheduler.Web.csproj`
  - Ensure all tests pass with `dotnet test ClinicScheduler/ClinicScheduler.slnx`
  - Ask the user if questions arise.

- [x] 7. Remove mock data files and registrations
  - [x] 7.1 Delete mock data files and remove service registrations
    - Delete `ClinicScheduler/ClinicScheduler.Shared/Services/ClinicDataStore.cs`
    - Delete `ClinicScheduler/ClinicScheduler.Shared/Services/MockClinicData.cs`
    - Remove `builder.Services.AddScoped<ClinicDataStore>()` from `ClinicScheduler.Web/Program.cs`
    - Remove `builder.Services.AddScoped<ClinicDataStore>()` from `ClinicScheduler.Web.Client/Program.cs`
    - Remove `builder.Services.AddScoped<SessionState>()` from both `Program.cs` files (if no other components still reference it)
    - Remove `using ClinicScheduler.Shared.Services` where it was only needed for mock types
    - Verify no remaining references to `ClinicDataStore`, `MockClinicData`, or `SessionState` in any wired component
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

  - [x] 7.2 Write integration tests to verify no mock references remain
    - Verify the project compiles with zero errors after mock removal
    - Run a grep/search to confirm zero references to `ClinicDataStore` or `MockClinicData` in the codebase
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [x] 8. Final checkpoint - Verify clean build and all tests pass
  - Ensure the project builds with `dotnet build ClinicScheduler/ClinicScheduler.Web/ClinicScheduler.Web.csproj`
  - Ensure all existing tests pass with `dotnet test ClinicScheduler/ClinicScheduler.slnx`
  - Verify zero modifications to existing working pages (Home.razor, Schedule.razor, Patients.razor, etc.), API controllers, entity classes, or migration files
  - Ask the user if questions arise.
  - _Requirements: 6.1, 6.2, 6.3, 6.4_

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Components are wired (tasks 1–5) before mock data is removed (task 7) to keep the build green throughout
- Each task references specific requirements for traceability
- Property tests use FsCheck with xUnit integration and validate the 4 correctness properties from the design document
- Notes in the real schema are stored as `Appointment.Notes` (string property), not as separate entities
- All entity IDs are `int`, not `string` — parameter types change accordingly
- The `SessionState` mock auth service is replaced by `AuthenticationStateProvider` + `[Authorize]` attributes
