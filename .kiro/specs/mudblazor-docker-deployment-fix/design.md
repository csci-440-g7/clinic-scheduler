# MudBlazor Docker Deployment Fix — Bugfix Design

## Overview

The ClinicScheduler Blazor application deployed on EC2 (52.72.1.65:8081) renders a blank login page because of a combination of deployment script errors and a missing `DefaultLayout` attribute. The deploy scripts pull from `origin main` instead of `origin MVP`, skip Docker cache invalidation, and display the wrong port. The `Routes.razor` component lacks `DefaultLayout="typeof(Layout.MainLayout)"`, so pages without an explicit `@layout` directive get no MudBlazor providers. This design formalizes each bug condition, the exact code changes, and the verification strategy.

## Glossary

- **Bug_Condition (C)**: The set of conditions that cause the blank-page deployment failure — wrong branch, stale Docker cache, missing DefaultLayout, incorrect port message, and no Docker cleanup
- **Property (P)**: The desired behavior — deploy scripts pull `MVP`, Docker rebuilds from scratch, all pages receive MudBlazor providers via DefaultLayout, output shows port 8081, and stale artifacts are pruned
- **Preservation**: Existing behaviors that must remain unchanged — authenticated page rendering, database container health, `.env` validation, middleware ordering (commit 78e16f6), and the `8081:8080` port mapping
- **Routes.razor**: The shared router component in `ClinicScheduler.Shared/Routes.razor` that maps URLs to page components via `RouteView`
- **MainLayout**: The layout component in `ClinicScheduler.Shared/Layout/MainLayout.razor` that provides MudBlazor providers (`MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`), the app bar, and the navigation drawer
- **deploy/start.sh**: The script that pulls latest code and rebuilds/restarts Docker containers on EC2
- **deploy/bootstrap.sh**: The one-time EC2 setup script that installs Docker, clones the repo, and creates `.env`

## Bug Details

### Bug Condition

The deployment failure manifests through six interrelated defects. The deploy scripts target the wrong Git branch, Docker caching preserves stale images, `Routes.razor` omits the `DefaultLayout` attribute, the start script prints the wrong port, and redeployments accumulate stale Docker artifacts.

**Formal Specification:**
```
FUNCTION isBugCondition(input)
  INPUT: input of type DeploymentExecution
  OUTPUT: boolean

  branchWrong     := input.gitPullBranch ≠ "MVP"
  cacheStale      := input.dockerBuildUsesCache = true
  noDefaultLayout := input.routeViewHasDefaultLayout = false
  portWrong       := input.outputPort ≠ input.dockerComposeExternalPort
  noCleanup       := input.isRedeployment AND input.prunesDanglingImages = false

  RETURN branchWrong OR cacheStale OR noDefaultLayout OR portWrong OR noCleanup
END FUNCTION
```

### Examples

- **Wrong branch (start.sh)**: User runs `bash deploy/start.sh` on EC2. The script executes `git pull origin main`. The `MVP` branch has the middleware fix (commit 78e16f6) but `main` does not. Result: EC2 runs stale code. Expected: `git pull origin MVP` fetches the latest code.
- **Wrong branch (bootstrap.sh)**: User runs bootstrap on a fresh EC2 instance. The script clones without `-b MVP` and pulls from `main`. Result: repo is on `main`. Expected: clone with `-b MVP` and pull from `MVP`.
- **Docker cache**: User runs `start.sh` after pulling correct code. `docker-compose up --build` reuses cached layers from the old build. Result: running container still has old code. Expected: `docker-compose build --no-cache` forces a fresh build.
- **Missing DefaultLayout**: User navigates to `/login` (or any page without `@layout`). `RouteView` has no `DefaultLayout`, so the page renders without `MainLayout`. MudBlazor providers are absent, MudBlazor components render as empty HTML. Result: blank page. Expected: `DefaultLayout="typeof(Layout.MainLayout)"` ensures all pages get MudBlazor providers.
- **Port mismatch**: `start.sh` prints `http://<IP>:8080` but `docker-compose.yml` maps `8081:8080`. User visits port 8080 and gets nothing. Expected: script prints port `8081`.
- **No cleanup**: User re-runs `start.sh` multiple times. Old images and containers accumulate on the 8 GB t3.small disk. Expected: script stops old containers and prunes dangling images before rebuilding.

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- Pages that already specify `@layout MainLayout` (e.g., `NotFound.razor`) must continue to render with the full layout including app bar, navigation drawer, and MudBlazor providers
- The `NotFound` route (`/not-found`) must continue to display a not-found message
- The PostgreSQL container must continue to start on internal port 5432 with health checks
- `bootstrap.sh` must continue to install Docker, Docker Compose, clone the repo, and create `.env` from `.env.example`
- The application must continue to auto-apply EF Core migrations, seed the database, enforce production password policy, and disable Swagger UI in Production mode
- `start.sh` must continue to reject startup when `.env` contains placeholder `changeme` values
- The middleware pipeline must continue to call `UseStaticFiles()` before `UseAuthentication()` and `UseAuthorization()` (commit 78e16f6)
- `docker-compose.yml` must continue to use the `"8081:8080"` port mapping

