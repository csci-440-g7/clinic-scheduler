# CI/CD Pipeline — ClinicScheduler

> Paste into [mermaid.live](https://mermaid.live) to export as PNG/SVG for slides.

```mermaid
flowchart LR
    Push["Push to<br/><b>MVP</b> branch"] --> Checkout["Checkout<br/>code"]

    subgraph Build ["Build & Test (ubuntu-latest)"]
        Checkout --> Setup[".NET 10<br/>SDK setup"]
        Setup --> Restore["Restore<br/>Web + Core.Tests"]
        Restore --> BuildStep["Build<br/>(Release)"]
        BuildStep --> Test["Run<br/>Core.Tests<br/>(69 unit tests)"]
    end

    Test --> Gate{"Repo ==<br/>Bradly187/<br/>clinic-scheduler?"}
    Gate -->|Yes| SSH["SSH into<br/>EC2"]
    Gate -->|No| Skip["Skip deploy"]

    subgraph Deploy ["Deploy (EC2 t3.small)"]
        SSH --> Fetch["git fetch<br/>origin MVP"]
        Fetch --> Reset["git reset --hard<br/>origin/MVP"]
        Reset --> Native["start-native.sh<br/>dotnet publish +<br/>systemd restart"]
    end

    style Build fill:#e8f4e8,stroke:#4caf50,stroke-width:1px
    style Deploy fill:#e3f2fd,stroke:#2196f3,stroke-width:1px
```

## What's Covered
- ✅ `ClinicScheduler.Web` — build
- ✅ `ClinicScheduler.Core.Tests` — 69 entity unit tests

## What's Not Covered in Pipeline
- ❌ `ClinicScheduler.Web.Tests` — 24 service unit + 53 integration tests (require Docker/Testcontainers)
- ❌ Branch protection / PR gating (deploys on any push to MVP)
