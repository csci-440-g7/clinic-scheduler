# Native EC2 Deploy Bugfix Design

## Overview

The ClinicScheduler web app targets .NET 10 (`net10.0`) and is currently deployed on EC2 using Docker for both the app and PostgreSQL. A known .NET 10 compatibility issue prevents the app from building or running correctly inside Docker containers. The fix is to run the app natively on the EC2 host using `dotnet publish` and a systemd service, while keeping PostgreSQL in its Docker container. This requires three deployment artifacts: `deploy/bootstrap.sh` (one-time EC2 setup), `deploy/start-native.sh` (deploy/redeploy script), and a systemd service unit (`clinic-scheduler.service`) generated inline by the deploy script.

## Glossary

- **Bug_Condition (C)**: The app is built and run inside a Docker container using .NET 10 SDK/runtime images, causing build or runtime failures
- **Property (P)**: The app builds and runs successfully as a native process on the EC2 host, managed by systemd, connecting to PostgreSQL in Docker on localhost
- **Preservation**: PostgreSQL Docker container configuration, connection string pattern, port mapping, environment variable handling, and application behavior must remain unchanged
- **bootstrap.sh**: One-time setup script in `deploy/bootstrap.sh` that installs .NET 10 SDK, Docker, and Docker Compose on a fresh Amazon Linux 2023 EC2 instance
- **start-native.sh**: Deployment script in `deploy/start-native.sh` that pulls code, starts only PostgreSQL in Docker, publishes the app natively, and manages it via systemd
- **clinic-scheduler.service**: A systemd service unit that runs the published .NET app as `ec2-user`, with auto-restart and environment variable passthrough
- **REPO_DIR**: `/home/ec2-user/clinic-scheduler` — the cloned repository path on EC2
- **APP_DIR**: `/home/ec2-user/app` — the directory where `dotnet publish` output is placed

## Bug Details

### Bug Condition

The bug manifests when the ClinicScheduler app is built or run inside Docker containers using the `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0` images. The .NET 10 Docker images have a known compatibility issue that causes the build to fail or the runtime to behave incorrectly (e.g., missing `blazor.web.js` in published output).

**Formal Specification:**
```
FUNCTION isBugCondition(deployment)
  INPUT: deployment of type DeploymentConfiguration
  OUTPUT: boolean
  
  RETURN deployment.appRuntime == "docker"
         AND deployment.dotnetVersion == "10.0"
         AND deployment.usesDockerImages IN ["mcr.microsoft.com/dotnet/sdk:10.0", "mcr.microsoft.com/dotnet/aspnet:10.0"]
END FUNCTION
```

### Examples

- **Docker build failure**: Running `docker compose up` with the existing `Dockerfile` and `docker-compose.yml` fails during the `dotnet publish` step inside the container, or the published output is missing `blazor.web.js`
- **Container runtime failure**: The app container starts but the Blazor app does not load because static assets were not correctly published inside the Docker image
- **PostgreSQL unaffected**: The `db` service (PostgreSQL 17 Alpine) starts and passes health checks successfully even when the app container fails
- **Native build succeeds**: Running `dotnet publish` directly on the EC2 host with the same .NET 10 SDK produces a correct, complete published output including all Blazor static assets

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- PostgreSQL 17 must continue to run in a Docker container with the same volume persistence (`clinic_db_data`), health checks (`pg_isready`), and port mapping (`5432:5432`)
- The connection string pattern (`Host=localhost;Database=clinic_scheduler;Username=postgres;Password=...`) must continue to work — the only change is `Host=db` becomes `Host=localhost` since the app now runs on the host instead of inside a Docker network
- The app must continue to listen on port 8081 externally and serve the Blazor web application with identical behavior
- Environment variables (`ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__DefaultConnection`, `SeedAdmin__Password`) must continue to be sourced from the `.env` file and passed to the app process

**Scope:**
All inputs that do NOT involve running the .NET app inside a Docker container should be completely unaffected by this fix. This includes:
- PostgreSQL container lifecycle (start, stop, health checks, data persistence)
- The `.env` file format and variable names
- The application's runtime behavior (routes, API endpoints, authentication, database migrations)
- The `deploy/start.sh` Docker-based script (kept as a fallback, not modified)

## Hypothesized Root Cause

Based on the bug description, the most likely issues are:

1. **.NET 10 Docker Image Compatibility**: The `mcr.microsoft.com/dotnet/sdk:10.0` image has a known issue where `dotnet publish` for Blazor WebAssembly projects does not correctly include `blazor.web.js` and related static assets in the published output. This is a .NET 10 preview/early-release regression in the Docker SDK image.

2. **Multi-stage Build Artifact Loss**: The Dockerfile uses a multi-stage build (`sdk` → `aspnet` runtime). Even if the build succeeds, the runtime image may not correctly serve Blazor static files due to missing workload components in the .NET 10 ASP.NET runtime image.

