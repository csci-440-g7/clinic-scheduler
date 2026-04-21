# Implementation Plan: Appointment Notifications

## Overview

Extend the existing notification infrastructure so that every staff-initiated appointment action (create, reschedule, cancel, update) immediately generates an in-app notification for the patient. The implementation adds two new `NotificationType` enum values, creates an `AppointmentNotificationService` in the Web project, integrates it into `AppointmentsController`, upgrades `Notifications.razor` with type-specific icons/colors and actionable buttons, and adds a "Notifications" nav link with unread badge to `MainLayout.razor` for all roles. Property-based tests validate the six correctness properties from the design using FsCheck and an in-memory database.

## Tasks

- [x] 1. Extend NotificationType enum with new values
  - Append `AppointmentCreated` (ordinal 9) and `AppointmentUpdated` (ordinal 10) to the `NotificationType` enum in `ClinicScheduler.Core/Entities/Notification.cs`
  - Existing values must remain unchanged to preserve backward compatibility with stored integer values
  - _Requirements: 5.1, 5.2, 5.3_

- [x] 2. Create AppointmentNotificationService and register in DI
  - [x] 2.1 Create `AppointmentNotificationService.cs` in `ClinicScheduler.Web/Services/`
    - Inject `ClinicDbContext` and `ILogger<AppointmentNotificationService>` via constructor
    - Implement `NotifyAppointmentCreatedAsync(Appointment, CancellationToken)` — look up AppUser by `Patient.Email`, create a Notification of type `AppointmentCreated` with therapist name, date, and time in the message, set `RelatedAppointmentId` to the appointment Id
    - Implement `NotifyAppointmentRescheduledAsync(Appointment, DateTime originalStartTime, DateTime originalEndTime, CancellationToken)` — create a Notification of type `AppointmentRescheduled` with both old and new date/time in the message
    - Implement `NotifyAppointmentCancelledAsync(Appointment, CancellationToken)` — create a Notification of type `CancellationApproved` with therapist name and original date/time
    - Implement `NotifyAppointmentUpdatedAsync(Appointment, string changeDescription, CancellationToken)` — create a Notification of type `AppointmentUpdated` with the change description in the message; use fallback message if description is null/empty
    - Each method: load Patient/Therapist via Include if not loaded, query `_db.Users.FirstOrDefaultAsync(u => u.UserName == appointment.Patient.Email)`, return silently if no user found, wrap entire body in try/catch logging errors without throwing
    - Follow the same user lookup pattern as `AppointmentReminderService`
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 7.1, 7.2, 8.1, 8.2, 8.3, 8.4_

  - [x] 2.2 Register `AppointmentNotificationService` as a scoped service in `ClinicScheduler.Web/Program.cs`
    - Add `builder.Services.AddScoped<AppointmentNotificationService>()` alongside the existing `AppointmentSchedulingService` and `MissedAppointmentService` registrations
    - _Requirements: 7.3_

- [x] 3. Integrate notification calls into AppointmentsController
  - [x] 3.1 Inject `AppointmentNotificationService` into `AppointmentsController` constructor
    - Add the service as a constructor parameter and store as a private field
    - _Requirements: 1.1, 2.1, 3.1, 4.1_

  - [x] 3.2 Add notification call to the Create action
    - After the successful `SaveChangesAsync` and re-query of the created appointment, call `NotifyAppointmentCreatedAsync` with the fully-loaded appointment entity
    - _Requirements: 1.1, 1.3, 1.4_

  - [x] 3.3 Add notification calls to the Update action
    - Capture original `StartTime` and `EndTime` before applying changes
    - If `StartTime` changed (reschedule): call `NotifyAppointmentRescheduledAsync` with original and new times
    - If `Status` changed to `Canceled`: call `NotifyAppointmentCancelledAsync`
    - If other details changed without time change: build a change description string and call `NotifyAppointmentUpdatedAsync`
    - _Requirements: 2.1, 2.2, 3.1, 3.2, 4.1, 4.2_

