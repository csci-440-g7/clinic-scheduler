# Requirements Document

## Introduction

This feature addresses gaps identified from a CSCI-359-61E Systems Analysis & Design requirements report compared to the current ClinicScheduler codebase. The gaps span domain model completeness (TimeSlot, ScheduleConflict, TreatmentPlan status, Therapist NPI, Location capacity), infrastructure concerns (automatic audit logging), UI accessibility (WCAG 2.1), documentation, and production deployment documentation. Implementing these gaps brings the system into alignment with the original design specification and prepares it for nationwide multi-location expansion.

## Glossary

- **Scheduling_Service**: The `AppointmentSchedulingService` responsible for creating, validating, and rescheduling appointments.
- **TimeSlot**: A configurable entity representing an available scheduling window at a specific location, defined by start time, end time, and day of week.
- **ScheduleConflict**: A dedicated entity that records detected scheduling conflicts, categorized by type and tracking resolution status.
- **TreatmentPlan**: An entity representing a patient's prescribed therapy schedule, including frequency, duration, assigned therapist, and lifecycle status.
- **TreatmentPlanStatus**: An enumeration of treatment plan lifecycle states: Active, Suspended, Ended.
- **Therapist**: A clinical staff member who provides therapy sessions to patients.
- **NPI_Number**: A National Provider Identifier, a unique 10-digit identification number issued to healthcare providers in the United States.
- **Location**: A physical clinic site with address, time zone, rooms, and operational configuration.
- **Daily_Capacity**: The maximum number of patients a specific location can serve in a single day.
- **AuditLog**: An immutable record of entity changes (create, modify, delete) captured automatically by the system.
- **DbContext**: The Entity Framework Core database context (`ClinicDbContext`) that manages entity persistence and change tracking.
- **ChangeTracker**: The EF Core component within the DbContext that detects entity state changes before saving to the database.
- **WCAG_2_1**: Web Content Accessibility Guidelines version 2.1, a set of standards for making web content accessible to people with disabilities.
- **ARIA**: Accessible Rich Internet Applications, a set of HTML attributes that define ways to make web content more accessible.
- **Skip_Navigation**: A hidden link at the top of a page that allows keyboard users to bypass repetitive navigation and jump directly to main content.
- **Conflict_Type**: A categorization of scheduling conflicts: DoubleBook (overlapping appointments for same resource), OutsideHours (appointment outside location operating hours), Capacity (location daily capacity exceeded).

## Requirements

### Requirement 1: Configurable TimeSlot Entity

**User Story:** As a clinic administrator, I want to configure available time slots per location, so that different clinic locations can operate on different schedules as the organization expands nationwide.

#### Acceptance Criteria

1. THE TimeSlot entity SHALL have the following properties: an integer identifier, a start time (TimeOnly), an end time (TimeOnly), a day of week (DayOfWeek), and a foreign key reference to a Location.
2. WHEN a TimeSlot is created, THE TimeSlot SHALL validate that the start time is earlier than the end time.
3. WHEN a TimeSlot is created, THE TimeSlot SHALL validate that the day of week is a value between Sunday (0) and Saturday (6).
4. THE DbContext SHALL include a DbSet for TimeSlot entities and configure the Location-to-TimeSlot relationship as one-to-many.
5. WHEN the Scheduling_Service validates an appointment time, THE Scheduling_Service SHALL check the appointment start time against the TimeSlot records for the appointment's location instead of using hardcoded clinic hours.
6. IF no TimeSlot records exist for a location, THEN THE Scheduling_Service SHALL fall back to the default schedule of 8:00 AM to 5:00 PM on weekdays (Monday through Friday) with 30-minute increments.

### Requirement 2: ScheduleConflict Entity

**User Story:** As a clinic manager, I want scheduling conflicts to be recorded with their type and resolution status, so that I can track and resolve conflicts systematically rather than relying on a simple boolean flag.

#### Acceptance Criteria

