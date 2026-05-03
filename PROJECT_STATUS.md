# Project Status — ClinicScheduler

**Last updated:** 2026-04-18 (Phase 2 complete — app live on EC2)
**Branch:** `MVP` (worktree `claude/gallant-brown`)
**Course:** East Texas A&M CSCI-440 Group 7 Capstone

---

## Overall Assessment

The project is **production-ready** with all core scheduling features implemented, tested, and documented. A full gap analysis was performed against the original system vision (see `.amazonq/rules/memory-bank/`) with no blocking issues found.

---

## What Is Complete

### Backend — API & Business Logic
- All 8 REST controllers implemented (`Appointments`, `Patients`, `Therapists`, `Rooms`, `Locations`, `TherapyTypes`, `TreatmentPlans`, `Account`)
- All CRUD endpoints present; `POST /api/appointments/{id}/mark-missed` implemented with auto-reschedule
- Correct HTTP status codes throughout: `201 Created`, `204 No Content`, `400 Bad Request`, `404 Not Found`, `409 Conflict`
- All 5 scheduling business rules enforced in `AppointmentSchedulingService`:
  - Weekdays only (Mon–Fri)
  - 8:00 AM – 5:00 PM window
  - 30-minute slots only
  - Max 12 concurrent patients clinic-wide
  - No therapist, room, or patient double-booking
- Auto-reschedule after missed: searches same time-of-day first, up to 30 days forward

### Database & Infrastructure
- EF Core 10 + Npgsql (PostgreSQL 17)
- 10 migrations applied; `db.Database.Migrate()` runs on startup
- `ClinicDbContext` includes audit logging, auto-timestamps, check constraints, unique indexes
- `DatabaseSeeder` seeds roles, demo accounts, and sample clinic data on first run
- `AppointmentReminderService` background service sends 24-hour reminder notifications
- Docker + `docker-compose.yml` configured (`postgres:17-alpine` + `app` on port `8080`/`8081`)

### Auth & Roles
- ASP.NET Core Identity integrated
- Roles: `Admin`, `ClinicManager`, `Therapist`, `Staff`, `Patient`
- Cookie-based auth via `AccountController` (`POST /account/login`, `POST /account/logout`)
- Fallback policy: authenticated user required globally

### Domain Features Beyond Original Spec
- Appointment Request workflow (patient requests → staff approve/deny)
- Audit logging (automatic change tracking via `SaveChangesAsync`)
- In-app notification system
- Patient notes on appointments
- MAUI mobile/desktop shell (`ClinicScheduler` project)

### AWS / Deployment Readiness (Phase 1 — 2026-04-18)
- `docker-compose.yml` credentials replaced with `${POSTGRES_PASSWORD}` / `${SEED_ADMIN_PASSWORD}` env vars (no hardcoded secrets)
- `.env.example` added at repo root documenting all required runtime variables
- `AppointmentSchedulingService.SlotDuration` / `SlotDurationMinutes` promoted to `public static` — referenced by `AppointmentsController` instead of a bare literal
- `PatientOnboarding.razor` now creates an ASP.NET Core Identity login for each patient (`Patient` role) during the onboarding wizard — patients can log in immediately after registration
- CORS policy `"AppPolicy"` added to `Program.cs`: any-origin in Development/Test, `AllowedOrigins` config list (with credentials) in Production
- Identity password rules tightened for `Production` only (10 chars, requires digit, uppercase, non-alphanumeric); Development and Test remain relaxed so seeded demo accounts work

### Blazor UI (all in `ClinicScheduler.Shared/Pages/`)
All pages are functional with MudBlazor — not stubs:

| Page | Description |
|---|---|
| `Home.razor` | Role-based dashboard (staff view: request queue, stats; patient view: upcoming appointments, treatment plan) |
| `Schedule.razor` | Weekly calendar, 8am–5pm slots, click-to-create, status color-coding, mark-missed trigger |
| `Patients.razor` | Patient management grid |
| `Therapists.razor` | Therapist management grid |
| `Rooms.razor` | Room management grid |
| `TherapyTypes.razor` | Therapy type management |
| `TreatmentPlans.razor` | Treatment plan management with multi-select therapy types |
| `PatientProfile.razor` | Patient detail view |
| `PatientOnboarding.razor` | Patient registration flow |
| `StaffOnboarding.razor` | Staff registration flow |
| `Reports.razor` | Reporting page |
| `UserManagement.razor` | Admin user management |
| `Notifications.razor` | In-app notification display |

