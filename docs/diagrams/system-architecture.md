# System Architecture — ClinicScheduler

> Paste into [mermaid.live](https://mermaid.live) to export as PNG/SVG for slides.

```mermaid
graph TB
    subgraph Presentation ["Presentation Layer"]
        Web["<b>ClinicScheduler.Web</b><br/>ASP.NET Core Host<br/>Program.cs · DI · Middleware<br/>8 API Controllers<br/>Identity Auth"]
        Client["<b>ClinicScheduler.Web.Client</b><br/>Blazor WebAssembly<br/>Client-side interactivity"]
        Shared["<b>ClinicScheduler.Shared</b><br/>23 Blazor Pages (MudBlazor)<br/>Components · Layouts"]
    end

    subgraph Business ["Business Logic Layer"]
        Core["<b>ClinicScheduler.Core</b><br/>Domain Entities<br/>AppointmentSchedulingService<br/>IRepository&lt;T&gt; interfaces<br/>5 scheduling rules"]
    end

    subgraph Data ["Data Access Layer"]
        Infra["<b>ClinicScheduler.Infrastructure</b><br/>ClinicDbContext (EF Core 10)<br/>Repository&lt;T&gt; implementation<br/>Migrations · Audit logging<br/>DatabaseSeeder"]
    end

    subgraph Storage ["Storage"]
        PG[("PostgreSQL 17")]
    end

    subgraph Testing ["Test Projects"]
        CoreTests["<b>Core.Tests</b><br/>69 entity unit tests"]
        WebTests["<b>Web.Tests</b><br/>24 service unit tests<br/>53 integration tests<br/>(Testcontainers + WebAppFactory)"]
    end

    %% Dependency arrows (direction = "depends on")
    Web --> Core
    Web --> Infra
    Web --> Client
    Client --> Shared
    Shared --> Core
    Shared --> Infra
    Infra --> Core
    Infra --> PG

    CoreTests -.-> Core
    WebTests -.-> Web

    style Presentation fill:#e3f2fd,stroke:#1976d2,stroke-width:1px
    style Business fill:#e8f5e9,stroke:#388e3c,stroke-width:1px
    style Data fill:#fff3e0,stroke:#f57c00,stroke-width:1px
    style Storage fill:#fce4ec,stroke:#c62828,stroke-width:1px
    style Testing fill:#f3e5f5,stroke:#7b1fa2,stroke-width:1px,stroke-dasharray: 5 5
```

## Dependency Direction
All arrows point **inward** — Presentation → Business → Data. Core has **zero** external project dependencies (only `System.ComponentModel.Annotations`), which keeps domain logic portable and testable.

## Key Design Decisions
| Decision | Rationale |
|---|---|
| Business rules in `Core`, not controllers | Controllers stay thin; rules are testable without HTTP |
| `IRepository<T>` in Core, implementation in Infrastructure | Dependency inversion — Core defines contracts, Infrastructure fulfills them |
| Shared project for Blazor pages | Same UI runs in Server and WebAssembly render modes |
| Testcontainers for integration tests | Real PostgreSQL, no mocks — catches SQL/EF issues that unit tests miss |
