# Fix Web Tests Auth — Bugfix Design

## Overview

After `[Authorize]` attributes were added to all API controllers, the 43 integration tests in `ClinicScheduler.Web.Tests` fail because `WebAppFixture.Client` sends unauthenticated HTTP requests. The cookie authentication middleware redirects these requests to `/login`, producing HTTP 405 for non-GET methods and HTML instead of JSON for GET methods.

The fix registers a fake `AuthenticationHandler<AuthenticationSchemeOptions>` inside `WebApplicationFactory.WithWebHostBuilder` that automatically authenticates every request as a `ClaimsPrincipal` with the `Admin` role. This is the standard ASP.NET Core pattern for integration testing with authorization — it bypasses real login while keeping the authorization pipeline active so role-based `[Authorize]` attributes are still evaluated.

## Glossary

- **Bug_Condition (C)**: An HTTP request sent by `WebAppFixture.Client` to an endpoint decorated with `[Authorize]`, where no authentication scheme is configured in the test host — causing a redirect to `/login` instead of processing the request.
- **Property (P)**: Every HTTP request from the test client is authenticated as an Admin user and reaches the target controller action, returning the expected status code and JSON content.
- **Preservation**: All existing test behavior — business rule validation, CRUD operations, database seeding, container lifecycle, and table truncation — remains unchanged after the fix.
- **WebAppFixture**: The shared xUnit fixture in `Fixtures/WebAppFixture.cs` that provisions a PostgreSQL Testcontainer, boots the ASP.NET Core app via `WebApplicationFactory<Program>`, and exposes an `HttpClient` to all test classes in the `"WebApp"` collection.
- **TestAuthHandler**: A custom `AuthenticationHandler<AuthenticationSchemeOptions>` registered only in the test host that creates a `ClaimsPrincipal` with `Admin` role claims for every request.
- **SeedData**: Static helper class that POSTs entities (patients, therapists, locations, rooms, therapy types) via the API and returns their IDs. Fails first when auth is missing because `EnsureSuccessStatusCode()` throws on 405 responses.

## Bug Details

### Bug Condition

The bug manifests when any integration test sends an HTTP request through `WebAppFixture.Client` to an API endpoint that has an `[Authorize]` attribute. The cookie authentication middleware sees no authentication cookie, so it redirects to `/login`. For POST/PUT/DELETE this yields 405 (the login page only accepts GET). For GET this yields 200 with HTML content instead of JSON.

**Formal Specification:**
```
FUNCTION isBugCondition(input)
  INPUT: input of type HttpRequestMessage sent by WebAppFixture.Client
  OUTPUT: boolean

  RETURN input.RequestUri.StartsWithApiRoute = true
     AND TargetEndpoint(input).HasAuthorizeAttribute = true
     AND input.Headers.Contains("Cookie") = false
     AND TestHost.AuthenticationScheme = "Cookies" (default, no test override)
END FUNCTION
```

### Examples

- **POST /api/patients** with a JSON body → Expected: 201 Created with JSON. Actual: 302 redirect to `/login`, then 405 Method Not Allowed.
- **GET /api/therapists** → Expected: 200 OK with JSON array. Actual: 302 redirect to `/login`, then 200 OK with HTML login page content.
- **DELETE /api/appointments/1** → Expected: 204 No Content. Actual: 302 redirect to `/login`, then 405 Method Not Allowed.
- **SeedData.CreatePatientAsync** → Calls POST /api/patients internally. Throws `HttpRequestException` from `EnsureSuccessStatusCode()` on the 405 response, cascading failure to every test that depends on seeded data.

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- Business rule validations (30-minute slots, weekday-only, 8am–5pm window, 12-patient cap) must continue to return 400/409 with appropriate error messages.
- CRUD operations (Create, Read, Update, Delete) for all entities must continue to persist and retrieve data correctly via the Testcontainers PostgreSQL database.
- `WebAppFixture.InitializeAsync` must continue to start a PostgreSQL container, apply EF migrations, seed the database, and produce a working `HttpClient`.
- `ResetDatabaseAsync` must continue to truncate all data tables between test classes.
- The production application must continue to require real cookie-based authentication — the test auth handler is scoped exclusively to the test `WebApplicationFactory`.

