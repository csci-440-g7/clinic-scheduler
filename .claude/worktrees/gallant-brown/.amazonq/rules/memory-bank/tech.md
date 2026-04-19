# Technology Stack

## Languages & Runtime
- **C# / .NET 10.0** — all server and client projects target `net10.0`
- **JavaScript** — minimal vanilla JS for Blazor reconnect modal (`ReconnectModal.razor.js`)
- **Razor** — `.razor` components for Blazor UI

## Frameworks
- **ASP.NET Core 10** — web host, middleware pipeline, API controllers
- **Blazor** — interactive UI; Server + WebAssembly hybrid (Auto render mode)
- **Entity Framework Core 10** with **Npgsql** provider — ORM + PostgreSQL

## Key NuGet Packages
| Package | Purpose |
|---|---|
| `MudBlazor 9.x` | UI component library |
| `Microsoft.AspNetCore.OpenApi` + `Swashbuckle.AspNetCore` | OpenAPI / Swagger docs |
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | WASM hosting |
| `Microsoft.EntityFrameworkCore.Design` | EF migrations tooling |
| `xunit` | Test framework |
| `FluentAssertions` | Assertion library |
| `Moq` | Mocking for unit tests |
| `Testcontainers.PostgreSql` | Real PostgreSQL container for integration tests |
| `Microsoft.AspNetCore.Mvc.Testing` | `WebApplicationFactory` for integration tests |

## Database
- **PostgreSQL 17** (Alpine) in Docker; database name `clinic_scheduler`
- Connection string key: `ConnectionStrings:DefaultConnection`
- Migrations applied automatically on startup via `db.Database.Migrate()`

## Infrastructure
- **Docker / Docker Compose** — `docker-compose.yml` runs `db` (postgres:17-alpine) + `app` (port 8080)
- **Dockerfile** — multi-stage: `mcr.microsoft.com/dotnet/sdk:10.0` build → `mcr.microsoft.com/dotnet/aspnet:10.0` runtime
- HTTPS termination handled by load balancer in production; app listens on HTTP 8080

## Development Commands
```bash
# Run web app locally (requires PostgreSQL or Docker)
dotnet run --project ClinicScheduler/ClinicScheduler.Web

# Run all tests
dotnet test ClinicScheduler/ClinicScheduler.Web.Tests

# Add EF migration
dotnet ef migrations add <Name> --project ClinicScheduler/ClinicScheduler.Infrastructure --startup-project ClinicScheduler/ClinicScheduler.Web

# Docker Compose (full stack)
docker-compose up --build

# Swagger UI (dev only)
http://localhost:<port>/swagger
```

## Configuration
- `appsettings.json` / `appsettings.Development.json` — connection strings, environment settings
- `UserSecretsId` set on `ClinicScheduler.Web` for local secrets
- `ASPNETCORE_ENVIRONMENT=Testing` used by `WebAppFixture` to skip connection string validation
