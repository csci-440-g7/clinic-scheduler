# Project Structure

## Solution Layout
```
gallant-brown/
├── ClinicScheduler/
│   ├── ClinicScheduler.Core/           # Domain entities, interfaces, business services
│   ├── ClinicScheduler.Infrastructure/ # EF Core DbContext, Repository implementation, migrations
│   ├── ClinicScheduler.Shared/         # Shared Razor components, IFormFactor, SessionState, ClinicDataStore
│   ├── ClinicScheduler.Web/            # ASP.NET Core host: API controllers, Contracts (DTOs), server-side Blazor
│   ├── ClinicScheduler.Web.Client/     # Blazor WebAssembly client: Layout, client-side services
│   └── ClinicScheduler.Web.Tests/      # xUnit integration + unit tests
├── docker-compose.yml                  # App + PostgreSQL containers
├── Dockerfile                          # Multi-stage build (SDK → ASP.NET runtime)
└── gallant-brown.sln
```

## Project Responsibilities

| Project | Role |
|---|---|
| `ClinicScheduler.Core` | Entities (`Appointment`, `Patient`, `Therapist`, `Room`, `Location`, `TreatmentPlan`, `TherapyType`), `IRepository<T>`, `AppointmentSchedulingService`, `MissedAppointmentService` |
| `ClinicScheduler.Infrastructure` | `ClinicDbContext` (EF Core + Npgsql), `Repository<T>` generic implementation, EF migrations |
| `ClinicScheduler.Shared` | `IFormFactor`, `SessionState`, `ClinicDataStore`, shared Razor components/pages |
| `ClinicScheduler.Web` | `Program.cs` (DI wiring, middleware), API controllers, `Contracts/` DTOs, server `FormFactor` |
| `ClinicScheduler.Web.Client` | WASM `Program.cs`, `ReconnectModal`, client `FormFactor` |
| `ClinicScheduler.Web.Tests` | `WebAppFixture` (Testcontainers PostgreSQL + `WebApplicationFactory`), `SeedData`, API integration tests, unit tests |

## Key Architectural Patterns
- **Clean Architecture**: Core has no external dependencies; Infrastructure and Web depend on Core
- **Generic Repository**: `IRepository<T>` / `Repository<T>` abstracts all data access
- **DTO / Contract layer**: `Contracts/` folder in `ClinicScheduler.Web` holds request/response types separate from domain entities
- **Blazor Auto render mode**: Server-side rendering with WebAssembly fallback; `IFormFactor` abstracts platform detection
- **API Controllers**: REST endpoints under `/api/` prefix; JSON enums serialized as strings
- **EF Core auto-migration**: `db.Database.Migrate()` runs on startup

## API Endpoints (inferred from tests)
- `GET/POST /api/appointments`, `GET/PUT/DELETE /api/appointments/{id}`
- `POST /api/appointments/{id}/mark-missed`
- `GET/POST /api/patients`, `GET/PUT/DELETE /api/patients/{id}`
- `GET/POST /api/therapists`, `GET/PUT/DELETE /api/therapists/{id}`
- `GET/POST /api/locations`, `GET/PUT/DELETE /api/locations/{id}`
- `GET/POST /api/rooms`, `GET/PUT/DELETE /api/rooms/{id}`
- `GET/POST /api/therapytypes`, `GET/PUT/DELETE /api/therapytypes/{id}`
- `GET/POST /api/treatmentplans`, `GET/PUT/DELETE /api/treatmentplans/{id}`
