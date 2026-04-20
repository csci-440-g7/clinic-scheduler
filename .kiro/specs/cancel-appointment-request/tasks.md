# Implementation Plan: Cancel Appointment Request

## Overview

Implement a request-based cancellation workflow for appointments. Patients submit a cancellation request with a reason, staff review and approve or deny it, and notifications are sent at each stage. The implementation follows the existing `AppointmentRequest` pattern and reuses `AppointmentRequestStatus`, `Notification`, and established UI modal/sidebar patterns.

## Tasks

- [x] 1. Create CancelAppointmentRequest entity and update data model
  - [x] 1.1 Create `CancelAppointmentRequest` entity class in `ClinicScheduler.Core/Entities/CancelAppointmentRequest.cs`
    - Follow the `AppointmentRequest` pattern with private setters and private EF constructor
    - Properties: `Id`, `PatientId`, `Patient`, `AppointmentId`, `Appointment`, `Reason`, `Status` (AppointmentRequestStatus, default Pending), `CreatedAt`, `DenialReason`
    - Domain constructor accepting `Patient`, `Appointment`, and `string reason` — validate reason is not whitespace, validate appointment status is Scheduled or Rescheduled
    - `Approve()` method: sets Status to Approved, calls `Appointment.Cancel()`
    - `Deny(string reason)` method: validates reason is not whitespace, sets Status to Denied, stores DenialReason
    - _Requirements: 6.1, 6.2, 6.3, 5.3_

  - [x] 1.2 Add `CancellationRequested`, `CancellationApproved`, `CancellationDenied` values to the `NotificationType` enum in `ClinicScheduler.Core/Entities/Notification.cs`
    - _Requirements: 2.3_

  - [x] 1.3 Add `DbSet<CancelAppointmentRequest> CancelAppointmentRequests` to `ClinicDbContext` in `ClinicScheduler.Infrastructure/Data/ClinicDbContext.cs`
    - _Requirements: 6.4_

  - [x] 1.4 Create EF Core migration for the `CancelAppointmentRequests` table
    - Foreign keys to `Patients` and `Appointments`
    - _Requirements: 6.1, 6.4_

- [ ] 2. Property-based tests for CancelAppointmentRequest entity
  - [ ]* 2.1 Write property test: Cancel request creation preserves all input fields
    - **Property 1: Cancel request creation preserves all input fields**
    - For any valid patient, eligible appointment, and non-whitespace reason, the created entity has status Pending, correct PatientId, correct AppointmentId, the provided reason, and a CreatedAt timestamp
    - **Validates: Requirements 1.5, 6.1**

  - [ ]* 2.2 Write property test: Whitespace-only reasons are rejected
    - **Property 2: Whitespace-only reasons are rejected**
    - For any string composed entirely of whitespace (including empty), creating a cancel request or denying with that reason throws an exception and no state change occurs
    - **Validates: Requirements 1.6, 4.1**

  - [ ]* 2.3 Write property test: Approval transitions both request and appointment
    - **Property 3: Approval transitions both request and appointment**
    - For any pending CancelAppointmentRequest linked to a Scheduled or Rescheduled appointment, approving sets request status to Approved and appointment status to Canceled
    - **Validates: Requirements 3.2, 3.3**

  - [ ]* 2.4 Write property test: Denial stores reason and preserves appointment status
    - **Property 4: Denial stores reason and preserves appointment status**
    - For any pending CancelAppointmentRequest and valid non-whitespace denial reason, denying sets request status to Denied, stores the denial reason, and leaves appointment status unchanged
    - **Validates: Requirements 4.2, 4.4**

  - [ ]* 2.5 Write property test: Ineligible appointments are rejected
    - **Property 9: Ineligible appointments are rejected**
    - For any appointment with status Completed, Canceled, or Missed, attempting to create a cancel request throws an exception
    - **Validates: Requirements 5.3**