1. THE ScheduleConflict entity SHALL have the following properties: an integer identifier, a foreign key reference to an Appointment, a detection timestamp (DateTime), a ConflictType enumeration value, and a boolean resolved flag.
2. THE ConflictType enumeration SHALL define the following values: DoubleBook, OutsideHours, Capacity.
3. WHEN the Scheduling_Service detects a scheduling conflict during appointment creation, THE Scheduling_Service SHALL create a ScheduleConflict record with the appropriate ConflictType and set the resolved flag to false.
4. WHEN a ScheduleConflict is resolved, THE ScheduleConflict entity SHALL update the resolved flag to true and record the resolution timestamp.
5. THE DbContext SHALL include a DbSet for ScheduleConflict entities and configure the Appointment-to-ScheduleConflict relationship as one-to-many.
6. THE Appointment entity SHALL retain the existing HasConflict boolean property for backward compatibility and set it to true when any associated ScheduleConflict record exists.

### Requirement 3: Automatic Audit Logging

**User Story:** As a compliance officer, I want all entity changes to be automatically recorded in the audit log, so that I have an immutable trail of all schedule, patient, and user modifications for regulatory compliance.

#### Acceptance Criteria

1. WHEN the DbContext SaveChangesAsync method is called, THE DbContext SHALL intercept all tracked entity changes (Added, Modified, Deleted) and create an AuditLog entry for each changed entity before persisting to the database.
2. THE AuditLog entry SHALL record the entity name, entity identifier, the action performed (Created, Modified, Deleted), a summary of changed property values, and the UTC timestamp.
3. WHILE processing Modified entities, THE DbContext SHALL capture the original and current values of each changed property and format the differences as the ChangeSummary string.
4. THE DbContext SHALL exclude AuditLog entities from audit logging to prevent recursive logging.
5. WHEN an entity is Added, THE AuditLog ChangeSummary SHALL contain the key property values of the new entity.
6. WHEN an entity is Deleted, THE AuditLog ChangeSummary SHALL contain the key property values of the removed entity.

### Requirement 4: TreatmentPlan Status Lifecycle

**User Story:** As a therapist, I want treatment plans to have explicit lifecycle states (Active, Suspended, Ended), so that I can manage patient treatment progression and distinguish between current and historical plans.

#### Acceptance Criteria

1. THE TreatmentPlanStatus enumeration SHALL define the following values: Active, Suspended, Ended.
2. THE TreatmentPlan entity SHALL include a Status property of type TreatmentPlanStatus.
3. WHEN a TreatmentPlan is created, THE TreatmentPlan SHALL set the initial Status to Active.
4. WHEN a TreatmentPlan Status is changed to Suspended, THE TreatmentPlan SHALL record the suspension and update the UpdatedAt timestamp.
5. WHEN a TreatmentPlan Status is changed to Ended, THE TreatmentPlan SHALL record the termination and update the UpdatedAt timestamp.
6. IF a TreatmentPlan Status is Ended, THEN THE TreatmentPlan SHALL reject attempts to change the Status to Active or Suspended.
7. THE TreatmentPlans Blazor page SHALL display the current Status of each treatment plan and provide controls to change the Status.

### Requirement 5: Therapist NPI Number

**User Story:** As a clinic administrator, I want each therapist record to include a National Provider Identifier (NPI) number, so that the system captures the clinical identification required for healthcare provider records.

#### Acceptance Criteria

1. THE Therapist entity SHALL include an optional NpiNumber property of type string.
2. WHEN an NpiNumber is provided, THE Therapist entity SHALL validate that the NpiNumber is exactly 10 digits.
3. THE Therapist management UI SHALL display the NpiNumber field and allow editing during therapist creation and update operations.
4. THE DbContext SHALL configure a unique index on the Therapist NpiNumber column, filtered to exclude null values.

### Requirement 6: WCAG 2.1 Accessibility Compliance

