---
inclusion: manual
---

# ClinicScheduler Project Context

## Project Overview
- **App**: ClinicScheduler — a Blazor web app (.NET 10) for a pain management clinic
- **Solution**: `ClinicScheduler/ClinicScheduler.slnx`
- **Key projects**:
  - `ClinicScheduler.Web` — ASP.NET Core host (server-side Blazor + WebAssembly)
  - `ClinicScheduler.Shared` — Shared Razor components, pages, layouts
  - `ClinicScheduler.Core` — Domain entities and services
  - `ClinicScheduler.Infrastructure` — EF Core DbContext, data access
  - `ClinicScheduler.Core.Tests` — xUnit + FsCheck property-based tests
- **UI Framework**: MudBlazor (v9 preview) + custom CSS in some pages
- **Database**: PostgreSQL 17 (runs in Docker on EC2)
- **Target framework**: `net10.0`

## Deployment (EC2 — Native)
- **Why native**: .NET 10 has a known Docker image compatibility issue with Blazor static assets (`blazor.web.js` missing)
- **EC2 IP**: `52.72.1.65`
- **App URL**: `http://52.72.1.65:8081`
- **SSH**: `ssh -i "C:\Machine_Learning\.claude\clinic-capstone-key.pem" ec2-user@52.72.1.65`
  - Key permissions must be locked down: `icacls ... /inheritance:r /remove "BUILTIN\Users" /grant:r "$($env:USERNAME):(R)"`
- **Deploy scripts** (in `deploy/` directory):
  - `bootstrap.sh` — one-time EC2 setup: installs .NET 10 SDK, Docker, Docker Compose, clones repo, creates `.env`
  - `start-native.sh` — redeploy script: pulls MVP, starts Postgres in Docker, cleans up old app Docker artifacts (app only, not db), publishes natively via `dotnet publish`, creates/updates systemd service
- **Systemd service**: `clinic-scheduler.service`
  - Logs: `journalctl -u clinic-scheduler -f`
  - Restart: `sudo systemctl restart clinic-scheduler`
  - Stop: `sudo systemctl stop clinic-scheduler`
- **PostgreSQL**: runs in Docker via `docker compose -f /home/ec2-user/clinic-scheduler/docker-compose.yml up -d db`
- **Redeploy workflow**:
  1. Push to MVP branch from local
  2. SSH into EC2
  3. `cd /home/ec2-user/clinic-scheduler && git pull origin MVP && bash deploy/start-native.sh`
- **Known issue**: If the app can't connect after deploy, PostgreSQL may have been stopped. Fix: `docker compose -f /home/ec2-user/clinic-scheduler/docker-compose.yml up -d db && sleep 5 && sudo systemctl restart clinic-scheduler`

## Demo Accounts (from SeedData.cs)
| Role | Email | Password |
|------|-------|----------|
| Admin | admin@clinic.com | *(set in .env as SEED_ADMIN_PASSWORD)* |
| Clinic Manager | manager@clinic.com | Manager@1234 |
| Staff | staff@clinic.com | Staff@Clinic1 |
| Therapist | therapist@clinic.com | Therapist@1234 |
| Patient | patient@clinic.com | Patient@1234 |

## Key UI Files (for cosmetic changes)
| Page | File |
|------|------|
| Login | `ClinicScheduler/ClinicScheduler.Shared/Pages/Login.razor` |
| Dashboard (staff + patient) | `ClinicScheduler/ClinicScheduler.Shared/Pages/Home.razor` |
| Calendar | `ClinicScheduler/ClinicScheduler.Shared/Pages/Calendar.razor` + `CalendarView.razor` |
| Book Appointment modal | `ClinicScheduler/ClinicScheduler.Shared/Pages/BookAppointmentModal.razor` |
| Patients list | `ClinicScheduler/ClinicScheduler.Shared/Pages/Patients.razor` |
| Treatment Plans | `ClinicScheduler/ClinicScheduler.Shared/Pages/TreatmentPlans.razor` |
| Patient Onboarding | `ClinicScheduler/ClinicScheduler.Shared/Pages/PatientOnboarding.razor` |
| Sidebar nav | `ClinicScheduler/ClinicScheduler.Shared/Layout/NavMenu.razor` + `.razor.css` |
| Main layout | `ClinicScheduler/ClinicScheduler.Shared/Layout/MainLayout.razor` |
| Staff Dashboard | `ClinicScheduler/ClinicScheduler.Shared/Pages/StaffDashboard.razor` |

## Styling Notes
- Some pages use MudBlazor components (`MudSelect`, `MudButton`, `MudDataGrid`, etc.)
- Some pages (Patients, Calendar, BookAppointment) use custom HTML + inline `<style>` blocks
- The sidebar nav uses scoped CSS in `NavMenu.razor.css`
- Login page uses plain HTML inputs (not MudBlazor) because it's a server-side form POST

## Work Done Today (April 19, 2026)

### Native EC2 Deploy (bugfix spec)
- Created `deploy/bootstrap.sh` and `deploy/start-native.sh` for native .NET deployment
- App runs natively via systemd, PostgreSQL stays in Docker
- Fixed Docker cleanup step that was killing PostgreSQL (`docker compose down --rmi local` → `docker compose rm -sf app`)
- Wrote property-based tests (FsCheck) for bug condition and preservation

### Bug Fixes
- **SeedData.cs**: Added `patient@clinic.com` as a Patient record so the demo patient account can submit appointment requests
- **TreatmentPlans.razor**: Fixed TherapyType EF tracking conflict (use `DbContext.FindAsync` instead of reusing tracked list), fixed Therapy Types dropdown showing IDs instead of names
- **BookAppointmentModal.razor**: Fixed `DateTime Kind=Unspecified` error — now uses `DateTime.SpecifyKind(..., DateTimeKind.Utc)`

### UI Fixes
- **NavMenu.razor.css**: Changed nav link colors from light gray (designed for dark sidebar) to dark colors for white sidebar background, updated SVG icon fills from white to dark
- **Login.razor**: Replaced MudBlazor CSS class wrappers on plain HTML inputs with clean inline styles (no more shadows)
- **Patients.razor**: Split phone and email onto separate lines, improved spacing between name/phone/email/View details

### Remaining UI Work
- Patient list spacing may need further tweaking after deploy verification
- Staff Dashboard (`StaffDashboard.razor`) has similar patient list items that may need the same spacing fix
- General cosmetic polish across all pages
