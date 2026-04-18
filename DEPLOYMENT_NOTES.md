# ClinicScheduler — Deployment Guide

## Architecture

```
Internet (port 8080)
    → EC2 t3.small  (us-east-1, Amazon Linux 2023)
        └── Docker Compose
               ├── ClinicScheduler.Web  — ASP.NET Core 10 Blazor app (port 8080)
               └── PostgreSQL 17-alpine — database (port 5432, internal only)
```

- **Region:** us-east-1
- **Key pair:** `clinic-capstone-key` (`.pem` stored locally)
- **Access:** `http://<EC2-public-IP>:8080`
- **Domain / HTTPS:** deferred — wire up Nginx + Let's Encrypt when ready

---

## Phase 2 Checklist

- [ ] Step 1 — Launch EC2 instance
- [ ] Step 2 — Configure security group
- [ ] Step 3 — SSH and run bootstrap
- [ ] Step 4 — Edit `.env` with real passwords
- [ ] Step 5 — Start the app
- [ ] Step 6 — Verify

---

## Step 1 — Launch EC2 Instance

1. Open [AWS Console → EC2 → Launch Instance](https://us-east-1.console.aws.amazon.com/ec2/home?region=us-east-1#LaunchInstances:)
2. Fill in:
   - **Name:** `clinic-scheduler`
   - **AMI:** Amazon Linux 2023 AMI *(free tier eligible — search "Amazon Linux 2023")*
   - **Instance type:** `t3.small`
   - **Key pair:** select `clinic-capstone-key` *(your existing key)*
3. Under **Network settings → Firewall**, click **Create security group** and name it `clinic-scheduler-sg`.  
   Add these inbound rules (outbound can stay as default Allow All):

   | Type | Protocol | Port | Source | Purpose |
   |---|---|---|---|---|
   | SSH | TCP | 22 | My IP | Admin access |
   | Custom TCP | TCP | 8080 | 0.0.0.0/0 | App (HTTP) |

4. Under **Configure storage:** keep default (8 GB gp3 is fine).
5. Click **Launch instance**.
6. Once running, note the **Public IPv4 address** from the instance detail page — this is your app URL.

---

## Step 2 — SSH Into the Instance

From your local machine (PowerShell or bash):

```bash
ssh -i "C:\painpi\clinic-capstone-key.pem" ec2-user@<EC2-public-IP>
```

If you get a permissions error on the `.pem` file in PowerShell:

```powershell
icacls "C:\painpi\clinic-capstone-key.pem" /inheritance:r /grant:r "$($env:USERNAME):(R)"
```

---

## Step 3 — Run Bootstrap Script

Once logged in to EC2, run the bootstrap in one command:

```bash
curl -fsSL https://raw.githubusercontent.com/csci-440-g7/clinic-scheduler/main/deploy/bootstrap.sh | bash
```

Or if you prefer to inspect it first:

```bash
# Clone repo manually, then run
sudo dnf install -y git
git clone https://github.com/csci-440-g7/clinic-scheduler.git /home/ec2-user/clinic-scheduler
bash /home/ec2-user/clinic-scheduler/deploy/bootstrap.sh
```

The bootstrap script:
- Installs Docker and Docker Compose
- Clones the repo to `/home/ec2-user/clinic-scheduler`
- Copies `.env.example` → `.env` (if `.env` doesn't already exist)

**After bootstrap, log out and back in** so the docker group takes effect:

```bash
exit
ssh -i "C:\painpi\clinic-capstone-key.pem" ec2-user@<EC2-public-IP>
```

---

## Step 4 — Edit `.env` With Real Passwords

```bash
nano /home/ec2-user/clinic-scheduler/.env
```

Set these values:

```env
POSTGRES_PASSWORD=<any strong password — e.g. Pg$Clinic2026>
SEED_ADMIN_PASSWORD=<min 10 chars, uppercase, digit, special char — e.g. MyAdmin@2026!>
ASPNETCORE_ENVIRONMENT=Production
```

Save with `Ctrl+O`, exit with `Ctrl+X`.

> **Password policy (Production):** `SEED_ADMIN_PASSWORD` must have ≥ 10 characters,
> at least one uppercase letter, one digit, and one special character.
> The app throws `InvalidOperationException` on startup if this variable is missing.

---

## Step 5 — Start the App

```bash
bash /home/ec2-user/clinic-scheduler/deploy/start.sh
```

This script:
1. Validates `.env` has no placeholder `changeme` values
2. Pulls latest code from `main`
3. Runs `docker-compose up --build -d`

First run takes 2–3 minutes to build the image. Subsequent runs are faster (layer cache).

---

## Step 6 — Verify

```bash
# Check containers are running
docker ps

# Watch startup logs (Ctrl+C to stop following)
docker-compose -f /home/ec2-user/clinic-scheduler/docker-compose.yml logs -f app
```

Open in a browser: `http://<EC2-public-IP>:8080`

You should see the login page. Log in with:

| Role | Email | Password |
|---|---|---|
| Admin | admin@clinic.com | *(your `SEED_ADMIN_PASSWORD`)* |
| Clinic Manager | manager@clinic.com | `Manager@1234` |
| Therapist | therapist@clinic.com | `Therapist@1234` |
| Staff | staff@clinic.com | `Staff@Clinic1` |
| Patient | patient@clinic.com | `Patient@1234` |

---

## Updating the App After Code Changes

Once Phase 3 (CI/CD) is set up, pushes to `main` will deploy automatically.  
Until then, update manually:

```bash
bash /home/ec2-user/clinic-scheduler/deploy/start.sh
```

---

## Useful Commands on EC2

```bash
# View running containers
docker ps

# Follow app logs
docker-compose -f ~/clinic-scheduler/docker-compose.yml logs -f app

# Stop everything
docker-compose -f ~/clinic-scheduler/docker-compose.yml down

# Stop and wipe database volume (full reset)
docker-compose -f ~/clinic-scheduler/docker-compose.yml down -v

# Connect to PostgreSQL directly
docker exec -it $(docker ps -qf "name=db") psql -U postgres -d clinic_scheduler
```

---

## Architecture Summary

### Request Flow

```
Browser → EC2:8080 → Docker: ClinicScheduler.Web (ASP.NET Core)
                               ├── Blazor Server (SignalR)
                               ├── REST API Controllers (/api/*)
                               └── EF Core → PostgreSQL (Docker internal network)
```

### Key Configuration Points

| What | Where |
|---|---|
| Connection string | `docker-compose.yml` env var → `${POSTGRES_PASSWORD}` |
| Admin password | `docker-compose.yml` env var → `${SEED_ADMIN_PASSWORD}` |
| Environment | `docker-compose.yml` env var → `${ASPNETCORE_ENVIRONMENT}` |
| All runtime secrets | `/home/ec2-user/clinic-scheduler/.env` (never committed) |
| Migrations | Auto-applied by `db.Database.Migrate()` on startup |
| Seed data | Applied once by `DatabaseSeeder.SeedAsync` when DB is empty |
| Swagger UI | Disabled in Production — only available in Development |

---

## Security Notes

- `appsettings.json` contains a local dev connection string — never used in production (env var overrides it)
- HTTPS redirect is skipped in Production — the app assumes a load balancer handles TLS termination
- Swagger UI is disabled in Production
- ASP.NET Core Identity password policy is enforced in Production (10 chars, complexity required)
- Port 5432 (PostgreSQL) is not exposed publicly — internal Docker network only
- Port 22 (SSH) should be restricted to your IP in the security group

---

## Next Steps (Phase 3 — CI/CD)

See Phase 3 plan: create `.github/workflows/deploy.yml` and add these GitHub repo secrets:

| Secret | Value |
|---|---|
| `EC2_HOST` | EC2 public IP |
| `EC2_USER` | `ec2-user` |
| `EC2_SSH_KEY` | Full contents of `clinic-capstone-key.pem` |
