# ClinicScheduler

East Texas A&M CSCI-440 Group 7 capstone — Pain Management Clinic Scheduler.

A web-based scheduling system for managing appointments at a pain management clinic, built with ASP.NET Core 10, Blazor, Entity Framework Core 10, and PostgreSQL.

---

## Features

- Schedule, update, cancel, and complete appointments
- Business rule enforcement: weekdays only, 8am–5pm window, 30-minute slots, max 12 concurrent patients
- Conflict detection: therapist, room, and patient double-booking prevention
- Auto-reschedule missed appointments to the next available slot
- Appointment request workflow (patient requests → staff approve/deny)
- Role-based access: Admin, ClinicManager, Therapist, Staff, Patient
- In-app notifications and audit logging
- REST API with OpenAPI/Swagger documentation
- Blazor interactive UI (Server + WebAssembly hybrid) using MudBlazor
- MAUI shell for Windows/macOS/mobile

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL and full-stack Docker run)
- Visual Studio 2022 (v17.12+) with workloads:
  - ASP.NET and web development
  - .NET Multi-platform App UI development (MAUI — only needed for the mobile/desktop shell)

---

## Project Structure

```
ClinicScheduler/
├── ClinicScheduler             — .NET MAUI Blazor Hybrid shell (Windows, macOS, iOS, Android)
├── ClinicScheduler.Core        — Domain entities, IRepository<T>, AppointmentSchedulingService
├── ClinicScheduler.Infrastructure — EF Core DbContext, Repository<T>, migrations
├── ClinicScheduler.Shared      — Shared Razor pages and components (used by Web + MAUI)
├── ClinicScheduler.Web         — ASP.NET Core host: API controllers, DTOs, server-side Blazor
├── ClinicScheduler.Web.Client  — Blazor WebAssembly interactive components
└── ClinicScheduler.Web.Tests   — xUnit integration + unit tests
```

Solution file: `ClinicScheduler/ClinicScheduler.slnx`

---

## Getting Started

### Option 1 — Docker (recommended, no local PostgreSQL needed)

```bash
docker-compose up --build
```

The app will be available at `http://localhost:8081`.

> **Credentials:** By default, `docker-compose` uses fallback values from `.env.example`.
> For production deployments, copy `.env.example` to `.env` and set strong, unique passwords before running:
> ```bash
> cp .env.example .env
> # edit .env with strong passwords
> docker-compose up --build
> ```

Default demo accounts (seeded on first run):

| Role | Email | Password |
|---|---|---|
| Admin | admin@clinic.com | *(set via `SeedAdmin__Password` env var)* |
| Clinic Manager | manager@clinic.com | *(set via seed configuration)* |
| Therapist | therapist@clinic.com | *(set via seed configuration)* |
| Staff | staff@clinic.com | *(set via seed configuration)* |
| Patient | patient@clinic.com | *(set via seed configuration)* |

> **Note:** Default demo passwords are configured in the database seeder for development only. See the seed configuration for current values. Never use demo passwords in production.

### Option 2 — Local development

1. Start a PostgreSQL instance (or use `docker-compose up db` to start only the database).

2. Set the connection string in `ClinicScheduler/ClinicScheduler.Web/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=clinic_scheduler;Username=postgres;Password=postgres"
     }
   }
   ```

3. Run the web app:
   ```bash
   dotnet run --project ClinicScheduler/ClinicScheduler.Web
   ```

4. Open the Swagger UI at `https://localhost:<port>/swagger` to explore the API.

### Option 3 — Visual Studio

1. Open `ClinicScheduler/ClinicScheduler.slnx`.
2. Set `ClinicScheduler.Web` as the startup project for the web app, or `ClinicScheduler` for the MAUI shell.
3. Press F5.

---

## Running Tests

Docker must be running — integration tests spin up a real `postgres:17-alpine` container automatically via Testcontainers.

```bash
# Entity unit tests
dotnet test ClinicScheduler/ClinicScheduler.Core.Tests

# Unit tests only (no Docker required)
dotnet test ClinicScheduler/ClinicScheduler.Web.Tests --filter "FullyQualifiedName~Unit"

# Integration tests (Docker required)
dotnet test ClinicScheduler/ClinicScheduler.Web.Tests --filter "FullyQualifiedName~Api"

# Full web test suite
dotnet test ClinicScheduler/ClinicScheduler.Web.Tests
```

See [TESTING.md](TESTING.md) for a full description of the testing strategy.

---

## Deployment

**Live:** Deployed on AWS EC2 (us-east-1). Contact the team for the current deployment URL.

See [DEPLOYMENT_NOTES.md](DEPLOYMENT_NOTES.md) for step-by-step EC2 deployment instructions.

Required environment variables for production (set in `.env` on EC2 — never committed):

| Variable | Purpose |
|---|---|
| `POSTGRES_PASSWORD` | PostgreSQL password (used by both db and app containers) |
| `SEED_ADMIN_PASSWORD` | Admin account password seeded on first run (min 10 chars, uppercase, digit, special char) |
| `ASPNETCORE_ENVIRONMENT` | Set to `Production` on EC2 |

To update the running EC2 deployment after pushing changes to `MVP`:
```bash
ssh -i <path-to-your-key>.pem ec2-user@<ec2-host>
cd /home/ec2-user/clinic-scheduler
git pull origin MVP
docker-compose --env-file .env up --build -d
```

---

## Database Migrations

Migrations are applied automatically on startup. To add a new migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project ClinicScheduler/ClinicScheduler.Infrastructure \
  --startup-project ClinicScheduler/ClinicScheduler.Web
```

---

## API Overview

All endpoints are under `/api/` and documented at `/swagger` in development.

| Resource | Endpoints |
|---|---|
| Appointments | `GET/POST /api/appointments`, `GET/PUT/DELETE /api/appointments/{id}`, `POST /api/appointments/{id}/mark-missed` |
| Patients | `GET/POST /api/patients`, `GET/PUT/DELETE /api/patients/{id}` |
| Therapists | `GET/POST /api/therapists`, `GET/PUT/DELETE /api/therapists/{id}` |
| Rooms | `GET/POST /api/rooms`, `GET/PUT/DELETE /api/rooms/{id}`, `GET /api/rooms/location/{id}` |
| Locations | `GET/POST /api/locations`, `GET/PUT/DELETE /api/locations/{id}` |
| Therapy Types | `GET/POST /api/therapytypes`, `GET/PUT/DELETE /api/therapytypes/{id}` |
| Treatment Plans | `GET/POST /api/treatmentplans`, `GET/PUT/DELETE /api/treatmentplans/{id}` |
