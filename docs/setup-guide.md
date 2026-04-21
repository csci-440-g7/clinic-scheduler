# Setup Guide

## Prerequisites

| Requirement | Minimum Version | Notes |
|-------------|----------------|-------|
| .NET SDK | 10.0 | [Download](https://dotnet.microsoft.com/download/dotnet/10.0) |
| PostgreSQL | 14+ | 17 recommended; or use Docker |
| Docker (optional) | 20.10+ | For containerized development |
| Docker Compose (optional) | 2.0+ | Included with Docker Desktop |

## Quick Start with Docker Compose

The fastest way to get running:

```bash
# 1. Clone the repository
git clone <repository-url>
cd ClinicScheduler

# 2. Copy the environment file and set passwords
cp .env.example .env
# Edit .env — set POSTGRES_PASSWORD and SEED_ADMIN_PASSWORD

# 3. Start everything
docker-compose up --build
```

The app will be available at `http://localhost:8081`.

## Local Development Setup

### 1. Install PostgreSQL

Install PostgreSQL and create the database:

```bash
# macOS (Homebrew)
brew install postgresql@17
brew services start postgresql@17

# Ubuntu/Debian
sudo apt install postgresql

# Create the database
psql -U postgres -c "CREATE DATABASE clinic_scheduler;"
```

### 2. Configure the Connection String

The connection string is in `ClinicScheduler/ClinicScheduler.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=clinic_scheduler;Username=postgres;Password=postgres"
  }
}
```

For local development, the default values work with a standard PostgreSQL install. For custom credentials, update the connection string or use environment variables:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=clinic_scheduler;Username=myuser;Password=mypassword"
```

### 3. Set the Admin Seed Password

The app seeds an admin account on first run. Set the password via environment variable or user secrets:

```bash
# Environment variable
export SeedAdmin__Password="MyAdmin@2026!"

# Or use .NET User Secrets (development only)
cd ClinicScheduler/ClinicScheduler.Web
dotnet user-secrets set "SeedAdmin:Password" "MyAdmin@2026!"
```

**Production password requirements:** minimum 10 characters, at least one uppercase letter, one digit, and one special character.

### 4. Build and Run

```bash
# From the repository root
cd ClinicScheduler

# Restore dependencies
dotnet restore ClinicScheduler.Web/ClinicScheduler.Web.csproj

# Build
dotnet build ClinicScheduler.Web/ClinicScheduler.Web.csproj

# Run
dotnet run --project ClinicScheduler.Web
```

The app starts at `https://localhost:5001` (HTTPS) or `http://localhost:5000` (HTTP).

EF Core migrations run automatically on startup in Development mode. If the database is unavailable, the app logs a warning and continues.

### 5. Run Tests

```bash
# Run all tests
dotnet test

# Run only Core tests
dotnet test ClinicScheduler.Core.Tests
```

## Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Yes | PostgreSQL connection string |
| `SeedAdmin__Password` | Yes | Password for the seeded admin account |
| `ASPNETCORE_ENVIRONMENT` | No | `Development` (default) or `Production` |
| `AllowedOrigins__0`, `__1`, etc. | No | CORS allowed origins for production |

## Database Migrations

Migrations are applied automatically on startup. To manage them manually:

```bash
cd ClinicScheduler

# Apply pending migrations
dotnet ef database update --project ClinicScheduler.Infrastructure --startup-project ClinicScheduler.Web

# Add a new migration
dotnet ef migrations add <MigrationName> --project ClinicScheduler.Infrastructure --startup-project ClinicScheduler.Web

# Revert the last migration
dotnet ef database update <PreviousMigrationName> --project ClinicScheduler.Infrastructure --startup-project ClinicScheduler.Web
```

## Swagger / OpenAPI

In Development mode, Swagger UI is available at `/swagger`. The OpenAPI spec is at `/openapi/v1.json`.

## Default Accounts

After seeding, the following admin account is available:

| Email | Role | Password |
|-------|------|----------|
| `admin@clinic.com` | Admin | Value of `SeedAdmin:Password` |

Additional users (staff, therapists, patients) can be created through the admin UI.