**Scope:**
All non-authentication aspects of the test infrastructure and application behavior should be completely unaffected by this fix. The only change is that the test `HttpClient` now carries an authenticated identity. This includes:
- Database provisioning and migration
- Entity Framework query and persistence behavior
- Controller business logic and validation
- JSON serialization settings
- Test collection ordering and parallelism

## Hypothesized Root Cause

Based on the bug description, the root cause is straightforward:

1. **Missing Test Authentication Scheme**: `WebAppFixture` creates a `WebApplicationFactory<Program>` that inherits the production authentication configuration (ASP.NET Core Identity with cookie authentication). The test `HttpClient` never logs in, so every request to a `[Authorize]`-decorated endpoint is treated as unauthenticated.

2. **Cookie Authentication Redirect Behavior**: The production `ConfigureApplicationCookie` sets `LoginPath = "/login"`. When an unauthenticated request hits a protected endpoint, the cookie handler issues a 302 redirect to `/login`. The `HttpClient` follows the redirect, but `/login` is a Blazor page that only serves GET — so POST/PUT/DELETE methods get 405.

3. **No Test-Specific Auth Override**: The `WithWebHostBuilder` block in `WebAppFixture` only overrides the `DbContext` registration. It does not register a test authentication scheme or set a default authentication policy, so the full production auth pipeline runs in the test host.

4. **Timing**: This worked before the `api-authorization-security` spec because controllers had no `[Authorize]` attributes — anonymous requests reached the action methods directly.

## Correctness Properties

Property 1: Bug Condition — Authenticated Test Requests Reach API Actions

_For any_ HTTP request sent by `WebAppFixture.Client` to an endpoint with an `[Authorize]` attribute, the test authentication handler SHALL authenticate the request as a user with the `Admin` role, and the response SHALL have a non-redirect status code (not 302) and not 405, with `Content-Type: application/json` for responses that return a body.

**Validates: Requirements 2.1, 2.2, 2.3**

Property 2: Preservation — Business Rules and CRUD Behavior Unchanged

_For any_ authenticated test request that exercises business logic validation or CRUD operations, the fixed test infrastructure SHALL produce the same response status codes and body content as the application would for a legitimately authenticated Admin user — preserving all validation rules, error messages, and data persistence behavior.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4**

## Fix Implementation

### Changes Required

Assuming our root cause analysis is correct:

**File**: `ClinicScheduler/ClinicScheduler.Web.Tests/Fixtures/WebAppFixture.cs`

**Specific Changes**:

1. **Add a `TestAuthHandler` class**: Create a class inheriting from `AuthenticationHandler<AuthenticationSchemeOptions>` that overrides `HandleAuthenticateAsync` to return `AuthenticateResult.Success` with a `ClaimsPrincipal` containing:
   - `ClaimTypes.Name` = `"testadmin@clinic.com"`
   - `ClaimTypes.Role` = `"Admin"`
   - `ClaimTypes.NameIdentifier` = a stable test user ID

2. **Register the test authentication scheme**: In `WithWebHostBuilder` → `ConfigureServices`, call:
   - `services.AddAuthentication("TestScheme")` to set the default scheme
   - `.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { })` to register the handler

3. **Remove the default Identity authentication**: Remove or override the existing Identity authentication service registrations so the test scheme takes precedence. The `AddAuthentication("TestScheme")` call with `DefaultAuthenticateScheme` and `DefaultChallengeScheme` set to `"TestScheme"` will override the Identity defaults.

4. **No changes to test classes**: The existing test classes (`PatientsApiTests`, `TherapistsApiTests`, `AppointmentsApiTests`, etc.) and `SeedData` helpers require zero modifications — they already use `_fixture.Client` which will now carry the authenticated identity automatically.

5. **No changes to production code**: The `TestAuthHandler` and its registration exist only in the test project. The production `Program.cs` and controllers are untouched.

## Testing Strategy

### Validation Approach