**Scope:**
All inputs that do NOT involve the six bug conditions should be completely unaffected by this fix. This includes:
- Direct browser navigation to pages with explicit `@layout` directives
- Database operations (migrations, seeding, queries)
- API controller endpoints (`/api/*`)
- Authentication and authorization flows
- The Dockerfile build stages and runtime configuration

## Hypothesized Root Cause

Based on the bug description and code inspection, the root causes are:

1. **Wrong branch in deploy scripts**: Both `deploy/start.sh` (line: `git pull origin main`) and `deploy/bootstrap.sh` (lines: `git pull origin main` and `git clone` without `-b MVP`) hardcode `main` as the target branch. The active development branch is `MVP`, where the middleware ordering fix (commit 78e16f6) lives. This is a simple configuration error — the scripts were written when `main` was the active branch.

2. **Docker layer caching**: `start.sh` runs `docker-compose up --build -d`, which uses Docker's default layer cache. If the Dockerfile's `COPY` layers haven't changed (same file checksums), Docker reuses the cached build stage even though the source code on disk has changed. The `--no-cache` flag is needed to force a full rebuild.

3. **Missing DefaultLayout on RouteView**: In `Routes.razor`, the `<RouteView RouteData="@routeData" />` component has no `DefaultLayout` attribute. In Blazor, when a page component does not declare `@layout SomeLayout`, the router falls back to `RouteView.DefaultLayout`. Since it's null, those pages render without any layout — meaning no `MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, or `MudSnackbarProvider`. MudBlazor components require these providers to render. None of the 20+ pages in `ClinicScheduler.Shared/Pages/` have explicit `@layout` directives (except `NotFound.razor` in the worktree), so every page is affected.

4. **Port mismatch in output message**: `start.sh` prints `http://${PUBLIC_IP}:8080` but `docker-compose.yml` maps `"8081:8080"` (host port 8081 → container port 8080). This is a copy-paste error from when the port mapping was changed.

5. **No Docker cleanup on redeploy**: `start.sh` goes straight to `docker-compose up --build -d` without stopping old containers or pruning dangling images. On a t3.small with 8 GB disk, repeated deployments accumulate stale layers.

## Correctness Properties

Property 1: Bug Condition - Deployment Produces Working Application

_For any_ deployment execution where the bug condition holds (wrong branch, stale cache, missing DefaultLayout, wrong port, or no cleanup), the fixed deploy scripts and Routes.razor SHALL produce a working deployment where: (a) the correct `MVP` branch code is pulled, (b) the Docker image is rebuilt without cache, (c) all pages render with MudBlazor providers via DefaultLayout, (d) the output message shows port 8081, and (e) stale Docker artifacts are pruned on redeployment.

**Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5, 2.6**

Property 2: Preservation - Existing Behavior Unchanged

_For any_ input where the bug condition does NOT hold (pages with explicit `@layout`, database operations, API endpoints, authentication flows, `.env` validation, middleware ordering), the fixed code SHALL produce the same result as the original code, preserving all existing functionality including layout rendering for explicitly-decorated pages, database container health, migration/seeding behavior, and the `8081:8080` port mapping.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8**

## Fix Implementation

### Changes Required

Assuming our root cause analysis is correct:

**File 1**: `ClinicScheduler/ClinicScheduler.Shared/Routes.razor`

**Change**: Add `DefaultLayout` attribute to `RouteView`

**Current code:**
```razor
<RouteView RouteData="@routeData" />
```

**Fixed code:**
```razor
<RouteView RouteData="@routeData" DefaultLayout="typeof(Layout.MainLayout)" />
```

This ensures every page that lacks an explicit `@layout` directive receives `MainLayout`, which provides all MudBlazor providers.

---

**File 2**: `deploy/start.sh`

**Specific Changes:**

