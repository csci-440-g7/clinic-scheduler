# Requirements Document

## Introduction

The ClinicScheduler application exposes REST API endpoints through ASP.NET controllers that currently lack authorization enforcement, role-based access control, consistent input validation, CSRF protection on the login form, and a strong password policy across all environments. This feature addresses those five security gaps to ensure that API endpoints are protected, users can only access data appropriate to their role, all incoming data is validated at the API boundary, anti-forgery tokens are enforced on authentication forms, and passwords meet minimum complexity requirements regardless of environment.

## Glossary

- **API_Controller**: An ASP.NET `[ApiController]` class that handles HTTP requests for a specific resource (e.g., PatientsController, AppointmentsController).
- **Authorization_Middleware**: The ASP.NET middleware pipeline component (`UseAuthentication` / `UseAuthorization`) that evaluates `[Authorize]` attributes on controllers and actions.
- **Role**: An ASP.NET Identity role assigned to a user. The application defines five roles: Admin, ClinicManager, Staff, Therapist, and Patient.
- **Staff_Or_Above**: A user who holds the Admin, ClinicManager, Staff, or Therapist role.
- **Admin_Or_Manager**: A user who holds the Admin or ClinicManager role.
- **Request_DTO**: A C# class used to deserialize and validate incoming HTTP request bodies (e.g., CreateAppointmentRequest, UpdatePatientRequest).
- **Validation_Attribute**: A `System.ComponentModel.DataAnnotations` attribute (e.g., `[Required]`, `[StringLength]`, `[Range]`, `[EmailAddress]`) applied to Request_DTO properties.
- **Antiforgery_Token**: A server-generated token validated by ASP.NET to prevent cross-site request forgery attacks on form submissions.
- **Password_Policy**: The set of `IdentityOptions.Password` rules that govern minimum length, required character classes, and complexity for user passwords.
- **Owner_Access**: A data-access pattern where a user in the Patient role can only read or modify records that belong to that user's linked patient profile.

## Requirements

### Requirement 1: Enforce Authentication on All API Controllers

**User Story:** As a clinic administrator, I want all API endpoints to require authentication, so that unauthenticated users cannot access or modify clinic data.

#### Acceptance Criteria

1. THE Authorization_Middleware SHALL require an authenticated user for every action on PatientsController, TherapistsController, RoomsController, LocationsController, TherapyTypesController, TreatmentPlansController, CancelAppointmentRequestsController, and AppointmentsController.
2. WHEN an unauthenticated request is received by any protected API_Controller, THE Authorization_Middleware SHALL return HTTP 401 Unauthorized.
3. THE AccountController SHALL remain accessible to unauthenticated users for login and logout actions.

### Requirement 2: Role-Based Access Control for Appointment Management

**User Story:** As a clinic manager, I want appointment endpoints restricted by role, so that only authorized staff can create, update, and delete appointments.

#### Acceptance Criteria

1. WHEN a user with the Staff_Or_Above role sends a GET request to the appointments endpoint, THE AppointmentsController SHALL return the list of appointments.
2. WHEN a user with the Staff_Or_Above role sends a POST request to the appointments endpoint, THE AppointmentsController SHALL create the appointment.
3. WHEN a user with the Staff_Or_Above role sends a PUT request to the appointments endpoint, THE AppointmentsController SHALL update the appointment.
4. WHEN a user with the Staff_Or_Above role sends a DELETE request to the appointments endpoint, THE AppointmentsController SHALL delete the appointment.
5. WHEN a user with the Staff_Or_Above role sends a POST request to the mark-missed endpoint, THE AppointmentsController SHALL mark the appointment as missed and reschedule.
6. WHEN a user without the Staff_Or_Above role sends a create, update, delete, or mark-missed request to the appointments endpoint, THE Authorization_Middleware SHALL return HTTP 403 Forbidden.

### Requirement 3: Role-Based Access Control for Patient Data

**User Story:** As a clinic manager, I want patient data endpoints restricted by role, so that only authorized staff can manage patient records while patients can view their own data.

#### Acceptance Criteria

1. WHEN a user with the Staff_Or_Above role sends a GET request to the patients endpoint, THE PatientsController SHALL return the list of patients.
2. WHEN a user with the Staff_Or_Above role sends a POST, PUT, or DELETE request to the patients endpoint, THE PatientsController SHALL perform the requested operation.
3. WHEN a user with the Patient role sends a GET request for a patient record that belongs to that user, THE PatientsController SHALL return the patient record.
4. WHEN a user with the Patient role sends a GET request for a patient record that does not belong to that user, THE PatientsController SHALL return HTTP 403 Forbidden.
5. WHEN a user with the Patient role sends a POST, PUT, or DELETE request to the patients endpoint, THE Authorization_Middleware SHALL return HTTP 403 Forbidden.

### Requirement 4: Role-Based Access Control for Administrative Resources

**User Story:** As a clinic administrator, I want rooms, locations, and therapy type management restricted to Admin and ClinicManager roles, so that only authorized personnel can modify clinic configuration.

#### Acceptance Criteria

1. WHEN a user with the Admin_Or_Manager role sends a POST, PUT, or DELETE request to the rooms endpoint, THE RoomsController SHALL perform the requested operation.
2. WHEN a user with the Admin_Or_Manager role sends a POST, PUT, or DELETE request to the locations endpoint, THE LocationsController SHALL perform the requested operation.
3. WHEN a user with the Admin_Or_Manager role sends a POST, PUT, or DELETE request to the therapy-types endpoint, THE TherapyTypesController SHALL perform the requested operation.
4. WHEN an authenticated user sends a GET request to the rooms, locations, or therapy-types endpoint, THE respective API_Controller SHALL return the requested data.
5. WHEN a user without the Admin_Or_Manager role sends a POST, PUT, or DELETE request to the rooms, locations, or therapy-types endpoint, THE Authorization_Middleware SHALL return HTTP 403 Forbidden.

