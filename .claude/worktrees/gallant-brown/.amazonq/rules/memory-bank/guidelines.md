# Development Guidelines

## Code Style & Formatting
- **Nullable reference types enabled** (`<Nullable>enable</Nullable>`) — always handle nullability explicitly
- **Implicit usings enabled** — no need to add `using System;` etc. manually
- String properties default to `string.Empty`, not `null`, unless explicitly nullable
- `init`-only properties on DTOs/records (immutable after construction)
- `sealed` classes for DTOs: `public sealed class AppointmentDto`

## Naming Conventions
- PascalCase for classes, properties, methods, namespaces
- `_camelCase` for private fields (e.g., `_fixture`, `_postgres`)
- Test method names follow `MethodName_Condition_ExpectedResult` pattern (e.g., `ValidateSlot_OnWeekend_ThrowsArgumentException`)
- Suffix `Async` on all async methods
- DTOs named `<Entity>Dto`, requests named `Create<Entity>Request` / `Update<Entity>Request`
- Test helper classes: `SeedData` (static), `WebAppFixture` (shared fixture)

## Project & Namespace Structure
- Namespace mirrors folder path: `ClinicScheduler.Web.Contracts.Appointments`, `ClinicScheduler.Web.Tests.Api`, etc.
- Contracts (DTOs) live in `ClinicScheduler.Web/Contracts/<Entity>/` — one folder per domain entity
- Each contract folder contains: `<Entity>Dto.cs`, `Create<Entity>Request.cs`, `Update<Entity>Request.cs`

## API Design Patterns
- REST controllers under `/api/` prefix
- JSON enums serialized as strings via `JsonStringEnumConverter` (registered globally)
- OpenAPI schema transformer ensures enum schemas use string type
- HTTP status codes: 201 Created (POST success), 204 No Content (PUT/DELETE), 400 Bad Request (validation), 404 Not Found, 409 Conflict (business rule violation)
- Business rule violations (conflicts, capacity) → `InvalidOperationException` → 409
- Entity-not-found → `ArgumentException` → 400 or 404 depending on context

## DTO Documentation
- All DTO properties have XML `<summary>` and `<example>` tags for OpenAPI generation:
```csharp
/// <summary>Appointment start time (UTC).</summary>
/// <example>2024-03-01T09:00:00Z</example>
public DateTime StartTime { get; init; }
```
- `<GenerateDocumentationFile>true</GenerateDocumentationFile>` is set in the web project

## Business Rules (enforce in AppointmentSchedulingService)
- Appointments: weekdays only, 8:00 AM–5:00 PM window, 30-minute slots only
- Max 12 concurrent patients per time slot clinic-wide
- No therapist, room, or patient double-booking at the same slot
- Missed appointments are auto-rescheduled to the next available slot

## Dependency Injection Patterns
```csharp
// Generic repository — registered once for all entity types
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Business services — scoped
builder.Services.AddScoped<AppointmentSchedulingService>();

// Platform abstraction — singleton
builder.Services.AddSingleton<IFormFactor, FormFactor>();

// UI state — scoped
builder.Services.AddScoped<SessionState>();
builder.Services.AddScoped<ClinicDataStore>();
```

## Testing Patterns

### Unit Tests (ClinicScheduler.Web.Tests/Unit/)
- Use `Moq` to mock `IRepository<T>` dependencies
- Use `FluentAssertions` for all assertions (`Should().Be()`, `Should().Throw<>()`, `Should().NotThrow()`)
- `[Theory]` + `[InlineData]` for parameterized cases; `[Fact]` for single cases
- Build SUT via private static `BuildSut(...)` helper
- Build mocks via private static `BuildMocks()` returning a tuple
- Static test data as `private static readonly` fields

```csharp
private static AppointmentSchedulingService BuildSut(
    Mock<IRepository<Appointment>> apptRepo, ...) =>
    new(apptRepo.Object, ...);
```

### Integration Tests (ClinicScheduler.Web.Tests/Api/)
- `[Collection("WebApp")]` attribute ties tests to `WebAppFixture`
- `IAsyncLifetime.InitializeAsync()` calls `_fixture.ResetDatabaseAsync()` to clean DB before each test class
- Use `_fixture.Client.PostAsJsonAsync(...)` / `GetAsync(...)` for HTTP calls
- Parse responses with `JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())`
- `SeedData` static helpers create entities via API calls and return their IDs
- Use unique `suffix` strings per test to avoid cross-test data collisions

### Test Infrastructure
- `WebAppFixture` starts a real `PostgreSqlContainer` (Testcontainers) once per collection
- Replaces `DbContextOptions<ClinicDbContext>` registration to point at the test container
- `ResetDatabaseAsync()` truncates all tables with `RESTART IDENTITY CASCADE` in FK-safe order
- `WebApplicationFactory<Program>` with `UseEnvironment("Testing")` skips connection string validation

## Blazor / JS Patterns
- Co-located JS files: `ComponentName.razor.js` alongside the `.razor` file
- JS uses vanilla DOM APIs and `Blazor.reconnect()` / `Blazor.resumeCircuit()` for circuit management
- `IFormFactor` interface implemented differently per host: Web returns `"Web"` + `Environment.OSVersion`; WASM client has its own implementation
- `_Imports.razor` in each project for shared using directives

## Docker / Deployment
- Multi-stage Dockerfile: restore only web project's dependency tree (excludes MAUI) before copying full source
- Production: HTTPS terminated at load balancer; app runs HTTP on port 8080 (`ASPNETCORE_URLS=http://+:8080`)
- `ASPNETCORE_ENVIRONMENT=Production` set in `docker-compose.yml`
- App waits for DB health check (`condition: service_healthy`) before starting