1. **Fix branch**: Change `git pull origin main` to `git pull origin MVP`
2. **Add Docker cleanup**: Before rebuilding, stop existing containers and prune dangling images
3. **Force no-cache build**: Use `docker-compose build --no-cache` before `docker-compose up -d`
4. **Fix port in output**: Change `:8080` to `:8081` in the success message
5. **Update step numbering**: Adjust step numbers to reflect the new cleanup and build steps

**Full fixed script:**
```bash
#!/bin/bash
set -e

REPO_DIR="/home/ec2-user/clinic-scheduler"

echo "=== ClinicScheduler — Start / Update ==="

# Verify .env exists and has been filled in
if [ ! -f "$REPO_DIR/.env" ]; then
  echo "ERROR: .env not found. Run bootstrap.sh first, then edit .env."
  exit 1
fi

if grep -q "changeme" "$REPO_DIR/.env"; then
  echo "ERROR: .env still contains placeholder 'changeme' values."
  echo "       Edit $REPO_DIR/.env with real passwords before starting."
  exit 1
fi

# Pull latest code from MVP branch
echo "[1/4] Pulling latest code from MVP..."
git -C "$REPO_DIR" pull origin MVP

# Stop existing containers (if any)
echo "[2/4] Stopping existing containers and pruning old images..."
docker-compose -f "$REPO_DIR/docker-compose.yml" down || true
docker image prune -f || true

# Rebuild without cache to ensure fresh image
echo "[3/4] Building image (no cache)..."
docker-compose -f "$REPO_DIR/docker-compose.yml" --env-file "$REPO_DIR/.env" build --no-cache

# Start containers
echo "[4/4] Starting containers..."
docker-compose -f "$REPO_DIR/docker-compose.yml" --env-file "$REPO_DIR/.env" up -d

PUBLIC_IP=$(curl -sf http://169.254.169.254/latest/meta-data/public-ipv4 || echo "<public-ip>")
echo ""
echo "=== App is running! ==="
echo "  URL:  http://${PUBLIC_IP}:8081"
echo "  Logs: docker-compose -f $REPO_DIR/docker-compose.yml logs -f app"
echo "  Stop: docker-compose -f $REPO_DIR/docker-compose.yml down"
```

---

**File 3**: `deploy/bootstrap.sh`

**Specific Changes:**

1. **Fix clone branch**: Add `-b MVP` to `git clone` command
2. **Fix pull branch**: Change `git pull origin main` to `git pull origin MVP`

**Current clone block:**
```bash
if [ -d "$REPO_DIR" ]; then
  echo "  Repo already exists at $REPO_DIR — pulling latest..."
  git -C "$REPO_DIR" pull origin main
else
  git clone https://github.com/csci-440-g7/clinic-scheduler.git "$REPO_DIR"
fi
```

**Fixed clone block:**
```bash
if [ -d "$REPO_DIR" ]; then
  echo "  Repo already exists at $REPO_DIR — pulling latest..."
  git -C "$REPO_DIR" pull origin MVP
else
  git clone -b MVP https://github.com/csci-440-g7/clinic-scheduler.git "$REPO_DIR"
fi
```

---

**File 4**: `DEPLOYMENT_NOTES.md`

**Specific Changes:**

1. **Step 3 — bootstrap curl URL**: Change branch from `main` to `MVP` in the raw GitHub URL
2. **Step 5 — description**: Note that `start.sh` now does a no-cache rebuild and Docker cleanup
3. **Step 6 — verify URL**: Change port from `8080` to `8081`
4. **Security group table**: Add port `8081` rule (or change `8080` to `8081`)
5. **Architecture diagram**: Change port from `8080` to `8081`
6. **Useful commands section**: Ensure consistency with port `8081`
7. **Request flow diagram**: Update to show port `8081`

## Testing Strategy

### Validation Approach

The testing strategy follows a two-phase approach: first, surface counterexamples that demonstrate the bug on unfixed code, then verify the fix works correctly and preserves existing behavior. Because the bugs are primarily in deployment scripts and Blazor routing configuration, testing is a mix of script inspection, Blazor component testing, and manual deployment verification.

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples that demonstrate the bug BEFORE implementing the fix. Confirm or refute the root cause analysis. If we refute, we will need to re-hypothesize.

**Test Plan**: Inspect the current deploy scripts and Routes.razor to confirm the defects exist. For the DefaultLayout bug, render the `Routes` component in a test harness without `DefaultLayout` and verify that pages lack MudBlazor providers.

