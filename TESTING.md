# Testing Strategy — ClinicScheduler

## Overview

The test suite is organized into three layers that together cover domain logic, business rules, and the full HTTP/database stack.

| Layer | Project | Framework | Count |
|---|---|---|---|
| Entity unit tests | `ClinicScheduler.Core.Tests` | xUnit + FluentAssertions | 69 |
| Service unit tests | `ClinicScheduler.Web.Tests/Unit/` | xUnit + Moq + FluentAssertions | 24 |
| API integration tests | `ClinicScheduler.Web.Tests/Api/` | xUnit + Testcontainers + WebApplicationFactory | 53 |
| **Total** | | | **146** |

Run the full suite:

```bash
dotnet test ClinicScheduler/ClinicScheduler.Core.Tests
dotnet test ClinicScheduler/ClinicScheduler.Web.Tests
```

---

## Layer 1 — Entity Unit Tests (`ClinicScheduler.Core.Tests`)

### Purpose
Verify that domain entity constructors, state machines, and validation rules work in isolation — no database, no mocks, no HTTP.

### Location
```
ClinicScheduler/ClinicScheduler.Core.Tests/Entities/
  AppointmentTests.cs
  PatientTests.cs
  TherapistTests.cs
  TherapyTypeTests.cs
  TreatmentPlanTests.cs
```

### What's Covered
- Entity construction with valid and invalid arguments
- `Appointment` status transitions: `Cancel()`, `Complete()`, `MarkAsMissed()`, `Reschedule()`
- Guard clauses that prevent illegal state transitions (e.g., canceling an already-completed appointment)
- `TreatmentPlan` check constraints: `FrequencyPerWeek ∈ {2,3,4}`, `TotalDays ∈ {20,30,50}`
- `Patient` and `Therapist` contact info mutation via `UpdateContactInfo()` / `UpdateDetails()`

### Patterns
- `[Fact]` for single-case tests, `[Theory]` + `[InlineData]` for parameterized cases
- FluentAssertions: `.Should().Throw<T>()`, `.Should().Be()`, `.Should().NotThrow()`
- No mocking — entities are pure C# with no external dependencies

---

## Layer 2 — Service Unit Tests (`ClinicScheduler.Web.Tests/Unit/`)

### Purpose
Verify `AppointmentSchedulingService` business rules in isolation by mocking `IRepository<T>` dependencies with Moq.

### Location
```
ClinicScheduler/ClinicScheduler.Web.Tests/Unit/
  AppointmentSchedulingServiceTests.cs
```

### What's Covered

| Rule | Tests |
|---|---|
| Weekdays only (Mon–Fri) | `ValidateSlot_OnWeekend_ThrowsArgumentException` (Saturday, Sunday) |
| 30-minute boundaries only | `ValidateSlot_NonHalfHourBoundary_ThrowsArgumentException` (3 cases) |
| 8:00 AM start minimum | `ValidateSlot_Before8am_ThrowsArgumentException` |
| 5:00 PM end maximum | `ValidateSlot_At5pm_ThrowsArgumentException` |
| Valid slots pass | `ValidateSlot_ValidWeekdaySlot_DoesNotThrow` (5 boundary cases) |
| Patient not found | `CreateAppointmentAsync_PatientNotFound_ThrowsArgumentException` |
| Therapist not found | `CreateAppointmentAsync_TherapistNotFound_ThrowsArgumentException` |
| Room not found | `CreateAppointmentAsync_RoomNotFound_ThrowsArgumentException` |
| Therapist double-booking | `CreateAppointmentAsync_TherapistConflict_ThrowsInvalidOperationException` |
| Room double-booking | `CreateAppointmentAsync_RoomConflict_ThrowsInvalidOperationException` |
| Patient double-booking | `CreateAppointmentAsync_PatientDoubleBooked_ThrowsInvalidOperationException` |
| 12-patient cap enforced | `CreateAppointmentAsync_CapacityExceeded_ThrowsInvalidOperationException` |
| 11 patients allowed (boundary) | `CreateAppointmentAsync_ElevenConcurrentPatients_Succeeds` |
| Happy-path creation | `CreateAppointmentAsync_NoConflicts_ReturnsScheduledAppointment` |
| Reschedule requires Missed status | `RescheduleAfterMissedAsync_NotMissedStatus_ThrowsArgumentException` |

### Patterns

```csharp
// System Under Test factory — all tests use this
private static AppointmentSchedulingService BuildSut(
    Mock<IRepository<Appointment>> apptRepo,
    Mock<IRepository<Patient>> patientRepo,
    Mock<IRepository<Therapist>> therapistRepo,
    Mock<IRepository<Room>> roomRepo) =>
    new(apptRepo.Object, patientRepo.Object, therapistRepo.Object, roomRepo.Object);

// Mock factory — returns a tuple for easy destructuring
private static (...) BuildMocks() => (new(), new(), new(), new());
```

- Test method names follow `MethodName_Condition_ExpectedResult`
- Static `private static readonly` fields for shared test data (e.g., `ValidSlot`, `Patient1`)
- `[Theory]` + `[InlineData]` for boundary conditions on `ValidateSlot`

---

## Layer 3 — API Integration Tests (`ClinicScheduler.Web.Tests/Api/`)

### Purpose
Test the full HTTP stack — controller routing, request parsing, business rule enforcement, database writes, and response shape — against a real PostgreSQL database.

### Infrastructure: `WebAppFixture`

