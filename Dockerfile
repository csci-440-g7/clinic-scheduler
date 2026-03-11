# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first so NuGet restore is cached
COPY ClinicScheduler/ClinicScheduler.slnx ./ClinicScheduler/
COPY ClinicScheduler/ClinicScheduler.Core/ClinicScheduler.Core.csproj                     ./ClinicScheduler/ClinicScheduler.Core/
COPY ClinicScheduler/ClinicScheduler.Infrastructure/ClinicScheduler.Infrastructure.csproj ./ClinicScheduler/ClinicScheduler.Infrastructure/
COPY ClinicScheduler/ClinicScheduler.Shared/ClinicScheduler.Shared.csproj                 ./ClinicScheduler/ClinicScheduler.Shared/
COPY ClinicScheduler/ClinicScheduler.Web.Client/ClinicScheduler.Web.Client.csproj         ./ClinicScheduler/ClinicScheduler.Web.Client/
COPY ClinicScheduler/ClinicScheduler.Web/ClinicScheduler.Web.csproj                       ./ClinicScheduler/ClinicScheduler.Web/

# Restore only the web project's dependency tree (skip MAUI)
RUN dotnet restore ClinicScheduler/ClinicScheduler.Web/ClinicScheduler.Web.csproj

# Copy the rest of the source
COPY ClinicScheduler/ ./ClinicScheduler/

# Publish in Release mode to /app/publish
RUN dotnet publish ClinicScheduler/ClinicScheduler.Web/ClinicScheduler.Web.csproj \
    --no-restore \
    -c Release \
    -o /app/publish

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy only the published output from the build stage
COPY --from=build /app/publish .

# ASP.NET Core listens on 8080 by default in .NET 8+
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ClinicScheduler.Web.dll"]
