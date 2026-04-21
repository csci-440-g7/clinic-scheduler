# Bugfix Requirements Document

## Introduction

After adding `[Authorize]` attributes to all API controllers as part of the `api-authorization-security` spec, 43 integration tests in the `ClinicScheduler.Web.Tests` project now fail. The tests use `WebAppFixture.Client`, an `HttpClient` created by `WebApplicationFactory<Program>` that sends unauthenticated HTTP requests. Because cookie authentication is configured with `LoginPath = "/login"`, unauthenticated requests are redirected to the login page. For POST, PUT, and DELETE methods this results in HTTP 405 Method Not Allowed (the login page only accepts GET). For GET requests, the tests receive HTML from the login page instead of the expected JSON API response.

The fix requires configuring a test authentication handler in the `WebAppFixture` so that all requests made by the test `HttpClient` are automatically authenticated as a user with the Admin role, which has access to all endpoints. This is the standard ASP.NET Core approach for integration testing with authorization — registering a fake authentication scheme in the `WebApplicationFactory` that creates a `ClaimsPrincipal` with the needed claims and roles, bypassing real login entirely.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN any integration test in `ClinicScheduler.Web.Tests` sends a POST, PUT, or DELETE request to a protected API endpoint via `WebAppFixture.Client` THEN the system redirects to `/login` (a GET-only page) and returns HTTP 405 Method Not Allowed instead of the expected success status code.

1.2 WHEN any integration test sends a GET request to a protected API endpoint via `WebAppFixture.Client` THEN the system redirects to `/login` and returns the login page HTML with HTTP 200 instead of the expected JSON API response, causing assertion failures.

1.3 WHEN the `SeedData` helper methods (e.g., `CreatePatientAsync`, `CreateTherapistAsync`, `CreateLocationAsync`, `CreateRoomAsync`, `CreateTherapyTypeAsync`) send POST requests to create seed entities THEN those requests also fail with HTTP 405 due to the redirect, causing `EnsureSuccessStatusCode()` to throw and cascading failures across all tests that depend on seeded data.

### Expected Behavior (Correct)

2.1 WHEN any integration test sends a POST, PUT, or DELETE request to a protected API endpoint via `WebAppFixture.Client` THEN the system SHALL authenticate the request as a user with the Admin role and process the request normally, returning the expected success status code (201, 204, 200, etc.).

2.2 WHEN any integration test sends a GET request to a protected API endpoint via `WebAppFixture.Client` THEN the system SHALL authenticate the request as a user with the Admin role and return the expected JSON API response with HTTP 200.

2.3 WHEN the `SeedData` helper methods send POST requests to create seed entities THEN those requests SHALL be authenticated and succeed, allowing dependent tests to run with properly seeded data.

### Unchanged Behavior (Regression Prevention)

3.1 WHEN the integration tests exercise business logic rules (e.g., 30-minute appointment slots, weekday-only scheduling, 8am–5pm window, 12-patient cap) THEN the system SHALL CONTINUE TO enforce those rules and return the same validation error responses as before authorization was added.

3.2 WHEN the integration tests create, read, update, and delete entities through the API THEN the system SHALL CONTINUE TO persist and retrieve data correctly using the Testcontainers PostgreSQL database.

3.3 WHEN the `WebAppFixture` starts up THEN the system SHALL CONTINUE TO provision a PostgreSQL container, apply EF migrations, seed the database, and provide a working `HttpClient` to all test classes in the "WebApp" collection.

3.4 WHEN the `ResetDatabaseAsync` method is called between test classes THEN the system SHALL CONTINUE TO truncate all data tables so each test class starts with a clean database.

3.5 WHEN the production application runs (outside of the test environment) THEN the system SHALL CONTINUE TO require real cookie-based authentication for all protected API endpoints — the test authentication handler must only be active in the test host.

---

### Bug Condition

```pascal
FUNCTION isBugCondition(X)
  INPUT: X of type HttpRequest sent by WebAppFixture.Client
  OUTPUT: boolean
  
  // Returns true when the request targets a protected API endpoint
  // and the HttpClient has no authentication configured
  RETURN X.TargetEndpoint.HasAuthorizeAttribute = true
     AND X.Client.AuthenticationScheme = none
END FUNCTION
```

### Fix Checking Property

```pascal
// Property: Fix Checking — Authenticated test requests reach the API action
FOR ALL X WHERE isBugCondition(X) DO
  result ← SendRequest_WithTestAuth(X)
  ASSERT result.StatusCode ≠ 405
     AND result.StatusCode ≠ 302 (redirect to /login)
     AND result.ContentType = "application/json" (for non-204 responses)
END FOR
```

### Preservation Checking Property

```pascal
// Property: Preservation Checking — Non-auth behavior unchanged
FOR ALL X WHERE NOT isBugCondition(X) DO
  ASSERT F(X) = F'(X)
END FOR
// Specifically: business rule validations, CRUD operations, database seeding,
// container lifecycle, and production authentication all remain identical.
```