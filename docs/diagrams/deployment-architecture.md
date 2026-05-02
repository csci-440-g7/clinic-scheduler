# Deployment Architecture — ClinicScheduler

> Paste into [mermaid.live](https://mermaid.live) to export as PNG/SVG for slides.

```mermaid
graph TB
    subgraph Internet
        Browser["🌐 Browser<br/>(Any device)"]
    end

    subgraph AWS["AWS — EC2 t3.small (us-east-1, Amazon Linux 2023)"]
        subgraph Systemd["systemd service: clinic-scheduler"]
            App["ClinicScheduler.Web<br/>ASP.NET Core 10<br/>Blazor Server + WASM<br/>REST API Controllers<br/>Port 8081"]
        end

        subgraph Docker["Docker"]
            DB["PostgreSQL 17-alpine<br/>Port 5432 (internal only)"]
        end
    end

    subgraph GitHub["GitHub — Bradly187/clinic-scheduler"]
        Repo["MVP branch"]
    end

    Browser -->|"HTTP :8081"| App
    App -->|"EF Core / Npgsql"| DB
    Repo -->|"GitHub Actions<br/>SSH deploy"| App

    style AWS fill:#f9f3e8,stroke:#e8a735,stroke-width:2px
    style Systemd fill:#e8f4e8,stroke:#4caf50,stroke-width:1px
    style Docker fill:#e3f2fd,stroke:#2196f3,stroke-width:1px
    style GitHub fill:#f3e8f9,stroke:#9c27b0,stroke-width:1px
```

## Key Details
- .NET app runs **natively** under systemd (not in a Docker container)
- Only PostgreSQL runs in Docker
- Port 5432 is **not** exposed publicly — internal Docker network only
- HTTPS termination deferred (no load balancer / Nginx yet)
- Secrets loaded from `/home/ec2-user/clinic-scheduler/.env` (never committed)