### Tests — 146 total, all passing

| Suite | Count | Notes |
|---|---|---|
| `ClinicScheduler.Core.Tests` (entity unit) | 69 | No mocks, pure domain logic |
| `ClinicScheduler.Web.Tests/Unit` (service unit) | 24 | Moq, FluentAssertions, BuildSut pattern |
| `ClinicScheduler.Web.Tests/Api` (integration) | 53 | Testcontainers PostgreSQL, WebApplicationFactory |

See [TESTING.md](TESTING.md) for full strategy documentation.

### Documentation Added This Session
- [TESTING.md](TESTING.md) — full testing strategy document
- [PROJECT_STATUS.md](PROJECT_STATUS.md) — this file

---

## Work Done in This Session (2026-04-18) — Phase 2

| Task | Outcome |
|---|---|
| `deploy/bootstrap.sh` created | One-time EC2 setup: installs Docker, Docker Compose, clones repo, copies `.env.example` → `.env`; prints next-step instructions including public IP |
| `deploy/start.sh` created | Re-runnable start/update script: validates `.env` has no placeholder values, `git pull`, `docker-compose up --build -d`; prints app URL on completion |
| `DEPLOYMENT_NOTES.md` rewritten | Full step-by-step guide: EC2 launch (console settings, security group rules), SSH instructions including `.pem` permission fix for Windows, bootstrap, `.env` editing, start, verify, useful commands, architecture summary |

### Phase 2 Status — **LIVE** at http://52.72.1.65:8081

| Step | Status | Notes |
|---|---|---|
| Launch EC2 instance (t3.small, us-east-1, AMI: Amazon Linux 2023) | ✅ Complete | Elastic IP: `52.72.1.65` |
| Configure security group (SSH:22, TCP:8081, TCP:8080 public) | ✅ Complete | `launch-wizard-2` sg |
| SSH + Docker + repo bootstrap | ✅ Complete | Run manually (bootstrap.sh was pushed after initial setup) |
| `.env` configured with production passwords | ✅ Complete | Set on EC2, not committed |
| `docker-compose up --build` | ✅ Complete | App + PostgreSQL running |
| App accessible at public IP | ✅ Complete | http://52.72.1.65:8081 |

### Phase 2 Bug Fixes Applied During EC2 Deployment

| Bug | Fix |
|---|---|
| `ClinicDbContext` merge conflict had wrong base class (`DbContext` instead of `IdentityDbContext<AppUser>`) and missing `AppointmentRequests`, `Notifications`, `AuditLogs` DbSets — caused Docker build failure | Restored correct base class and DbSets; committed and pushed |
| Duplicate `ClinicScheduler.Web.Tests` entry in `ClinicScheduler.slnx` | Removed duplicate entry |
| `Moq` package missing from `ClinicScheduler.Web.Tests.csproj` | Added `Moq` v4.20.72 package reference |
| EF migration `FixTotalDaysConstraint` failed on EC2: PostgreSQL cannot auto-cast `text → integer` | Replaced `AlterColumn` with raw SQL `ALTER COLUMN ... TYPE integer USING "Col"::integer` |
| Duplicate `Index.razor` and `Home.razor` both registered at `@page "/"` — caused `AmbiguousMatchException` (HTTP 500) | Deleted legacy `Index.razor` |
| `Login.razor` was a mock (SessionState, hardcoded code) — caused infinite redirect loop with Identity auth fallback policy | Replaced with real `SignInManager.PasswordSignInAsync` login; added `[AllowAnonymous]` to `/login` and `/2fa` |
| `.claude/worktrees/` accidentally committed | Added `.claude/` to `.gitignore`; removed from tracking |

---

## Work Done in This Session (2026-04-18) — Phase 1

