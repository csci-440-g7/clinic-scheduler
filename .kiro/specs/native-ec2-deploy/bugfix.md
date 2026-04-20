# Bugfix Requirements Document

## Introduction

The ClinicScheduler application targets .NET 10 (`net10.0`) and is currently deployed on EC2 using Docker for both the app and PostgreSQL (via `docker-compose.yml`). .NET 10 has a known compatibility issue when running inside Docker containers, preventing the app from building or running correctly in the containerized environment. The workaround is to run the .NET app natively on the EC2 host while keeping PostgreSQL in Docker, using a systemd service for process management and auto-restart.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN the ClinicScheduler app is built and run inside a Docker container using the `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0` images THEN the system fails to build or run correctly due to a known .NET 10 Docker compatibility issue

1.2 WHEN `docker compose up` is executed to deploy the full stack (app + PostgreSQL) THEN the app container fails to start or behaves incorrectly while the PostgreSQL container starts successfully

### Expected Behavior (Correct)

2.1 WHEN the ClinicScheduler app is deployed to EC2 THEN the system SHALL build and run the app natively on the host using `dotnet publish` and a systemd service (`clinic-scheduler.service`), bypassing the .NET 10 Docker issue entirely

2.2 WHEN the deployment is executed via `deploy/start-native.sh` THEN the system SHALL start only the PostgreSQL container via Docker Compose, publish the app natively with `dotnet publish`, and manage the app process through systemd with auto-restart capability

2.3 WHEN the EC2 instance is bootstrapped via `deploy/bootstrap.sh` THEN the system SHALL install the .NET 10 SDK from the Microsoft package repository and Docker/Docker Compose, providing all prerequisites for the native deployment path

### Unchanged Behavior (Regression Prevention)

3.1 WHEN PostgreSQL is started via Docker Compose THEN the system SHALL CONTINUE TO run PostgreSQL 17 in a Docker container with the same volume persistence, health checks, and port mapping (5432:5432) as the current configuration

3.2 WHEN the app connects to PostgreSQL THEN the system SHALL CONTINUE TO use the same connection string pattern (`Host=...;Database=clinic_scheduler;Username=postgres;Password=...`) to reach the database

3.3 WHEN the app is running THEN the system SHALL CONTINUE TO listen on port 8080 and serve the Blazor web application with the same behavior as the Docker-based deployment

3.4 WHEN environment variables (`ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__DefaultConnection`, `SeedAdmin__Password`) are configured THEN the system SHALL CONTINUE TO respect these settings in the native deployment just as in the Docker-based deployment