- [x] 3. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 4. Create API controller and contracts
  - [x] 4.1 Create request/response DTOs in `ClinicScheduler.Web/Contracts/CancelAppointmentRequests/`
    - `CreateCancelAppointmentRequestDto` with `AppointmentId` (int) and `Reason` (string)
    - `CancelAppointmentRequestDto` with all entity fields mapped for API responses
    - _Requirements: 7.1_

  - [x] 4.2 Create `CancelAppointmentRequestsController` in `ClinicScheduler.Web/Api/CancelAppointmentRequestsController.cs`
    - `POST api/cancel-appointment-requests`: validate appointment exists (404), validate appointment status is Scheduled/Rescheduled (409), validate no existing pending cancel request for the appointment (409), create entity and return 201
    - `GET api/cancel-appointment-requests`: list all cancel requests for staff
    - `GET api/cancel-appointment-requests/{id}`: get a single cancel request
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

  - [ ]* 4.3 Write property tests for notification fan-out and content
    - **Property 5: Staff notification fan-out on creation**
    - For any set of staff users and any newly created cancel request, the system creates exactly one CancellationRequested notification per staff user
    - **Validates: Requirements 2.1**
    - **Property 6: Notification messages contain required details**
    - For any patient name, appointment date, and time, the CancellationRequested notification message contains the patient name, formatted date, and formatted time
    - **Validates: Requirements 2.2**

  - [ ]* 4.4 Write property tests for approval and denial notifications
    - **Property 7: Approval creates patient notification**
    - For any approved cancel request, the system creates exactly one CancellationApproved notification for the patient's user account
    - **Validates: Requirements 3.4**
    - **Property 8: Denial creates patient notification with reason**
    - For any denied cancel request with a denial reason, the system creates exactly one CancellationDenied notification for the patient's user account, and the message contains the denial reason
    - **Validates: Requirements 4.3**

- [x] 5. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Implement UI components
  - [x] 6.1 Create `CancelRequestModal.razor` in `ClinicScheduler.Shared/Pages/`
    - Follow the `RequestAppointmentModal` pattern: backdrop, panel, header with close button
    - Display read-only appointment context (date, time, therapist name)
    - Required reason textarea with validation error on empty/whitespace submission
    - On submit: look up patient from auth state, create `CancelAppointmentRequest`, create `CancellationRequested` notifications for all staff users, show success snackbar, invoke `OnRequested` callback
    - Parameters: `Appointment`, `OnClose` (EventCallback), `OnRequested` (EventCallback)
    - _Requirements: 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 2.1, 2.2_

  - [x] 6.2 Modify `AppointmentDetailModal.razor` to add cancellation request support
    - For `ViewerRole == "patient"` and appointment status is Scheduled or Rescheduled:
      - If no pending cancel request exists for this appointment, show "Request Cancellation" button
      - If a pending cancel request exists, show pending status indicator instead of the button
    - Clicking "Request Cancellation" opens the `CancelRequestModal`
    - Hide the button for Completed, Canceled, or Missed appointments
    - _Requirements: 1.1, 1.8, 5.1, 5.2_

  - [x] 6.3 Modify `CalendarSidebar.razor` to add "Pending Cancellations" section for staff
    - Mirror the existing "Pending Requests" section pattern
    - Query `CancelAppointmentRequests` with status Pending, include Patient and Appointment (with Therapist)
    - Display patient name, appointment date/time, therapist name, and cancellation reason
    - Add "Approve" button: call `Approve()` on the entity, set appointment to Canceled, create `CancellationApproved` notification for patient, show success snackbar
    - Add "Deny" button: prompt for denial reason (inline input or simple prompt), call `Deny(reason)`, create `CancellationDenied` notification for patient with denial reason, show info snackbar
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4_

  - [x] 6.4 Update `Notifications.razor` to handle new notification types
    - Add `CancellationRequested`, `CancellationApproved`, `CancellationDenied` cases to the `TypeIcon` switch expression
    - Add corresponding cases to the `TypeColor` switch expression
    - _Requirements: 2.3_

- [x] 7. Wire CalendarView to support CancelRequestModal
  - Update `CalendarView.razor` to pass the new cancellation-related callbacks through to `AppointmentDetailModal` if needed, ensuring the modal can trigger data refresh after a cancel request is submitted
  - _Requirements: 1.1, 1.2_

- [x] 8. Final checkpoint
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests use FsCheck (already in the test project) and validate universal correctness properties from the design document
- The implementation follows existing patterns: `AppointmentRequest` for the entity, `RequestAppointmentModal` for the modal, `CalendarSidebar` pending requests section for the staff review UI