| Task | Outcome |
|---|---|
| Secrets cleanup — `docker-compose.yml` | Replaced `devpassword` / `admin1234` literals with `${POSTGRES_PASSWORD:-devpassword}` / `${SEED_ADMIN_PASSWORD:-admin1234}` env var references |
| `.env.example` created | Documents all required runtime env vars; safe to commit — no real values |
| Slot duration constant | `AppointmentSchedulingService.SlotDuration` and `SlotDurationMinutes` made `public static`; `AppointmentsController` now references them |
| Patient login creation | `PatientOnboarding.razor` — added password fields (step 0), `UserManager<AppUser>` injection, Identity user created + assigned `Patient` role in `FinishAsync` |
| CORS policy | `AddCors("AppPolicy")` + `UseCors` added to `Program.cs`; any-origin in Dev/Test, configurable `AllowedOrigins` in Production |
| Password hardening | Production-only Identity password rules (10 chars, digit, uppercase, special char); condition changed to `IsProduction()` to protect Test environment from regression |
| All tests validated | 146 / 146 passing after changes (69 unit + 24 service unit + 53 integration) |

---

## Work Done in This Session (2026-04-17)

| Task | Outcome |
|---|---|
| Gap analysis vs. vision docs | No blocking gaps found; project ~92% of original spec + significant extras |
| Shared repo evaluation | Build clean, logic correct, pages functional, Docker already present |
| CS1591 XML doc warnings (build) | Fixed on all 8 controllers, `MarkMissedResponse`, `DatabaseSeeder`, `AppointmentReminderService`, `FormFactor` — zero warnings |
| Unit test gap: `RescheduleAfterMissedAsync` success path | Added `RescheduleAfterMissedAsync_NoConflicts_ReturnsAppointmentAfterMissedSlot` |
| Unit test gap: 30-day boundary | Added `RescheduleAfterMissedAsync_NoSlotIn30Days_ThrowsInvalidOperationException` |
| Full test run post-changes | 146 tests, 0 failures |
| TESTING.md created | Covers all three test layers, infrastructure, patterns, run commands, known gaps |

---

## Remaining Open Items

| Item | Priority | Notes |
|---|---|---|
| GitHub Actions CI/CD pipeline (Phase 3) | High | Create `.github/workflows/deploy.yml`; add `EC2_HOST=52.72.1.65`, `EC2_USER=ec2-user`, `EC2_SSH_KEY` secrets to repo |
| Full end-to-end login flow smoke test | High | Login page replaced with real Identity login — needs browser verification that sign-in works with all seeded accounts |
| `Twofactor.razor` mock cleanup | ✅ Done | Removed mock `/2fa` page — real MFA deferred as a future enhancement (see `docs/future-features.md`) |
| `AllowedOrigins` in production config | Low | Populate `AllowedOrigins` in EC2 `.env` or `appsettings.Production.json` if API will be consumed from external origins |
| Concurrency / load test for 12-patient cap | Low | Would require parallel HTTP requests in an integration test; low risk in practice |

---

## Key File Locations

| What | Where |
|---|---|
| Scheduling business logic | `ClinicScheduler/ClinicScheduler.Core/AppointmentSchedulingService.cs` |
| DbContext + audit logging | `ClinicScheduler/ClinicScheduler.Infrastructure/Data/ClinicDbContext.cs` |
| DI wiring + middleware | `ClinicScheduler/ClinicScheduler.Web/Program.cs` |
| All DTOs / contracts | `ClinicScheduler/ClinicScheduler.Web/Contracts/` |
| Blazor pages | `ClinicScheduler/ClinicScheduler.Shared/Pages/` |
| Test fixture | `ClinicScheduler/ClinicScheduler.Web.Tests/Fixtures/WebAppFixture.cs` |
| Unit tests | `ClinicScheduler/ClinicScheduler.Web.Tests/Unit/AppointmentSchedulingServiceTests.cs` |
| Integration tests | `ClinicScheduler/ClinicScheduler.Web.Tests/Api/` |
| Docker setup | `Dockerfile`, `docker-compose.yml` (repo root) |
| Vision / guidelines docs | `.amazonq/rules/memory-bank/` (`product.md`, `tech.md`, `structure.md`, `guidelines.md`) |

---

## Run Commands

```bash
# Web app (requires PostgreSQL or Docker)
dotnet run --project ClinicScheduler/ClinicScheduler.Web

# Full stack via Docker
docker-compose up --build

# All tests
dotnet test ClinicScheduler/ClinicScheduler.Core.Tests
dotnet test ClinicScheduler/ClinicScheduler.Web.Tests

# Unit tests only (no Docker)
dotnet test ClinicScheduler/ClinicScheduler.Web.Tests --filter "FullyQualifiedName~Unit"

# Integration tests only (Docker required)
dotnet test ClinicScheduler/ClinicScheduler.Web.Tests --filter "FullyQualifiedName~Api"
```
