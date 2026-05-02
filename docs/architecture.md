# Architecture Overview

## Layered Project Structure

ClinicScheduler follows a Clean Architecture layout with six projects in a single solution:

```
ClinicScheduler/
├── ClinicScheduler.Core            # Domain entities, enums, interfaces, services
├── ClinicScheduler.Infrastructure  # EF Core DbContext, repositories, data access
├── ClinicScheduler.Shared          # Razor components, pages, layouts (shared UI)
├── ClinicScheduler.Web             # ASP.NET Core host, API controllers, DI config
├── ClinicScheduler.Web.Client      # Blazor WebAssembly client project
├── ClinicScheduler (MAUI)          # .NET MAUI hybrid app (Android, iOS, macOS, Windows)
└── ClinicScheduler.Core.Tests      # xUnit + FsCheck property-based tests
```

### Dependency Flow

```
Web ──► Shared ──► Core
 │        │          ▲
 │        ▼          │
 │    Infrastructure─┘
 │
 ├──► Web.Client ──► Shared
 │
 └──► Core (direct reference for services)

MAUI ──► Shared ──► Core / Infrastructure
```

Dependencies point inward: `Core` has zero project references, `Infrastructure` depends only on `Core`, and the host projects (`Web`, `Web.Client`, `MAUI`) depend on the inner layers.

### Project Responsibilities

| Project | Responsibility |
|---------|---------------|
| **Core** | Domain entities (`Patient`, `Therapist`, `Appointment`, `TreatmentPlan`, `Location`, `Room`, `TimeSlot`, `ScheduleConflict`, etc.), enumerations, repository interfaces (`IRepository<T>`), and domain services (`AppointmentSchedulingService`, `MissedAppointmentService`). |
| **Infrastructure** | `ClinicDbContext` (EF Core + ASP.NET Core Identity), `Repository<T>` implementation, database seeding, migrations, and automatic audit logging. |
| **Shared** | All Razor pages and components (Home, Appointments, Patients, Therapists, Locations, Rooms, TreatmentPlans, TherapyTypes), `MainLayout`, shared services (`IFormFactor`), and static assets. |
| **Web** | ASP.NET Core host with `Program.cs` (DI registration, middleware pipeline), REST API controllers (`/api/*`), authentication/authorization config, OpenAPI/Swagger setup, and background services. |
| **Web.Client** | Blazor WebAssembly entry point. Shares UI components from `Shared` and runs interactively in the browser. |
| **MAUI** | .NET MAUI Blazor Hybrid app targeting Android, iOS, macOS, and Windows. Reuses the `Shared` UI layer via `BlazorWebView`. |

## Key Design Patterns

### Repository Pattern

All data access goes through `IRepository<T>`, defined in `Core`:

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
}
```

`Infrastructure` provides `Repository<T>`, a generic EF Core implementation registered as a scoped service.

### Domain Entities

Entities follow a consistent pattern:
- **Private parameterless constructor** for EF Core materialization.
- **Public constructor** with validation for application code.
- **`CreatedAt` / `UpdatedAt` timestamps** managed automatically.
- **Domain methods** for state transitions (e.g., `Appointment.Cancel()`, `TreatmentPlan.Suspend()`), keeping business rules inside the entity.

### Clean Architecture

Business logic lives in `Core` with no dependency on infrastructure or UI concerns. The `AppointmentSchedulingService` validates scheduling rules (time slots, capacity, conflicts) using repository interfaces, not EF Core directly. The outer layers (Web, Infrastructure) provide implementations and wire everything together via dependency injection.

### Automatic Audit Logging

`ClinicDbContext.SaveChangesAsync` intercepts all tracked entity changes (Added, Modified, Deleted) and creates `AuditLog` entries before persisting. This provides an immutable change trail without requiring callers to explicitly log changes.

## Technology Stack

| Category | Technology | Version |
|----------|-----------|---------|
| Runtime | .NET | 10.0 |
| Web Framework | ASP.NET Core | 10.0 |
| UI Framework | Blazor (Server + WebAssembly hybrid) | 10.0 |
| Component Library | MudBlazor | 9.0.0-preview.2 |
| ORM | Entity Framework Core | 10.0 |
| Database | PostgreSQL | 17 (Alpine) |
| Identity | ASP.NET Core Identity | 10.0 |
| Mobile | .NET MAUI Blazor Hybrid | 10.0 |
| API Docs | OpenAPI + Swashbuckle (Swagger UI) | 10.1.4 |
| Testing | xUnit, FsCheck, FluentAssertions, Moq | — |
| Containerization | Docker (multi-stage build) | — |

## Render Modes

The Blazor app uses a hybrid render mode:
- **Interactive Server** — components run on the server with SignalR for UI updates.
- **Interactive WebAssembly** — components can also run in the browser via WebAssembly.
- Both modes are registered in `Program.cs` and the `Web.Client` project provides the WASM entry point.

The MAUI project uses `BlazorWebView` to host the same Shared components natively on mobile and desktop platforms.