### Requirement 5: Role-Based Access Control for Therapist Management

**User Story:** As a clinic manager, I want therapist endpoints restricted by role, so that only staff-level users and above can view therapists and only managers and admins can modify therapist records.

#### Acceptance Criteria

1. WHEN a user with the Staff_Or_Above role sends a GET request to the therapists endpoint, THE TherapistsController SHALL return the list of therapists.
2. WHEN a user with the Admin_Or_Manager role sends a POST, PUT, or DELETE request to the therapists endpoint, THE TherapistsController SHALL perform the requested operation.
3. WHEN a user without the Admin_Or_Manager role sends a POST, PUT, or DELETE request to the therapists endpoint, THE Authorization_Middleware SHALL return HTTP 403 Forbidden.

### Requirement 6: Role-Based Access Control for Treatment Plans

**User Story:** As a clinic manager, I want treatment plan endpoints restricted by role, so that only staff-level users and above can manage treatment plans.

#### Acceptance Criteria

1. WHEN a user with the Staff_Or_Above role sends a GET request to the treatment-plans endpoint, THE TreatmentPlansController SHALL return the list of treatment plans.
2. WHEN a user with the Staff_Or_Above role sends a POST, PUT, or DELETE request to the treatment-plans endpoint, THE TreatmentPlansController SHALL perform the requested operation.
3. WHEN a user without the Staff_Or_Above role sends any request to the treatment-plans endpoint, THE Authorization_Middleware SHALL return HTTP 403 Forbidden.

### Requirement 7: Role-Based Access Control for Cancellation Requests

**User Story:** As a clinic manager, I want cancellation request endpoints restricted by role, so that patients can submit cancellation requests and staff can review them.

#### Acceptance Criteria

1. WHEN an authenticated user sends a POST request to the cancel-appointment-requests endpoint, THE CancelAppointmentRequestsController SHALL create the cancellation request.
2. WHEN a user with the Staff_Or_Above role sends a GET request to the cancel-appointment-requests endpoint, THE CancelAppointmentRequestsController SHALL return the list of cancellation requests.
3. WHEN a user with the Patient role sends a GET request to the cancel-appointment-requests endpoint, THE CancelAppointmentRequestsController SHALL return only cancellation requests belonging to that patient.
4. WHEN a user without the Staff_Or_Above or Patient role sends a GET request to the cancel-appointment-requests endpoint, THE Authorization_Middleware SHALL return HTTP 403 Forbidden.

### Requirement 8: Input Validation on UpdateAppointmentRequest

**User Story:** As a developer, I want the UpdateAppointmentRequest DTO to enforce validation attributes, so that invalid appointment updates are rejected at the API boundary.

#### Acceptance Criteria

1. THE UpdateAppointmentRequest SHALL require the PatientId property with a value of 1 or greater.
2. THE UpdateAppointmentRequest SHALL require the TherapistId property with a value of 1 or greater.
3. THE UpdateAppointmentRequest SHALL require the RoomId property with a value of 1 or greater.
4. WHEN an UpdateAppointmentRequest is submitted with a missing or invalid required field, THE API_Controller SHALL return HTTP 400 Bad Request with validation error details.

### Requirement 9: Input Validation on UpdateTherapistRequest

**User Story:** As a developer, I want the UpdateTherapistRequest DTO to enforce required field validation, so that incomplete therapist updates are rejected.

#### Acceptance Criteria

1. THE UpdateTherapistRequest SHALL require the FirstName property with a minimum length of 1 character.
2. THE UpdateTherapistRequest SHALL require the LastName property with a minimum length of 1 character.
3. THE UpdateTherapistRequest SHALL require the Email property and validate it as a well-formed email address.
4. WHEN an UpdateTherapistRequest is submitted with a missing or invalid required field, THE API_Controller SHALL return HTTP 400 Bad Request with validation error details.

### Requirement 10: CSRF Protection on Login and Logout Forms

**User Story:** As a security engineer, I want the login and logout forms to validate anti-forgery tokens, so that cross-site request forgery attacks against the authentication endpoints are prevented.

#### Acceptance Criteria

1. THE Login.razor form SHALL include an `<AntiforgeryToken />` component (or equivalent hidden input) so that the Antiforgery_Token is submitted with every POST to `/account/login`.
2. THE NavMenu.razor logout form SHALL include an `<AntiforgeryToken />` component (or equivalent hidden input) so that the Antiforgery_Token is submitted with every POST to `/account/logout`.
3. THE AccountController login action SHALL remove the `[IgnoreAntiforgeryToken]` attribute so that the Antiforgery_Token is validated on every POST request.
4. THE AccountController logout action SHALL remove the `[IgnoreAntiforgeryToken]` attribute so that the Antiforgery_Token is validated on every POST request.
5. WHEN a POST request to the login or logout endpoint is missing or has an invalid Antiforgery_Token, THE AccountController SHALL return HTTP 400 Bad Request.

### Requirement 11: Enforce Strong Password Policy in All Environments

**User Story:** As a security engineer, I want a consistent minimum password policy across all environments, so that development accounts are not created with weak passwords that could be exploited if the database is shared or migrated.

#### Acceptance Criteria

1. THE Password_Policy SHALL require a minimum password length of 8 characters in all environments.
2. THE Password_Policy SHALL require at least one uppercase letter in all environments.
3. THE Password_Policy SHALL require at least one digit in all environments.
4. THE Password_Policy SHALL require at least one non-alphanumeric character in all environments.
5. WHILE the application is running in the production environment, THE Password_Policy SHALL require a minimum password length of 10 characters.