The testing strategy follows a two-phase approach: first, confirm the bug exists on unfixed code by running the existing test suite, then verify the fix resolves all 43 failures while preserving business logic behavior.

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples that demonstrate the bug BEFORE implementing the fix. Confirm that the root cause is missing authentication in the test host.

**Test Plan**: Run the existing 43 integration tests on the UNFIXED code and observe the failure pattern. All failures should show either HTTP 405 (for POST/PUT/DELETE) or assertion failures on response content (for GET).

**Test Cases**:
1. **POST Patient Test**: `POST_Patient_Returns201WithId` fails with 405 (will fail on unfixed code)
2. **GET Therapists Test**: `GET_AllTherapists_Returns200WithList` fails — response is HTML not JSON (will fail on unfixed code)
3. **DELETE Appointment Test**: `DELETE_Appointment_Returns204` fails with 405 (will fail on unfixed code)
4. **SeedData Cascade Test**: Any test calling `SeedCoreEntitiesAsync` fails with `HttpRequestException` from `EnsureSuccessStatusCode()` (will fail on unfixed code)

**Expected Counterexamples**:
- All 43 tests fail with either 405 status codes or HTML content assertions
- Root cause confirmed: no authentication scheme override in test host

### Fix Checking

**Goal**: Verify that for all inputs where the bug condition holds, the fixed test infrastructure produces the expected behavior.

**Pseudocode:**
```
FOR ALL request WHERE isBugCondition(request) DO
  result := SendRequest_WithTestAuthHandler(request)
  ASSERT result.StatusCode ≠ 405
  ASSERT result.StatusCode ≠ 302
  ASSERT result.ContentType = "application/json" (for non-204 responses)
  ASSERT result.StatusCode IN expectedSuccessCodes(request)
END FOR
```

### Preservation Checking

**Goal**: Verify that for all inputs where the bug condition does NOT hold, the fixed test infrastructure produces the same result as the original application logic.

**Pseudocode:**
```
FOR ALL request WHERE NOT isBugCondition(request) DO
  ASSERT ApplicationLogic(request) = ApplicationLogic_WithTestAuth(request)
END FOR
```

**Testing Approach**: The existing 43 integration tests serve as the preservation suite. They cover business rule validations (invalid durations, weekend scheduling, time window violations, patient cap), CRUD operations across all entity types, and edge cases (not-found, cascading deletes). If all 43 tests pass after the fix, preservation is confirmed.

**Test Plan**: Run the full test suite after adding the `TestAuthHandler`. All 43 tests should pass with the same assertions they had before authorization was added.

**Test Cases**:
1. **Business Rule Preservation**: `POST_OnSaturday_Returns400`, `POST_60MinSlot_Returns400`, `POST_ThirteenthConcurrentPatient_Returns409` — verify validation errors still fire
2. **CRUD Preservation**: `POST_Patient_Returns201WithId`, `GET_AllTherapists_Returns200WithList`, `PUT_Patient_Returns204`, `DELETE_Therapist_Returns204` — verify standard operations work
3. **SeedData Preservation**: `SeedCoreEntitiesAsync` returns valid IDs without throwing — verify the test infrastructure pipeline works end-to-end
4. **Edge Case Preservation**: `GET_TherapistById_NotFound_Returns404`, `POST_MarkMissedOnNonExistentAppointment_Returns404` — verify not-found handling unchanged

### Unit Tests

- Verify `TestAuthHandler.HandleAuthenticateAsync` returns a success result with Admin role claims
- Verify the `ClaimsPrincipal` contains the expected `ClaimTypes.Name`, `ClaimTypes.Role`, and `ClaimTypes.NameIdentifier`

### Property-Based Tests

- Not applicable for this fix — the bug is in test infrastructure configuration, not in application logic with a variable input domain. The existing 43 integration tests provide comprehensive coverage across the input space.

### Integration Tests

- Run the full `ClinicScheduler.Web.Tests` suite (43 tests) — all should pass
- Verify that `SeedData` helper methods work for all entity types
- Verify that business rule tests still correctly reject invalid inputs
- Verify that the test auth handler does not interfere with `[AllowAnonymous]` endpoints (e.g., `AccountController`)
