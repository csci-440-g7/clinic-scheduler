# Requirements Document

## Introduction

This feature allows patients to submit a cancellation request for a scheduled appointment. Rather than canceling directly, the patient creates a request that clinic staff can review and either approve or deny. This follows the same request-based workflow already established for appointment booking requests. Staff members receive a notification when a new cancellation request is submitted, and patients receive a notification when their request is approved or denied.

## Glossary

- **Cancel_Request**: A record representing a patient's request to cancel a specific scheduled appointment. It has a status lifecycle of Pending, Approved, or Denied.
- **Patient**: A registered patient in the clinic system who can view their appointments and submit requests.
- **Staff_User**: A user with the Admin, ClinicManager, or Staff role who can review and act on cancellation requests.
- **Appointment**: A scheduled therapy session between a patient and a therapist.
- **Notification**: An in-app message delivered to a specific user, displayed on the Notifications page.
- **Appointment_Detail_Modal**: The modal dialog that displays appointment details, accessible from the calendar view.
- **Cancel_Request_Modal**: A modal dialog where the patient provides a reason for requesting cancellation.
- **Notifications_Page**: The page that lists all notifications for the authenticated user.

## Requirements

### Requirement 1: Submit a Cancellation Request

**User Story:** As a patient, I want to request cancellation of a scheduled appointment, so that clinic staff can review and process my cancellation.

#### Acceptance Criteria

1. WHEN a Patient views a scheduled Appointment in the Appointment_Detail_Modal, THE Appointment_Detail_Modal SHALL display a "Request Cancellation" button.
2. WHEN the Patient clicks the "Request Cancellation" button, THE System SHALL open the Cancel_Request_Modal.
3. THE Cancel_Request_Modal SHALL display the appointment date, time, and therapist name as read-only context.
4. THE Cancel_Request_Modal SHALL provide a required text field for the Patient to enter a cancellation reason.
5. WHEN the Patient submits the Cancel_Request_Modal with a valid reason, THE System SHALL create a Cancel_Request with status Pending, linked to the Appointment and the Patient.
6. WHEN the Patient submits the Cancel_Request_Modal with an empty reason, THE Cancel_Request_Modal SHALL display a validation error message indicating that a reason is required.
7. WHEN a Cancel_Request is successfully created, THE System SHALL display a confirmation message to the Patient.
8. WHILE an Appointment already has a Pending Cancel_Request, THE Appointment_Detail_Modal SHALL display the pending request status instead of the "Request Cancellation" button.

### Requirement 2: Staff Notification on Cancellation Request

**User Story:** As a staff member, I want to receive a notification when a patient submits a cancellation request, so that I can review and act on it promptly.

#### Acceptance Criteria

1. WHEN a Cancel_Request is created, THE System SHALL create a Notification of type CancellationRequested for each Staff_User.
2. THE Notification SHALL include the patient name, appointment date, and appointment time in the message.
3. WHEN a Staff_User views the Notifications_Page, THE Notifications_Page SHALL display CancellationRequested notifications with a distinct icon and color.

### Requirement 3: Review and Approve a Cancellation Request

**User Story:** As a staff member, I want to approve a cancellation request, so that the appointment is canceled as the patient requested.

#### Acceptance Criteria

1. WHEN a Staff_User views pending Cancel_Requests, THE System SHALL display the patient name, appointment details, and the cancellation reason.
2. WHEN a Staff_User approves a Cancel_Request, THE System SHALL update the Cancel_Request status to Approved.
3. WHEN a Cancel_Request is approved, THE System SHALL transition the linked Appointment status to Canceled.
4. WHEN a Cancel_Request is approved, THE System SHALL create a Notification for the Patient indicating that the cancellation request was approved.

### Requirement 4: Review and Deny a Cancellation Request

**User Story:** As a staff member, I want to deny a cancellation request, so that the appointment remains scheduled when cancellation is not appropriate.

#### Acceptance Criteria

1. WHEN a Staff_User denies a Cancel_Request, THE System SHALL require the Staff_User to provide a denial reason.
2. WHEN a Staff_User denies a Cancel_Request with a valid denial reason, THE System SHALL update the Cancel_Request status to Denied and store the denial reason.
3. WHEN a Cancel_Request is denied, THE System SHALL create a Notification for the Patient indicating that the cancellation request was denied, including the denial reason.
4. WHEN a Cancel_Request is denied, THE Appointment SHALL remain in its current status.

### Requirement 5: Cancellation Request Eligibility

**User Story:** As a patient, I want the system to only allow cancellation requests for eligible appointments, so that I do not submit requests for appointments that cannot be canceled.

#### Acceptance Criteria

1. THE Appointment_Detail_Modal SHALL display the "Request Cancellation" button only for Appointments with status Scheduled or Rescheduled.
2. WHILE an Appointment has status Completed, Canceled, or Missed, THE Appointment_Detail_Modal SHALL hide the "Request Cancellation" button.
3. THE System SHALL reject Cancel_Request submissions for Appointments that are not in Scheduled or Rescheduled status.

### Requirement 6: Cancel Request Data Model

**User Story:** As a developer, I want a CancelAppointmentRequest entity that follows the existing AppointmentRequest pattern, so that the data model is consistent across the codebase.

#### Acceptance Criteria

1. THE CancelAppointmentRequest entity SHALL store the patient identifier, appointment identifier, cancellation reason, status, and creation timestamp.
2. THE CancelAppointmentRequest entity SHALL use the same status lifecycle (Pending, Approved, Denied) as the existing AppointmentRequest entity.
3. THE CancelAppointmentRequest entity SHALL store an optional denial reason when the request is denied.
4. THE ClinicDbContext SHALL include a DbSet for CancelAppointmentRequest entities.

### Requirement 7: Cancel Request API Endpoint

**User Story:** As a developer, I want an API endpoint for creating cancellation requests, so that the UI can submit requests through the existing API pattern.

#### Acceptance Criteria

1. WHEN a POST request is received with a valid appointment identifier and cancellation reason, THE API SHALL create a CancelAppointmentRequest and return the created resource.
2. WHEN a POST request is received for an Appointment that is not in Scheduled or Rescheduled status, THE API SHALL return a 409 Conflict response.
3. WHEN a POST request is received for an Appointment that already has a Pending Cancel_Request, THE API SHALL return a 409 Conflict response.
4. IF the referenced Appointment does not exist, THEN THE API SHALL return a 404 Not Found response.
