# clinic-scheduler

East Texas A&amp;M CSCI-440 Group 7 capstone. Pain Management Clinic Scheduler.

# Getting started with development

## Prerequisites

- Install .NET 9.0 SDK (or higher)
- Install Visual Studio 2022 (v17.12+) with the following workloads:
  - .NET Multi-platform App UI development
  - ASP.NET and web development

## Project Structure

The solution consists of the following projects:

- **ClinicScheduler**: The .NET MAUI Blazor Hybrid application (Windows, Android, iOS, macOS).
- **ClinicScheduler.Web**: The Blazor Web application (Server/Client).
- **ClinicScheduler.Shared**: Shared Razor Class Library (UI and Services).
- **ClinicScheduler.Core**: Core business logic and entities.

## Local Development

1. Clone the repository.
2. Open `ClinicScheduler.sln` in Visual Studio.

### Running the MAUI App

1. Set `ClinicScheduler` as the Startup Project.
2. Select the target framework (e.g., Windows Machine).
3. Press F5 to run.

### Running the Web App

1. Set `ClinicScheduler.Web` as the Startup Project.
2. Press F5 to run.
