# Requirements Document

## Introduction

When clinic staff create, reschedule, or cancel appointments, patients currently receive no notification of the change. The only automated notification is the 24-hour upcoming appointment reminder sent by the AppointmentReminderService background service. This feature closes that gap by generating in-app notifications whenever staff perform actions on appointments, and by making those notifications actionable so patients can respond directly (e.g., confirm, view details, or request rescheduling).

The existing Notification entity, NotificationType enum, and Notifications.razor page provide the foundation. This feature extends them with new notification types for staff-initiated actions, introduces an Appointment_Notification_Service to create notifications at the point of change, and upgrades the Notifications page to render action buttons appropriate to each notification type.

## Glossary

- **Appointment_Notification_Service**: A service responsible for creating Notification records when staff perform actions on appointments.
- **Notification**: The existing entity that stores a user-facing notification with a type, title, message, read status, and optional related appointment reference.
- **NotificationType**: The existing enum that categorizes notifications. Will be extended with new values for appointment creation and update events.
- **Notifications_Page**: The existing Blazor page at `/notifications` that displays a user's notifications.
- **Appointment**: A scheduled therapy session linking a Patient, Therapist, Room, and time slot.
- **Patient**: A registered patient in the clinic system, identified by email and linked to an AppUser account.
- **AppUser**: The ASP.NET Identity user account, looked up by the Patient email to resolve the UserId for notifications.
- **Staff_Actor**: A therapist or clinic staff member who creates, updates, reschedules, or cancels appointments.
- **Action_Button**: A UI element rendered on a notification that allows the patient to perform a contextual action such as confirming, viewing, or requesting a reschedule.

## Requirements

### Requirement 1: Notify Patient on Appointment Creation

**User Story:** As a patient, I want to receive a notification when staff schedule a new appointment for me, so that I am aware of the upcoming session and can take action if needed.

#### Acceptance Criteria

1. WHEN a Staff_Actor creates a new Appointment, THE Appointment_Notification_Service SHALL create a Notification of type AppointmentCreated for the Patient linked to the Appointment.
2. THE Appointment_Notification_Service SHALL resolve the Patient's AppUser by matching the Patient email to the AppUser UserName.
3. THE Notification SHALL include the therapist name, appointment date, and appointment time in the message body.
4. THE Notification SHALL store the Appointment Id in the RelatedAppointmentId field.
5. IF the Patient does not have a corresponding AppUser account, THEN THE Appointment_Notification_Service SHALL skip notification creation for that Patient without raising an error.

### Requirement 2: Notify Patient on Appointment Rescheduling

**User Story:** As a patient, I want to receive a notification when my appointment is rescheduled, so that I know the new date and time and can respond accordingly.

#### Acceptance Criteria

1. WHEN a Staff_Actor reschedules an Appointment, THE Appointment_Notification_Service SHALL create a Notification of type AppointmentRescheduled for the Patient linked to the Appointment.
2. THE Notification message SHALL include both the original date/time and the new date/time of the Appointment.
3. THE Notification SHALL store the Appointment Id in the RelatedAppointmentId field.
4. IF the Patient does not have a corresponding AppUser account, THEN THE Appointment_Notification_Service SHALL skip notification creation for that Patient without raising an error.

### Requirement 3: Notify Patient on Appointment Cancellation

**User Story:** As a patient, I want to receive a notification when my appointment is cancelled by staff, so that I am informed promptly and can request a new appointment if needed.

#### Acceptance Criteria

1. WHEN a Staff_Actor cancels an Appointment, THE Appointment_Notification_Service SHALL create a Notification of type CancellationApproved for the Patient linked to the Appointment.
2. THE Notification message SHALL include the therapist name and the original appointment date and time.
3. THE Notification SHALL store the Appointment Id in the RelatedAppointmentId field.
4. IF the Patient does not have a corresponding AppUser account, THEN THE Appointment_Notification_Service SHALL skip notification creation for that Patient without raising an error.

### Requirement 4: Notify Patient on Appointment Update

**User Story:** As a patient, I want to receive a notification when details of my appointment change (e.g., room, therapist, or therapy type), so that I have accurate information about my session.

#### Acceptance Criteria

1. WHEN a Staff_Actor updates an Appointment's details (therapist, room, or therapy type) without changing the time, THE Appointment_Notification_Service SHALL create a Notification of type AppointmentUpdated for the Patient linked to the Appointment.
2. THE Notification message SHALL describe what changed (e.g., new therapist name or new room).
3. THE Notification SHALL store the Appointment Id in the RelatedAppointmentId field.
4. IF the Patient does not have a corresponding AppUser account, THEN THE Appointment_Notification_Service SHALL skip notification creation for that Patient without raising an error.

### Requirement 5: Extend NotificationType Enum

**User Story:** As a developer, I want the NotificationType enum to include values for appointment creation and update events, so that the system can categorize and render these notifications correctly.

#### Acceptance Criteria

1. THE NotificationType enum SHALL include an AppointmentCreated value.
2. THE NotificationType enum SHALL include an AppointmentUpdated value.
3. THE existing NotificationType values (MissedAppointment, UpcomingAppointment, RequestApproved, RequestDenied, SchedulingConflict, AppointmentRescheduled, CancellationRequested, CancellationApproved, CancellationDenied) SHALL remain unchanged.

### Requirement 6: Actionable Notification Rendering

**User Story:** As a patient, I want notifications to include action buttons relevant to the notification type, so that I can respond to appointment changes directly from the notification.

#### Acceptance Criteria

1. WHEN a Notification of type AppointmentCreated is displayed, THE Notifications_Page SHALL render a "View Appointment" Action_Button that navigates the user to the schedule page.
2. WHEN a Notification of type AppointmentRescheduled is displayed, THE Notifications_Page SHALL render a "View Appointment" Action_Button that navigates the user to the schedule page.
3. WHEN a Notification of type CancellationApproved is displayed, THE Notifications_Page SHALL render a "Request New Appointment" Action_Button that navigates the user to the appointment request page.
4. WHEN a Notification of type AppointmentUpdated is displayed, THE Notifications_Page SHALL render a "View Appointment" Action_Button that navigates the user to the schedule page.
5. THE Notifications_Page SHALL display an appropriate icon and color for each new NotificationType value, consistent with the existing icon and color mapping pattern.

### Requirement 7: Notification Service User Lookup Pattern

**User Story:** As a developer, I want the notification service to follow the same user lookup pattern as the AppointmentReminderService, so that the codebase remains consistent and maintainable.

#### Acceptance Criteria

1. THE Appointment_Notification_Service SHALL look up the AppUser by querying for a user whose UserName matches the Patient Email, following the same pattern used in AppointmentReminderService.
2. THE Appointment_Notification_Service SHALL use the ClinicDbContext to persist Notification entities.
3. THE Appointment_Notification_Service SHALL be registered as a scoped service in the dependency injection container.

### Requirement 8: Notification Logging and Persistence

**User Story:** As a clinic administrator, I want all appointment notifications to be persisted in the database, so that there is a complete audit trail of patient communications.

#### Acceptance Criteria

1. THE Appointment_Notification_Service SHALL persist every created Notification to the database via the ClinicDbContext before returning.
2. THE Notification CreatedAt field SHALL be set to the current UTC time at the moment of creation.
3. THE Notification IsRead field SHALL default to false when created.
4. IF a database error occurs during notification persistence, THEN THE Appointment_Notification_Service SHALL log the error and allow the appointment operation to complete without failing.
