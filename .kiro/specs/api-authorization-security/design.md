# Design Document: API Authorization & Security

## Overview

This design hardens the ClinicScheduler API layer by closing five security gaps identified in the requirements: unauthenticated API access, missing role-based access control, incomplete DTO validation, absent CSRF protection on authentication forms, and a weak non-production password policy.

The approach uses ASP.NET's built-in authorization infrastructure — `[Authorize]`, `[Authorize(Roles = "...")]`, and `[AllowAnonymous]` attributes — applied directly to controllers and actions. No custom middleware or policy handlers are needed because the existing role set (Admin, ClinicManager, Staff, Therapist, Patient) maps cleanly to the required access matrix. For the Patient owner-access pattern on the patients endpoint, a lightweight check inside the controller action compares the authenticated user's linked patient record against the requested resource.

Input validation uses `System.ComponentModel.DataAnnotations` attributes on the two under-validated DTOs (`UpdateAppointmentRequest`, `UpdateTherapistRequest`). CSRF protection is restored by removing `[IgnoreAntiforgeryToken]` from `AccountController` and adding `<AntiforgeryToken />` to the login and logout forms. The password policy is unified to a single strong baseline with an elevated minimum length in production.

### Design Decisions

1. **Attribute-based authorization over policy-based**: The role matrix is static and well-defined. Using `[Authorize(Roles = "...")]` is simpler, more readable, and avoids the overhead of custom `IAuthorizationHandler` implementations. If the role model grows more complex (e.g., resource-level permissions), policies can be introduced later.

2. **Controller-level `[Authorize]` with action-level overrides**: Each controller gets a base `[Authorize]` attribute. Actions that need different role restrictions use `[Authorize(Roles = "...")]` at the action level. This prevents accidental exposure of new endpoints.

3. **Owner-access via in-controller check**: Requirement 3 needs patients to view their own records. Rather than building a full resource-based authorization handler, the `PatientsController.GetById` action checks whether the authenticated user's patient ID matches the requested ID. This is proportional to the single use case.

4. **No new Patient.UserId column**: The Patient entity currently lacks a `UserId` foreign key to `AppUser`. The owner-access check will query the patient table by email, matching `Patient.Email` against the authenticated user's email claim. This avoids a schema migration for a single lookup. If more owner-access patterns emerge, adding a `UserId` FK would be the next step.

## Architecture

The changes are confined to the Web project layer. No new services, middleware, or infrastructure components are introduced.

```mermaid
graph TD
    subgraph "HTTP Pipeline (existing)"
        A[Request] --> B[UseAuthentication]
        B --> C[UseAuthorization]
        C --> D[UseAntiforgery]
        D --> E[Controller Action]
    end

    subgraph "Controller Layer (modified)"
        E --> F{Has [Authorize]?}
        F -->|No auth| G[401 Unauthorized]
        F -->|Authenticated| H{Role check}
        H -->|Forbidden| I[403 Forbidden]
        H -->|Allowed| J[Execute Action]
        J --> K{Owner check needed?}
        K -->|Yes| L[Compare user email to patient email]
        L -->|Mismatch| I
        L -->|Match| M[Return data]
        K -->|No| M
    end

    subgraph "DTO Validation (modified)"
        J --> N{ModelState.IsValid?}
        N -->|Invalid| O[400 Bad Request]
        N -->|Valid| P[Process request]
    end
```

### Affected Components