The fixture (`ClinicScheduler.Web.Tests/Fixtures/WebAppFixture.cs`) wires up the real app and a throwaway database for the entire test collection:

1. Starts a `postgres:17-alpine` container via **Testcontainers** (`PostgreSqlContainer`)
2. Boots the ASP.NET Core app using **`WebApplicationFactory<Program>`** with `UseEnvironment("Testing")`
3. Replaces the production `DbContextOptions<ClinicDbContext>` registration to point at the container
4. Disables the fallback authorization policy so API endpoints are accessible without a login cookie
5. Exposes `HttpClient Client` for making HTTP calls

```csharp
[CollectionDefinition("WebApp")]
public class WebAppCollection : ICollectionFixture<WebAppFixture> { }
```

All API test classes carry `[Collection("WebApp")]` — this ensures **one container and one app instance** are shared across the entire suite, keeping startup cost low.

### Database Reset

Each test class calls `ResetDatabaseAsync()` in `IAsyncLifetime.InitializeAsync()`:

```csharp
public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
```

`ResetDatabaseAsync()` truncates all tables with `RESTART IDENTITY CASCADE` in FK-safe order, so every test class begins with a completely empty database and auto-increment IDs reset to 1. This isolates test classes from each other without the overhead of restarting the container.

### Seed Helpers (`SeedData`)

Static helper methods in `SeedData` create prerequisite entities via the API and return their IDs:

```csharp
var (patientId, therapistId, roomId, locationId) =
    await SeedData.SeedCoreEntitiesAsync(_fixture.Client, suffix: "mytest");
```

Each helper accepts a `suffix` string appended to names and email addresses, preventing unique-constraint collisions between tests that run in the same database reset cycle.

### Test Files and Coverage

| File | Tests | Key Scenarios |
|---|---|---|
| `AppointmentsApiTests.cs` | 21 | Happy path, 30-min rule, weekday rule, 8am–5pm window, 12-patient cap, conflict detection, mark-missed + auto-reschedule, status transitions |
| `LocationsAndRoomsApiTests.cs` | varies | Location CRUD, room CRUD, `GET /rooms/location/{id}` |
| `PatientsApiTests.cs` | varies | Patient CRUD, 404 on unknown ID |
| `TherapistsApiTests.cs` | varies | Therapist CRUD |
| `TherapyTypesApiTests.cs` | varies | TherapyType CRUD |
| `TreatmentPlansApiTests.cs` | varies | Create with therapy types, invalid duration rejected, valid durations (20/30/50 days) |

### HTTP Status Code Expectations

| Scenario | Expected |
|---|---|
| Successful create | `201 Created` |
| Successful update / delete | `204 No Content` |
| Validation failure (bad input, wrong duration, weekend) | `400 Bad Request` |
| Entity not found | `404 Not Found` |
| Business rule violation (conflict, capacity) | `409 Conflict` |

### Example Integration Test

```csharp
[Fact]
public async Task POST_ThirteenthConcurrentPatient_Returns409()
{
    var locationId = await SeedData.CreateLocationAsync(_fixture.Client);

    for (var i = 1; i <= 12; i++)
    {
        var p = await SeedData.CreatePatientAsync(_fixture.Client, $"full{i}");
        var t = await SeedData.CreateTherapistAsync(_fixture.Client, $"full{i}");
        var r = await SeedData.CreateRoomAsync(_fixture.Client, locationId, $"full{i}");
        (await BookAsync(p, t, r, Slot9am)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    var p13 = await SeedData.CreatePatientAsync(_fixture.Client, "full13");
    var t13 = await SeedData.CreateTherapistAsync(_fixture.Client, "full13");
    var r13 = await SeedData.CreateRoomAsync(_fixture.Client, locationId, "full13");
    var response13 = await BookAsync(p13, t13, r13, Slot9am);

    response13.StatusCode.Should().Be(HttpStatusCode.Conflict);
    (await response13.Content.ReadAsStringAsync()).Should().Contain("12 concurrent patients");
}
```

---

## Known Gaps

| Gap | Impact | Status | Notes |
|---|---|---|---|
| `RescheduleAfterMissedAsync` success path not unit-tested | Low | ✅ Fixed | `RescheduleAfterMissedAsync_NoConflicts_ReturnsAppointmentAfterMissedSlot` added — asserts correct patient/therapist/room, start time after missed slot, same-time-of-day preference |
| No test for 30-day reschedule search boundary | Low | ✅ Fixed | `RescheduleAfterMissedAsync_NoSlotIn30Days_ThrowsInvalidOperationException` added — mocks therapist booked solid, asserts `InvalidOperationException` |
| No load / concurrency tests | Medium | ⚠️ Open | Race condition on the 12-patient cap is not exercised; would require parallel HTTP requests in an integration test |

---

## Running Tests Locally

Docker must be running — Testcontainers pulls and starts `postgres:17-alpine` automatically.

```bash
# All entity tests
dotnet test ClinicScheduler/ClinicScheduler.Core.Tests

# Unit tests only (no Docker required)
dotnet test ClinicScheduler/ClinicScheduler.Web.Tests --filter "FullyQualifiedName~Unit"

# Integration tests only (Docker required)
dotnet test ClinicScheduler/ClinicScheduler.Web.Tests --filter "FullyQualifiedName~Api"

# Full web test suite
dotnet test ClinicScheduler/ClinicScheduler.Web.Tests
```