3. **Native SDK Works Correctly**: The .NET 10 SDK installed directly on Amazon Linux via the Microsoft package repository does not exhibit this issue — `dotnet publish` produces complete output with all Blazor assets. This confirms the bug is specific to the Docker image packaging, not the SDK itself.

4. **Workaround Validity**: Running the app natively on the host completely bypasses the Docker image issue. The app connects to PostgreSQL on `localhost:5432` (exposed by the Docker container's port mapping) instead of using the Docker network hostname `db`.

## Correctness Properties

Property 1: Bug Condition - App Runs Natively Instead of in Docker

_For any_ deployment executed via `deploy/start-native.sh`, the .NET app SHALL be running as a native `dotnet` process managed by systemd (not inside a Docker container), and the published output SHALL include all required Blazor static assets including `blazor.web.js`.

**Validates: Requirements 2.1, 2.2**

Property 2: Preservation - PostgreSQL Docker Configuration Unchanged

_For any_ deployment executed via `deploy/start-native.sh`, PostgreSQL SHALL continue to run in a Docker container with the same image (`postgres:17-alpine`), volume persistence (`clinic_db_data`), health check (`pg_isready`), and port mapping (`5432:5432`), and the app SHALL connect to it via `localhost:5432` using the same connection string pattern.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4**

## Fix Implementation

### Changes Required

Assuming our root cause analysis is correct:

**File**: `deploy/bootstrap.sh`

**Purpose**: One-time EC2 instance setup

**Specific Changes**:
1. **Install .NET 10 SDK**: Add the Microsoft package repository RPM and install `dotnet-sdk-10.0` via `dnf`
2. **Install Docker and Docker Compose**: Install Docker for PostgreSQL container management, enable the Docker service, add `ec2-user` to the `docker` group, and install the Docker Compose standalone binary
3. **Clone repository**: Clone the `MVP` branch to `/home/ec2-user/clinic-scheduler` (or pull if already cloned)
4. **Create `.env` from template**: Copy `.env.example` to `.env` and prompt the user to fill in real passwords

**File**: `deploy/start-native.sh`

**Purpose**: Deploy or redeploy the app

**Specific Changes**:
1. **Validate `.env`**: Check that `.env` exists and does not contain placeholder `changeme` values
2. **Source `.env`**: Load environment variables from `.env` into the shell session
3. **Pull latest code**: `git pull origin MVP` to get the latest changes
4. **Start only PostgreSQL**: Run `docker-compose up -d db` to start only the database container (not the app)
5. **Wait for PostgreSQL health**: Poll `pg_isready` until PostgreSQL is accepting connections
6. **Stop existing app**: `sudo systemctl stop clinic-scheduler` to stop any running instance
7. **Clean up failed Docker build artifacts**: Remove stale Docker images and containers from previous failed builds (`docker compose down --rmi local` for the app service, and prune dangling images/build cache) to reclaim disk space and avoid confusion with the native deployment path
8. **Publish app natively**: Run `dotnet publish` targeting the `ClinicScheduler.Web.csproj` with Release configuration, outputting to `/home/ec2-user/app`
9. **Create systemd service unit**: Write `/etc/systemd/system/clinic-scheduler.service` with environment variables from `.env`, configure `Restart=always`, and set `After=network.target docker.service`
10. **Enable and start service**: `systemctl daemon-reload`, `systemctl enable`, `systemctl start`

**File**: Inline systemd unit (`clinic-scheduler.service`)

**Purpose**: Process management for the native app

**Specific Changes**:
1. **Service type**: `Type=exec` for a long-running foreground process
2. **User**: Run as `ec2-user` (not root)
3. **Working directory**: `/home/ec2-user/app`
4. **ExecStart**: `/usr/bin/dotnet /home/ec2-user/app/ClinicScheduler.Web.dll`
5. **Environment variables**: Pass `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS=http://+:8081`, `ConnectionStrings__DefaultConnection` (with `Host=localhost`), and `SeedAdmin__Password` from `.env` values
6. **Restart policy**: `Restart=always` with `RestartSec=5` for automatic recovery
7. **Dependencies**: `After=network.target docker.service` to ensure Docker (and thus PostgreSQL) is available

**File**: `docker-compose.yml`

**Purpose**: No changes required

**Rationale**: The existing `docker-compose.yml` already defines both `db` and `app` services. The `start-native.sh` script selectively starts only the `db` service with `docker-compose up -d db`. The `app` service definition is harmless when not started and serves as documentation of the Docker-based deployment path. The `start.sh` script remains as a fallback that starts both services.

## Testing Strategy

### Validation Approach

The testing strategy follows a two-phase approach: first, surface counterexamples that demonstrate the bug on unfixed code (Docker-based deployment), then verify the fix works correctly (native deployment) and preserves existing behavior (PostgreSQL, env vars, port mapping).

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples that demonstrate the bug BEFORE implementing the fix. Confirm or refute the root cause analysis. If we refute, we will need to re-hypothesize.

**Test Plan**: Attempt to build and run the app using the Docker-based path (`deploy/start.sh`) and observe the failure. Inspect the Docker build output and container logs to confirm the root cause is related to .NET 10 Docker image compatibility.

**Test Cases**:
1. **Docker Build Test**: Run `docker compose build` and check if `dotnet publish` succeeds inside the container (will fail or produce incomplete output on unfixed code)
2. **Docker Runtime Test**: Run `docker compose up` and check if the app container starts and serves the Blazor app (will fail on unfixed code)
3. **Static Asset Test**: Inspect the Docker image's published output for `blazor.web.js` (will be missing on unfixed code)
4. **PostgreSQL Isolation Test**: Verify that `docker compose up -d db` starts PostgreSQL successfully even when the app container fails (should pass — confirms the bug is app-specific)

**Expected Counterexamples**:
- `dotnet publish` inside Docker produces output missing `blazor.web.js` or fails entirely
- The app container exits with a non-zero code or serves a broken page
- Possible causes: .NET 10 Docker SDK image regression, missing Blazor workload in container

### Fix Checking

**Goal**: Verify that for all inputs where the bug condition holds (app would be run in Docker), the fixed deployment path (native) produces the expected behavior.

**Pseudocode:**
```
FOR ALL deployment WHERE isBugCondition(deployment) DO
  result := startNative(deployment)
  ASSERT result.appProcess IS managed BY systemd
  ASSERT result.appProcess IS NOT inside a Docker container
  ASSERT result.publishedOutput CONTAINS "blazor.web.js"
  ASSERT result.appResponds ON port 8081
  ASSERT result.postgresRunning IN Docker ON port 5432
END FOR
```

### Preservation Checking

**Goal**: Verify that for all inputs where the bug condition does NOT hold (PostgreSQL, env vars, connection strings), the fixed deployment produces the same result as the original.

**Pseudocode:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT postgresConfig_original(input) = postgresConfig_fixed(input)
  ASSERT connectionStringPattern_original(input) = connectionStringPattern_fixed(input)
  ASSERT envVarHandling_original(input) = envVarHandling_fixed(input)
  ASSERT appBehavior_original(input) = appBehavior_fixed(input)
END FOR
```

**Testing Approach**: Property-based testing is recommended for preservation checking because:
- It generates many environment variable combinations to verify they are all correctly passed through
- It catches edge cases in connection string formatting that manual tests might miss
- It provides strong guarantees that PostgreSQL configuration is unchanged across deployments

**Test Plan**: Observe behavior on UNFIXED code first for PostgreSQL container, connection strings, and environment variable handling, then write tests capturing that behavior.

**Test Cases**:
1. **PostgreSQL Preservation**: Verify that `docker-compose up -d db` starts PostgreSQL with the same image, volume, health check, and port mapping as the original `docker-compose up`
2. **Connection String Preservation**: Verify the connection string uses `Host=localhost` (native) instead of `Host=db` (Docker network) but otherwise has the same format and credentials
3. **Environment Variable Preservation**: Verify that `ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__DefaultConnection`, and `SeedAdmin__Password` from `.env` are correctly passed to the systemd service
4. **Port Mapping Preservation**: Verify the app listens on port 8081 externally, matching the original Docker-based deployment

### Unit Tests

- Test that `bootstrap.sh` installs the correct packages (`dotnet-sdk-10.0`, `docker`, `docker-compose`)
- Test that `start-native.sh` validates `.env` existence and placeholder detection
- Test that the systemd service unit has correct `ExecStart`, `User`, `WorkingDirectory`, and `Environment` directives
- Test that `start-native.sh` only starts the `db` service (not `app`) via Docker Compose

### Property-Based Tests

- Generate random `.env` configurations (varying passwords, environment names) and verify the systemd service unit correctly interpolates all values
- Generate random deployment states (first deploy vs redeploy, service already running vs stopped) and verify `start-native.sh` handles all cases correctly
- Test that for any valid `.env` file, the connection string in the systemd unit always uses `Host=localhost` and includes the correct password

### Integration Tests

- Test full deployment flow: `bootstrap.sh` → edit `.env` → `start-native.sh` → verify app responds on port 8081
- Test redeploy flow: run `start-native.sh` twice and verify the app restarts cleanly with updated code
- Test that PostgreSQL data persists across `start-native.sh` runs (volume not recreated)
- Test that `systemctl restart clinic-scheduler` recovers the app after a simulated crash