| Component | Change |
|---|---|
| `AccountController` | Remove `[IgnoreAntiforgeryToken]`, keep `[AllowAnonymous]` |
| `AppointmentsController` | Add `[Authorize(Roles = "Admin,ClinicManager,Staff,Therapist")]` |
| `PatientsController` | Add `[Authorize]` at class level, `[Authorize(Roles = "Admin,ClinicManager,Staff,Therapist")]` on mutating actions, owner-access check on GET |
| `TherapistsController` | Add `[Authorize(Roles = "Admin,ClinicManager,Staff,Therapist")]` on GET, `[Authorize(Roles = "Admin,ClinicManager")]` on POST/PUT/DELETE |
| `RoomsController` | Add `[Authorize]` on GET, `[Authorize(Roles = "Admin,ClinicManager")]` on POST/PUT/DELETE |
| `LocationsController` | Add `[Authorize]` on GET, `[Authorize(Roles = "Admin,ClinicManager")]` on POST/PUT/DELETE |
| `TherapyTypesController` | Add `[Authorize]` on GET, `[Authorize(Roles = "Admin,ClinicManager")]` on POST/PUT/DELETE |
| `TreatmentPlansController` | Add `[Authorize(Roles = "Admin,ClinicManager,Staff,Therapist")]` |
| `CancelAppointmentRequestsController` | Add `[Authorize]` at class level, role-specific logic on GET |
| `UpdateAppointmentRequest` | Add `[Required]` and `[Range(1, int.MaxValue)]` to PatientId, TherapistId, RoomId |
| `UpdateTherapistRequest` | Add `[Required]` and `[MinLength(1)]` to FirstName, LastName; add `[Required]` to Email |
| `Login.razor` | Add `<AntiforgeryToken />` inside the form |
| `NavMenu.razor` | Add `<AntiforgeryToken />` inside the logout form |
| `Program.cs` | Unify password policy to 8-char minimum with complexity in all environments, 10-char in production |

## Components and Interfaces

### Authorization Attribute Matrix

The following table defines the complete role-to-endpoint mapping. "Any authenticated" means `[Authorize]` with no role restriction.

| Controller | GET | POST | PUT | DELETE | Special |
|---|---|---|---|---|---|
| `AccountController` | — | `[AllowAnonymous]` | — | — | CSRF validated |
| `AppointmentsController` | Staff_Or_Above | Staff_Or_Above | Staff_Or_Above | Staff_Or_Above | mark-missed: Staff_Or_Above |
| `PatientsController` | Staff_Or_Above + Patient (own) | Staff_Or_Above | Staff_Or_Above | Staff_Or_Above | Owner-access on GET by ID |
| `TherapistsController` | Staff_Or_Above | Admin_Or_Manager | Admin_Or_Manager | Admin_Or_Manager | — |
| `RoomsController` | Any authenticated | Admin_Or_Manager | Admin_Or_Manager | Admin_Or_Manager | — |
| `LocationsController` | Any authenticated | Admin_Or_Manager | Admin_Or_Manager | Admin_Or_Manager | — |
| `TherapyTypesController` | Any authenticated | Admin_Or_Manager | Admin_Or_Manager | Admin_Or_Manager | — |
| `TreatmentPlansController` | Staff_Or_Above | Staff_Or_Above | Staff_Or_Above | Staff_Or_Above | — |
| `CancelAppointmentRequestsController` | Staff_Or_Above (all) / Patient (own) | Any authenticated | — | — | Filtered GET for patients |

### Role Constants

To avoid magic strings scattered across attributes, define a static class:

```csharp
namespace ClinicScheduler.Web;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string ClinicManager = "ClinicManager";
    public const string Staff = "Staff";
    public const string Therapist = "Therapist";
    public const string Patient = "Patient";

    public const string StaffOrAbove = $"{Admin},{ClinicManager},{Staff},{Therapist}";
    public const string AdminOrManager = $"{Admin},{ClinicManager}";
}
```

### Owner-Access Check (PatientsController)

For `GET /api/patients/{id}`, when the user is in the Patient role:

```csharp
// Pseudocode for the owner-access check
var userEmail = User.Identity?.Name;
var patient = await _repository.GetByIdAsync(id, ct);
if (patient is null) return NotFound();

if (User.IsInRole(RoleNames.Patient) && !User.IsInRole(RoleNames.Staff) /* etc. */)
{
    if (!string.Equals(patient.Email, userEmail, StringComparison.OrdinalIgnoreCase))
        return Forbid();
}

return Ok(MapToDto(patient));
```