- [x] 4. Add project reference and write property-based tests for AppointmentNotificationService
  - [x] 4.1 Add a project reference from `ClinicScheduler.Core.Tests` to `ClinicScheduler.Web`
    - Add `<ProjectReference Include="..\ClinicScheduler.Web\ClinicScheduler.Web.csproj" />` to the test project's csproj
    - This enables testing `AppointmentNotificationService` directly with an in-memory `ClinicDbContext`
    - _Requirements: 1.1, 2.1, 3.1, 4.1_

  - [x] 4.2 Write property test: Creation notification produces correct type and message content
    - **Property 1: Creation notification produces correct type and message content**
    - Create `AppointmentNotificationPropertyTests.cs` in `ClinicScheduler.Core.Tests/`
    - Set up FsCheck Arbitrary generators for valid appointment data (patient names, therapist names, dates, room names) with a matching AppUser in the in-memory database
    - For any valid appointment with a resolvable patient AppUser, verify `NotifyAppointmentCreatedAsync` produces exactly one Notification of type `AppointmentCreated` whose message contains the therapist full name, appointment date, and appointment time
    - Minimum 100 iterations
    - **Validates: Requirements 1.1, 1.3**

  - [x] 4.3 Write property test: Reschedule notification includes both old and new times
    - **Property 2: Reschedule notification includes both old and new times**
    - For any valid appointment reschedule with a resolvable patient AppUser, verify `NotifyAppointmentRescheduledAsync` produces exactly one Notification of type `AppointmentRescheduled` whose message contains both the original date/time and the new date/time
    - Minimum 100 iterations
    - **Validates: Requirements 2.1, 2.2**

  - [x] 4.4 Write property test: Cancellation notification produces correct type and message content
    - **Property 3: Cancellation notification produces correct type and message content**
    - For any valid appointment cancellation with a resolvable patient AppUser, verify `NotifyAppointmentCancelledAsync` produces exactly one Notification of type `CancellationApproved` whose message contains the therapist full name and original appointment date and time
    - Minimum 100 iterations
    - **Validates: Requirements 3.1, 3.2**

  - [x] 4.5 Write property test: Update notification describes the change
    - **Property 4: Update notification describes the change**
    - For any valid appointment detail update with a resolvable patient AppUser, verify `NotifyAppointmentUpdatedAsync` produces exactly one Notification of type `AppointmentUpdated` whose message contains the provided change description
    - Minimum 100 iterations
    - **Validates: Requirements 4.1, 4.2**

  - [x] 4.6 Write property test: RelatedAppointmentId invariant
    - **Property 5: RelatedAppointmentId invariant**
    - For any appointment notification event (created, rescheduled, cancelled, or updated) where a notification is produced, verify the notification's `RelatedAppointmentId` equals the source appointment's `Id`
    - Parameterize across all four event types
    - Minimum 100 iterations
    - **Validates: Requirements 1.4, 2.3, 3.3, 4.3**

  - [x] 4.7 Write property test: No-user graceful skip
    - **Property 6: No-user graceful skip**
    - For any appointment notification event where the patient's email does not match any AppUser's UserName, verify the service produces zero notifications and does not throw an exception
    - Parameterize across all four event types
    - Minimum 100 iterations
    - **Validates: Requirements 1.5, 2.4, 3.4, 4.4**

- [x] 5. Checkpoint - Verify service and property tests
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Update Notifications.razor with new type icons/colors and actionable buttons
  - [x] 6.1 Add icon and color mappings for new notification types
    - Add `AppointmentCreated` → `Icons.Material.Filled.EventAvailable` / `Color.Success` to the `TypeIcon` and `TypeColor` switch expressions
    - Add `AppointmentUpdated` → `Icons.Material.Filled.Edit` / `Color.Info` to the `TypeIcon` and `TypeColor` switch expressions
    - _Requirements: 6.5_

  - [x] 6.2 Replace generic "View Schedule" link with type-specific action buttons
    - `AppointmentCreated` → "View Appointment" button navigating to `/schedule`
    - `AppointmentRescheduled` → "View Appointment" button navigating to `/schedule`
    - `AppointmentUpdated` → "View Appointment" button navigating to `/schedule`
    - `CancellationApproved` → "Request New Appointment" button navigating to `/appointment-request`
    - All other types with `RelatedAppointmentId` → "View Schedule" link to `/schedule` (existing behavior)
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

- [x] 7. Update MainLayout.razor with Notifications nav link and unread badge
  - [x] 7.1 Add "Notifications" NavLink to all role sections
    - Add a "Notifications" `NavLink` pointing to `/notifications` inside each role's `AuthorizeView` block (Patient, Therapist, Staff)
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [x] 7.2 Implement unread notification badge
    - Inject `ClinicDbContext` and `AuthenticationStateProvider` into `MainLayout.razor`
    - Query unread notification count on layout initialization: `_unreadCount = await DbContext.Notifications.CountAsync(n => n.UserId == _userId && !n.IsRead)`
    - Display a badge (e.g., `<span class="badge">`) next to the Notifications link when `_unreadCount > 0`
    - Add CSS styling for the badge indicator
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

- [x] 8. Write example-based unit tests
  - [x] 8.1 Create `AppointmentNotificationTests.cs` in `ClinicScheduler.Core.Tests/`
    - Test enum backward compatibility: verify all 9 existing `NotificationType` values are present with unchanged ordinal values, plus the 2 new values at ordinals 9 and 10
    - Test notification defaults: verify `IsRead` is `false` and `CreatedAt` is approximately `DateTime.UtcNow` after creation
    - Test database error resilience: mock a `SaveChangesAsync` failure and verify the service logs the error without throwing
    - Test null/empty change description fallback: verify `NotifyAppointmentUpdatedAsync` uses a fallback message when change description is null or empty
    - _Requirements: 5.1, 5.2, 5.3, 8.1, 8.2, 8.3, 8.4_

- [x] 9. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- The `ClinicScheduler.Core.Tests` project already has FsCheck 3.3.2, xunit, FluentAssertions, Moq, and Microsoft.EntityFrameworkCore.InMemory configured
- The test project already references ClinicScheduler.Core, ClinicScheduler.Infrastructure, and ClinicScheduler.Shared; a reference to ClinicScheduler.Web is added in task 4.1 to enable direct testing of the notification service
- Property tests validate universal correctness properties; unit tests validate specific examples and edge cases
- The notification service follows the same fire-and-forget-on-error pattern as `AppointmentReminderService` — notification failures are logged but never roll back the appointment operation
