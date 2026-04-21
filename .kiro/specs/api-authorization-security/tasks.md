# Implementation Plan: API Authorization & Security

## Overview

This plan hardens the ClinicScheduler API by adding authentication enforcement, role-based access control, DTO validation, CSRF protection, and a unified password policy. Changes are confined to the Web project layer (controllers, DTOs, Razor forms, and `Program.cs`) plus new tests in the Core.Tests project. Each task builds incrementally so the application remains functional after every step.

## Tasks

- [x] 1. Create RoleNames constants and enforce authentication on all API controllers
  - [x] 1.1 Create `RoleNames` static class in `ClinicScheduler.Web/RoleNames.cs` with constants: `Admin`, `ClinicManager`, `Staff`, `Therapist`, `Patient`, `StaffOrAbove` ("Admin,ClinicManager,Staff,Therapist"), and `AdminOrManager` ("Admin,ClinicManager")
    - _Requirements: 2, 3, 4, 5, 6, 7_
  - [x] 1.2 Add `[Authorize]` attribute to `PatientsController`, `TherapistsController`, `RoomsController`, `LocationsController`, `TherapyTypesController`, `TreatmentPlansController`, `CancelAppointmentRequestsController`, and `AppointmentsController` at the class level so all actions require authentication by default
    - Import `Microsoft.AspNetCore.Authorization` where missing
    - `AccountController` must keep `[AllowAnonymous]` and NOT get `[Authorize]`
    - _Requirements: 1.1, 1.2, 1.3_

- [x] 2. Add role-based authorization to AppointmentsController and PatientsController
  - [x] 2.1 On `AppointmentsController`, replace the class-level `[Authorize]` with `[Authorize(Roles = RoleNames.StaffOrAbove)]` so all actions (GET, POST, PUT, DELETE, mark-missed) require Staff_Or_Above
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_
  - [x] 2.2 On `PatientsController`, keep class-level `[Authorize]`. Add `[Authorize(Roles = RoleNames.StaffOrAbove)]` to `Create`, `Update`, and `Delete` actions. Modify `GetAll` to add `[Authorize(Roles = RoleNames.StaffOrAbove)]`. Modify `GetById` to allow Staff_Or_Above full access and Patient role owner-access only (compare `User.Identity.Name` email against `patient.Email`; return `Forbid()` on mismatch)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 3. Add role-based authorization to administrative and remaining controllers
  - [x] 3.1 On `RoomsController`, `LocationsController`, and `TherapyTypesController`: keep class-level `[Authorize]` for GET actions (any authenticated user). Add `[Authorize(Roles = RoleNames.AdminOrManager)]` to `Create`, `Update`, and `Delete` actions
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_
  - [x] 3.2 On `TherapistsController`: add `[Authorize(Roles = RoleNames.StaffOrAbove)]` to `GetAll` and `GetById`. Add `[Authorize(Roles = RoleNames.AdminOrManager)]` to `Create`, `Update`, and `Delete`
    - _Requirements: 5.1, 5.2, 5.3_
  - [x] 3.3 On `TreatmentPlansController`: replace class-level `[Authorize]` with `[Authorize(Roles = RoleNames.StaffOrAbove)]` so all actions require Staff_Or_Above
    - _Requirements: 6.1, 6.2, 6.3_
  - [x] 3.4 On `CancelAppointmentRequestsController`: keep class-level `[Authorize]` for POST (any authenticated user). Add `[Authorize(Roles = RoleNames.StaffOrAbove)]` to `GetAll` and `GetById`, then add Patient-role filtering logic to `GetAll` — when the user is in the Patient role and not Staff_Or_Above, filter results to only cancellation requests where `Patient.Email` matches `User.Identity.Name`. Allow Patient role access to `GetAll` by using a custom check inside the action rather than a strict role attribute (keep `[Authorize]` at class level, check roles in action body)
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [x] 4. Checkpoint - Verify authorization wiring
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Add input validation to DTOs
  - [x] 5.1 In `UpdateAppointmentRequest.cs`, add `[Required]` and `[Range(1, int.MaxValue)]` attributes to `PatientId`, `TherapistId`, and `RoomId` properties
    - _Requirements: 8.1, 8.2, 8.3, 8.4_
  - [x] 5.2 In `UpdateTherapistRequest.cs`, add `[Required]` and `[StringLength(100, MinimumLength = 1)]` to `FirstName` and `LastName`. Add `[Required]` to `Email` (it already has `[EmailAddress]` and `[StringLength(255)]`)
    - _Requirements: 9.1, 9.2, 9.3, 9.4_
  - [x] 5.3 Write property test for UpdateAppointmentRequest ID field validation
    - **Property 1: UpdateAppointmentRequest ID field validation**
    - Generate random `UpdateAppointmentRequest` instances with varying PatientId, TherapistId, and RoomId values. Validate that model validation fails when any ID < 1 and passes when all IDs >= 1.
    - Use FsCheck with xUnit in `ClinicScheduler.Core.Tests/DtoValidationPropertyTests.cs`
    - Minimum 100 iterations
    - **Validates: Requirements 8.1, 8.2, 8.3**
  - [x] 5.4 Write property test for UpdateTherapistRequest required name validation
    - **Property 2: UpdateTherapistRequest required name validation**
    - Generate random `UpdateTherapistRequest` instances with varying FirstName and LastName strings. Validate that model validation fails when either name is empty/null and passes when both are non-empty.
    - Use FsCheck with xUnit in `ClinicScheduler.Core.Tests/DtoValidationPropertyTests.cs`
    - Minimum 100 iterations
    - **Validates: Requirements 9.1, 9.2**

- [x] 6. Add CSRF protection to login and logout forms
  - [x] 6.1 In `Login.razor`, add `<AntiforgeryToken />` inside the `<form>` tag (after the opening `<form>` element, before the hidden `returnUrl` input)
    - _Requirements: 10.1_
  - [x] 6.2 In `NavMenu.razor`, add `<AntiforgeryToken />` inside the logout `<form>` tag
    - _Requirements: 10.2_
  - [x] 6.3 In `AccountController.cs`, remove the `[IgnoreAntiforgeryToken]` attribute from both the `Login` and `Logout` actions
    - _Requirements: 10.3, 10.4, 10.5_

- [x] 7. Enforce strong password policy in all environments
  - [x] 7.1 In `Program.cs`, replace the current environment-conditional password configuration with a unified baseline: `RequireDigit = true`, `RequiredLength = 8`, `RequireNonAlphanumeric = true`, `RequireUppercase = true` for all environments. Add a production-only override that sets `RequiredLength = 10`
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5_
  - [x] 7.2 Write property test for password policy acceptance
    - **Property 3: Password policy acceptance**
    - Generate random password strings. Validate that the non-production password policy accepts if and only if length >= 8, has at least one uppercase letter, at least one digit, and at least one non-alphanumeric character.
    - Use FsCheck with xUnit in `ClinicScheduler.Core.Tests/PasswordPolicyPropertyTests.cs`
    - Minimum 100 iterations
    - **Validates: Requirements 11.1, 11.2, 11.3, 11.4**

- [x] 8. Checkpoint - Verify all changes compile and tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- All tasks are required, including property-based tests
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document (Properties 1–3)
- Property 4 (cancellation request patient filtering) is covered by the filtering logic in task 3.4 and can be validated via unit tests
- The `RoleNames` constants class avoids magic strings across all controller attributes
- No database schema changes are required — owner-access uses email matching