### CancelAppointmentRequests Filtered GET

For `GET /api/cancelappointmentrequests`, when the user is in the Patient role:

```csharp
// Filter to only the patient's own requests
if (User.IsInRole(RoleNames.Patient) && !isStaffOrAbove)
{
    var userEmail = User.Identity?.Name;
    requests = requests.Where(r => r.Patient.Email == userEmail);
}
```

### CSRF Token Restoration

**Login.razor** — add inside the `<form>` tag:
```razor
<form method="post" action="/account/login">
    <AntiforgeryToken />
    ...
</form>
```

**NavMenu.razor** — add inside the logout `<form>` tag:
```razor
<form method="post" action="/account/logout">
    <AntiforgeryToken />
    ...
</form>
```

**AccountController** — remove both `[IgnoreAntiforgeryToken]` attributes. The antiforgery middleware already runs in the pipeline and will validate the token automatically.

## Data Models

### Modified DTOs

**UpdateAppointmentRequest** — add validation attributes to match `CreateAppointmentRequest`:

```csharp
public sealed class UpdateAppointmentRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int PatientId { get; init; }

    [Required]
    [Range(1, int.MaxValue)]
    public int TherapistId { get; init; }

    [Required]
    [Range(1, int.MaxValue)]
    public int RoomId { get; init; }

    // ... remaining properties unchanged
}
```

**UpdateTherapistRequest** — add `[Required]` and `[MinLength]`:

```csharp
public sealed class UpdateTherapistRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;

    // ... remaining properties unchanged
}
```

### Password Policy Configuration

**Program.cs** — unified policy:

```csharp
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // Baseline: all environments
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;

    // Elevated: production only
    if (builder.Environment.IsProduction())
    {
        options.Password.RequiredLength = 10;
    }

    options.SignIn.RequireConfirmedAccount = false;
})
```

### No Schema Changes

The Patient entity does not need a new `UserId` column. The owner-access check uses `Patient.Email` matched against the authenticated user's email claim (`User.Identity.Name`). This works because the seed data already uses the same email for the Patient user account and the Patient entity record.


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

Most of the acceptance criteria in this feature are authorization wiring checks (finite role × endpoint combinations) best covered by example-based integration tests. However, four properties emerge from the DTO validation and password policy requirements, where the input space is large and behavior varies meaningfully with input.

### Property 1: UpdateAppointmentRequest ID field validation

*For any* `UpdateAppointmentRequest` instance, the model is valid only if `PatientId`, `TherapistId`, and `RoomId` are each greater than or equal to 1. If any of these fields is 0 or negative, model validation SHALL report an error for that field.

**Validates: Requirements 8.1, 8.2, 8.3**

### Property 2: UpdateTherapistRequest required name validation

*For any* `UpdateTherapistRequest` instance, the model is valid only if `FirstName` and `LastName` are each non-empty (length >= 1). If either field is empty or null, model validation SHALL report an error for that field.

**Validates: Requirements 9.1, 9.2**

### Property 3: Password policy acceptance

*For any* password string, the non-production password policy accepts the password if and only if it has length >= 8, contains at least one uppercase letter, contains at least one digit, and contains at least one non-alphanumeric character. A password missing any one of these criteria SHALL be rejected.

**Validates: Requirements 11.1, 11.2, 11.3, 11.4**

### Property 4: Cancellation request patient filtering

*For any* set of cancellation requests belonging to various patients, when a user in the Patient role queries the cancellation requests endpoint, the returned list SHALL contain only requests where the patient's email matches the authenticated user's email. No request belonging to a different patient SHALL appear in the result.

**Validates: Requirements 7.3**

## Error Handling

