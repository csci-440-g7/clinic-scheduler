# ClinicScheduler

A web-based clinic scheduling system built with ASP.NET Core, Blazor, and PostgreSQL. Designed for physical therapy clinics to manage patients, therapists, appointments, treatment plans, and scheduling workflows.

## Tech Stack

- **Backend:** ASP.NET Core 10, Entity Framework Core, PostgreSQL
- **Frontend:** Blazor (Interactive Server + WebAssembly), MudBlazor component library
- **Auth:** ASP.NET Core Identity with role-based access control
- **Testing:** xUnit, FluentAssertions, FsCheck (property-based), Moq, Testcontainers

## Project Structure

| Project | Purpose |
|---|---|
| `ClinicScheduler.Core` | Domain entities, interfaces, and business logic services |
| `ClinicScheduler.Infrastructure` | EF Core DbContext, migrations, and repository implementation |
| `ClinicScheduler.Shared` | Blazor pages, components, and shared layout |
| `ClinicScheduler.Web` | ASP.NET Core host, API controllers, Identity config, background services |
| `ClinicScheduler.Web.Client` | Blazor WebAssembly client project |
| `ClinicScheduler.Core.Tests` | Unit and property-based tests for domain logic |
| `ClinicScheduler.Web.Tests` | Integration tests using Testcontainers (PostgreSQL) |

## Features

- **Appointment Management** — Book, reschedule, cancel, and track appointments with conflict detection
- **Patient & Therapist Management** — CRUD operations with role-based views
- **Treatment Plans** — Configurable frequency and therapy type assignments
- **Scheduling Conflict Detection** — Double-booking, capacity, and operating hours validation
- **Calendar View** — Visual scheduling interface with sidebar navigation
- **Notifications** — In-app notifications for appointment events (created, rescheduled, canceled, missed)
- **Audit Logging** — Automatic change tracking via EF Core SaveChanges override
- **Reports** — Clinic reporting dashboard
- **Role-Based Access** — Admin, Clinic Manager, Staff, Therapist, and Patient roles
- **Missed Appointment Handling** — Automatic rescheduling with reminder background service

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/) (local instance or Docker)

## Getting Started

1. **Clone the repository**

   ```bash
   git clone https://github.com/csci-440-g7/clinic-scheduler.git
   cd clinic-scheduler/ClinicScheduler
   ```

2. **Set up PostgreSQL**

   Create a database named `clinic_scheduler` with a `postgres` user, or update the connection string in `ClinicScheduler.Web/appsettings.Development.json`.

3. **Configure secrets**

   The app requires a seed admin password. Set it via user secrets:

   ```bash
   cd ClinicScheduler.Web
   dotnet user-secrets set "SeedAdmin:Password" "YourSecurePassword1!"
   ```

4. **Run the application**

   ```bash
   dotnet run --project ClinicScheduler.Web
   ```

   EF Core migrations run automatically on startup in Development mode.

5. **Access the app**

   Open `https://localhost:5001` (or the port shown in console output).

   Swagger UI is available at `/swagger` in Development mode.

## Running Tests

```bash
# Unit and property-based tests (no database required)
dotnet test ClinicScheduler.Core.Tests

# Integration tests (requires Docker for Testcontainers)
dotnet test ClinicScheduler.Web.Tests
```

## API

REST endpoints are available under `/api/` for:

- `/api/appointments` — CRUD + reschedule/cancel workflows
- `/api/patients` — Patient management
- `/api/therapists` — Therapist management
- `/api/locations` — Location management
- `/api/rooms` — Room management
- `/api/therapytypes` — Therapy type catalog
- `/api/treatmentplans` — Treatment plan management
- `/api/cancelappointmentrequests` — Cancellation request workflow
- `/api/account` — Authentication (login/logout/register)

OpenAPI spec is served at `/openapi/v1.json` in Development mode.