**Test Cases**:
1. **Branch Check (start.sh)**: Grep `start.sh` for `git pull origin main` — confirms wrong branch (will find match on unfixed code)
2. **Branch Check (bootstrap.sh)**: Grep `bootstrap.sh` for `origin main` and confirm no `-b MVP` on clone — confirms wrong branch (will find match on unfixed code)
3. **Docker Cache Check**: Grep `start.sh` for `--no-cache` — confirms absence of cache-busting flag (will find no match on unfixed code)
4. **DefaultLayout Check**: Inspect `Routes.razor` for `DefaultLayout` attribute — confirms it is missing (will find no match on unfixed code)
5. **Port Check**: Grep `start.sh` for `:8080` in the output message — confirms wrong port (will find match on unfixed code)
6. **Cleanup Check**: Grep `start.sh` for `docker image prune` or `docker-compose down` before build — confirms no cleanup (will find no match on unfixed code)

**Expected Counterexamples**:
- `start.sh` contains `git pull origin main` instead of `git pull origin MVP`
- `bootstrap.sh` contains `git pull origin main` and `git clone` without `-b MVP`
- `start.sh` lacks `--no-cache` flag
- `Routes.razor` lacks `DefaultLayout` attribute
- `start.sh` prints port `8080` instead of `8081`
- `start.sh` has no Docker cleanup commands

### Fix Checking

**Goal**: Verify that for all inputs where the bug condition holds, the fixed files produce the expected behavior.

**Pseudocode:**
```
FOR ALL input WHERE isBugCondition(input) DO
  result := deployWithFixedFiles(input)
  ASSERT result.gitPullBranch = "MVP"
     AND result.dockerBuildUsedNoCache = true
     AND result.allPagesHaveDefaultLayout = true
     AND result.outputPort = 8081
     AND result.staleArtifactsPruned = true
END FOR
```

### Preservation Checking

**Goal**: Verify that for all inputs where the bug condition does NOT hold, the fixed code produces the same result as the original code.

**Pseudocode:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT originalBehavior(input) = fixedBehavior(input)
END FOR
```

**Testing Approach**: Property-based testing is recommended for preservation checking because:
- It generates many test cases automatically across the input domain
- It catches edge cases that manual unit tests might miss
- It provides strong guarantees that behavior is unchanged for all non-buggy inputs

**Test Plan**: Observe behavior on UNFIXED code first for non-bug inputs (pages with explicit layouts, database operations, `.env` validation), then write tests capturing that behavior.

**Test Cases**:
1. **Explicit Layout Preservation**: Verify that pages with `@layout MainLayout` (e.g., `NotFound.razor`) continue to render identically with the full layout after adding `DefaultLayout` to `RouteView`
2. **`.env` Validation Preservation**: Verify that `start.sh` still rejects startup when `.env` contains `changeme` values — the validation logic is untouched by the fix
3. **Docker Compose Config Preservation**: Verify that `docker-compose.yml` port mapping (`8081:8080`), database health check, and environment variable passthrough are unchanged
4. **Middleware Order Preservation**: Verify that `Program.cs` still calls `UseStaticFiles()` before `UseAuthentication()` and `UseAuthorization()` — this code is not modified by the fix

### Unit Tests

- Verify `Routes.razor` has `DefaultLayout="typeof(Layout.MainLayout)"` attribute on `RouteView`
- Verify `start.sh` contains `git pull origin MVP` (not `main`)
- Verify `start.sh` contains `--no-cache` in the docker-compose build command
- Verify `start.sh` contains `docker-compose down` and `docker image prune` before rebuild
- Verify `start.sh` output message references port `8081`
- Verify `bootstrap.sh` contains `git pull origin MVP` and `git clone -b MVP`

### Property-Based Tests

- Generate random page components (with and without explicit `@layout`) and verify all receive MudBlazor providers when routed through the fixed `Routes.razor`
- Generate random `.env` file contents and verify `start.sh` validation logic accepts/rejects correctly (unchanged behavior)
- Generate random deployment scenarios (first run vs. redeployment) and verify cleanup runs only on redeployment

### Integration Tests

- **Full deployment test**: SSH into EC2, run the fixed `start.sh`, and verify the app is accessible at `http://52.72.1.65:8081`
- **Login page test**: Navigate to `http://52.72.1.65:8081/login` and verify MudBlazor components render (non-blank page with styled login form)
- **Authenticated page test**: Log in with `admin@clinic.com` and verify the dashboard renders with the app bar, navigation drawer, and MudBlazor-styled content
- **Docker cleanup test**: Run `start.sh` twice and verify `docker images` shows no dangling images after the second run