**User Story:** As a user with disabilities, I want the application to follow WCAG 2.1 Level AA guidelines, so that I can navigate and use the scheduling system with assistive technologies.

#### Acceptance Criteria

1. THE MainLayout SHALL include a Skip_Navigation link as the first focusable element that navigates to the main content area when activated.
2. THE MainLayout SHALL assign an ARIA landmark role of "navigation" to the sidebar element and an ARIA landmark role of "main" to the main content area.
3. WHEN interactive elements (buttons, links, icon buttons) lack visible text, THE Blazor pages SHALL provide an aria-label attribute describing the element's purpose.
4. THE dashboard stat cards on the Home page SHALL use semantic HTML structure with appropriate ARIA attributes to convey their meaning to screen readers.
5. THE data tables across all pages SHALL include appropriate table header associations for screen reader navigation.

### Requirement 7: Comprehensive Documentation

**User Story:** As a developer or administrator, I want comprehensive documentation covering architecture, setup, and administration, so that new team members can onboard efficiently and administrators can manage the system independently.

#### Acceptance Criteria

1. THE developer documentation SHALL include an architecture overview describing the layered project structure (Core, Infrastructure, Shared, Web), key design patterns (Repository, Domain Entities), and technology stack (.NET 10, Blazor, EF Core, PostgreSQL, MudBlazor).
2. THE developer documentation SHALL include a setup guide with prerequisites, database configuration steps, environment variable descriptions, and commands to build and run the application.
3. THE developer documentation SHALL include an API documentation section listing all REST API endpoints, their HTTP methods, request parameters, and response formats.
4. THE administrator documentation SHALL include a user guide covering user management, appointment scheduling workflows, treatment plan management, and report generation.

### Requirement 8: Location Daily Capacity

**User Story:** As a clinic administrator, I want to set a daily patient capacity per location, so that scheduling respects each location's physical and staffing constraints rather than using a single global limit.

#### Acceptance Criteria

1. THE Location entity SHALL include a DailyCapacity property of type integer with a default value of 12.
2. WHEN the Scheduling_Service validates a new appointment, THE Scheduling_Service SHALL count the number of distinct patients with active appointments at the target location on the appointment date and reject the appointment if the count meets or exceeds the location's DailyCapacity.
3. IF a Location DailyCapacity is not explicitly set, THEN THE Scheduling_Service SHALL use the default value of 12.
4. THE Location management UI SHALL display the DailyCapacity field and allow administrators to update the value.
5. WHEN a DailyCapacity value is provided, THE Location entity SHALL validate that the value is a positive integer greater than zero.

### Requirement 9: Cross-Browser and Responsive Testing Documentation

**User Story:** As a QA engineer, I want documented browser compatibility targets and responsive design verification procedures, so that the team can systematically verify the application works across supported environments.

#### Acceptance Criteria

1. THE testing documentation SHALL list the supported browsers and minimum versions (Chrome latest two versions, Firefox latest two versions, Edge latest two versions, Safari latest two versions on macOS and iOS).
2. THE testing documentation SHALL describe responsive design breakpoints used by the application and the expected layout behavior at each breakpoint.
3. THE testing documentation SHALL include a manual testing checklist for verifying responsive behavior on mobile, tablet, and desktop viewports.

### Requirement 10: HTTPS Production Deployment Documentation

**User Story:** As a DevOps engineer, I want the HTTPS termination strategy documented, so that the team understands why the application skips HTTPS redirection in production and how TLS is handled by the infrastructure.

#### Acceptance Criteria

1. THE deployment documentation SHALL explain that HTTPS termination is handled by the external load balancer in production environments.
2. THE deployment documentation SHALL document that the application explicitly skips `UseHttpsRedirection()` in production because TLS is terminated at the load balancer layer.
3. THE deployment documentation SHALL include a diagram or description of the request flow from client through load balancer to application container, showing where TLS encryption is applied and terminated.
4. THE deployment documentation SHALL list the required load balancer configuration for TLS certificates and health check endpoints.