| Scenario | HTTP Status | Response Body |
|---|---|---|
| Unauthenticated request to protected endpoint | 401 Unauthorized | Default ASP.NET challenge response (redirect to `/login` for browser, 401 for API clients) |
| Authenticated user lacks required role | 403 Forbidden | Default ASP.NET forbid response |
| Patient requests another patient's record | 403 Forbidden | Default ASP.NET forbid response |
| Invalid DTO (missing required field, out-of-range value) | 400 Bad Request | `ValidationProblemDetails` with field-level errors |
| Missing or invalid antiforgery token on login/logout | 400 Bad Request | Default antiforgery validation failure |
| Password does not meet policy on registration/change | 400 Bad Request | Identity error list from `IdentityResult.Errors` |

### Error Handling Design Decisions

- **No custom error responses for auth failures**: ASP.NET's default 401/403 behavior is appropriate. The cookie authentication scheme redirects browsers to `/login` and returns 401 for API-style requests (based on the `Accept` header). No custom `IAuthorizationMiddlewareResultHandler` is needed.
- **Validation errors use ProblemDetails**: The `[ApiController]` attribute already enables automatic 400 responses with `ValidationProblemDetails` when `ModelState` is invalid. No additional error handling code is needed in the controllers.

## Testing Strategy

### Unit Tests (Example-Based)

Unit tests cover the finite authorization matrix and specific edge cases. These use `WebApplicationFactory<Program>` for integration-style tests against the real middleware pipeline.

**Authorization enforcement tests** (Requirements 1–7):
- For each controller, test that unauthenticated requests return 401
- For each role × endpoint × HTTP method combination in the access matrix, test the expected status code (200/201/204 for allowed, 403 for denied)
- Test the Patient owner-access pattern: own record returns 200, other patient's record returns 403
- Test the cancellation request filtering: Patient sees only own requests, Staff sees all

**CSRF protection tests** (Requirement 10):
- Test that POST to `/account/login` without antiforgery token returns 400
- Test that POST to `/account/logout` without antiforgery token returns 400
- Test that POST with valid antiforgery token succeeds

**Password policy configuration test** (Requirement 11.5):
- Test that in production environment, `RequiredLength` is 10

### Property-Based Tests (FsCheck + xUnit)

Property tests cover the DTO validation and password policy where the input space is large. The project already uses FsCheck 3.3.2 with xUnit.

**Configuration**: Minimum 100 iterations per property test.

| Property | Test Description | Tag |
|---|---|---|
| Property 1 | Generate random `UpdateAppointmentRequest` with varying PatientId/TherapistId/RoomId values. Validate that model validation fails when any ID < 1 and passes when all IDs >= 1. | `Feature: api-authorization-security, Property 1: UpdateAppointmentRequest ID field validation` |
| Property 2 | Generate random `UpdateTherapistRequest` with varying FirstName/LastName strings. Validate that model validation fails when either name is empty and passes when both are non-empty. | `Feature: api-authorization-security, Property 2: UpdateTherapistRequest required name validation` |
| Property 3 | Generate random password strings. Validate that the password policy accepts if and only if length >= 8, has uppercase, has digit, and has non-alphanumeric character. | `Feature: api-authorization-security, Property 3: Password policy acceptance` |
| Property 4 | Generate random sets of cancellation requests with various patient emails. Filter as the controller would for a given patient email. Verify the result contains only matching requests. | `Feature: api-authorization-security, Property 4: Cancellation request patient filtering` |

### Test Organization

- Authorization and CSRF integration tests: `ClinicScheduler.Core.Tests/ApiAuthorizationTests.cs`
- DTO validation property tests: `ClinicScheduler.Core.Tests/DtoValidationPropertyTests.cs`
- Password policy property tests: `ClinicScheduler.Core.Tests/PasswordPolicyPropertyTests.cs`
- Cancellation filtering property test: `ClinicScheduler.Core.Tests/CancelRequestFilterPropertyTests.cs`
