# Bugfix Requirements Document

## Introduction

The ClinicScheduler Blazor application deployed on EC2 fails to render MudBlazor components (blank login page) because the running Docker image is cached from before a middleware ordering fix (commit 78e16f6). The middleware fix itself is already committed to the `MVP` branch, but the deploy scripts (`deploy/start.sh` and `deploy/bootstrap.sh`) pull from `origin main` instead of `origin MVP`, so EC2 never receives the latest code. Additionally, the deploy scripts do not force a no-cache Docker rebuild, so even after pulling the correct branch the stale image persists. A secondary application-level bug exists in `Routes.razor`, which is missing the `DefaultLayout` attribute — pages without an explicit `@layout` directive receive no MudBlazor providers. Finally, the deploy scripts contain a cosmetic port mismatch in their output messages and lack Docker artifact cleanup.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN `deploy/start.sh` is executed on EC2 THEN the system runs `git pull origin main` but the active development branch is `MVP`, so the latest code (including the middleware ordering fix from commit 78e16f6) is never pulled to the EC2 instance

1.2 WHEN `deploy/bootstrap.sh` is executed on a fresh or existing EC2 instance THEN the system runs `git pull origin main` (for an existing clone) instead of `origin MVP`, so the repository on EC2 remains on stale code that lacks the middleware fix

1.3 WHEN `deploy/start.sh` rebuilds the Docker image via `docker-compose up --build -d` THEN the system uses Docker's layer cache, which preserves the old image built before the middleware ordering fix, so the running container still has the defective middleware order even if the source code on disk is correct

1.4 WHEN any page is routed through `Routes.razor` and that page does not have an explicit `@layout` directive THEN the system renders the page without any layout because `RouteView` has no `DefaultLayout` attribute, causing all MudBlazor provider components (`MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`) to be absent from the component tree

1.5 WHEN `deploy/start.sh` completes successfully THEN the system prints `http://<EC2-IP>:8080` as the application URL, but the app is actually exposed on port `8081` (per `docker-compose.yml` mapping `"8081:8080"`), giving the user an incorrect URL

1.6 WHEN `deploy/start.sh` is re-run to update the application THEN the system executes `docker-compose up --build -d` without first removing old containers, dangling images, or stale Docker layers, causing disk waste on the constrained EC2 `t3.small` instance

### Expected Behavior (Correct)

2.1 WHEN `deploy/start.sh` is executed on EC2 THEN the system SHALL run `git pull origin MVP` so that the latest code from the active development branch (including the middleware ordering fix) is pulled to the EC2 instance

2.2 WHEN `deploy/bootstrap.sh` is executed on an existing EC2 instance THEN the system SHALL run `git pull origin MVP` (and clone with `-b MVP` for fresh installs) so the repository on EC2 tracks the correct branch

2.3 WHEN `deploy/start.sh` rebuilds the Docker image THEN the system SHALL execute `docker-compose build --no-cache` before starting containers, ensuring the Docker image is rebuilt from scratch and incorporates all source code changes including the middleware ordering fix

2.4 WHEN any page is routed through `Routes.razor` and that page does not have an explicit `@layout` directive THEN the system SHALL apply `MainLayout` as the `DefaultLayout` on the `RouteView` component, ensuring all MudBlazor providers are present in the component tree

2.5 WHEN `deploy/start.sh` completes successfully THEN the system SHALL print `http://<EC2-IP>:8081` as the application URL, matching the actual port mapping in `docker-compose.yml`

2.6 WHEN `deploy/start.sh` is re-run to update the application THEN the system SHALL stop existing containers and prune dangling Docker images before rebuilding, preventing stale artifact accumulation on the EC2 instance

### Unchanged Behavior (Regression Prevention)

3.1 WHEN authenticated pages that already specify `@layout MainLayout` are rendered THEN the system SHALL CONTINUE TO display those pages with the full `MainLayout` including the app bar, navigation drawer, and MudBlazor providers

3.2 WHEN the `NotFound` route is triggered THEN the system SHALL CONTINUE TO display a not-found message to the user

3.3 WHEN the PostgreSQL database container starts via `docker-compose` THEN the system SHALL CONTINUE TO be accessible on internal port `5432` with the configured credentials and health check

3.4 WHEN `deploy/bootstrap.sh` is run on a fresh EC2 instance THEN the system SHALL CONTINUE TO install Docker, Docker Compose, clone the repository, and create `.env` from `.env.example`

3.5 WHEN the application starts in Production mode THEN the system SHALL CONTINUE TO auto-apply EF Core migrations, seed the database, enforce the production password policy, and disable Swagger UI

3.6 WHEN `.env` contains placeholder `changeme` values THEN `deploy/start.sh` SHALL CONTINUE TO reject the start and display an error message

3.7 WHEN the middleware pipeline executes in the deployed application THEN the system SHALL CONTINUE TO call `UseStaticFiles()` before `UseAuthentication()` and `UseAuthorization()` (the fix from commit 78e16f6 that is already in the codebase)

3.8 WHEN `docker-compose.yml` maps the app container port THEN the system SHALL CONTINUE TO use the `"8081:8080"` mapping (port 8081 is intentional for the deployment)

---

## Bug Condition (Formal)

### Bug Condition Function — Wrong Branch in Deploy Scripts (Bugs 1 & 2)

```pascal
FUNCTION isBugCondition_Branch(X)
  INPUT: X of type DeployScriptExecution
  OUTPUT: boolean

  // Returns true when a deploy script pulls from a branch
  // other than the active development branch (MVP)
  RETURN X.gitPullTargetBranch ≠ "MVP"
END FUNCTION
```

### Property: Fix Checking — Branch

```pascal
// Property: Fix Checking — Deploy scripts pull the correct branch
FOR ALL X WHERE isBugCondition_Branch(X) DO
  config ← fixedDeployScript'(X)
  ASSERT config.gitPullTargetBranch = "MVP"
     AND config.cloneBranch = "MVP"
END FOR
```

### Preservation: Branch

```pascal
// Property: Preservation Checking — All other deploy script behavior unchanged
FOR ALL X WHERE NOT isBugCondition_Branch(X) DO
  ASSERT executeScript(X) = executeScript'(X)
END FOR
```

### Bug Condition Function — Docker Cache (Bug 3)

```pascal
FUNCTION isBugCondition_DockerCache(X)
  INPUT: X of type DockerBuildExecution
  OUTPUT: boolean

  // Returns true when docker-compose build uses cached layers
  // (i.e., does not use --no-cache flag)
  RETURN X.buildUsesCache = true
END FUNCTION
```

### Property: Fix Checking — Docker Cache

```pascal
// Property: Fix Checking — Docker image is rebuilt without cache
FOR ALL X WHERE isBugCondition_DockerCache(X) DO
  result ← executeStartScript'(X)
  ASSERT result.dockerBuildUsedNoCache = true
     AND result.runningImageReflectsLatestSource = true
END FOR
```

### Bug Condition Function — Missing DefaultLayout (Bug 4)

```pascal
FUNCTION isBugCondition_Rendering(X)
  INPUT: X of type PageRenderRequest
  OUTPUT: boolean

  // Returns true when the page has no explicit @layout directive
  // and relies on DefaultLayout from RouteView (which is missing)
  RETURN X.page.hasExplicitLayoutDirective = false
END FUNCTION
```

### Property: Fix Checking — Rendering

```pascal
// Property: Fix Checking — All pages receive MudBlazor providers
FOR ALL X WHERE isBugCondition_Rendering(X) DO
  result ← renderPage'(X)
  ASSERT result.hasMudThemeProvider = true
     AND result.hasMudPopoverProvider = true
     AND result.hasMudDialogProvider = true
     AND result.hasMudSnackbarProvider = true
     AND result.mudComponentsRenderCorrectly = true
END FOR
```

### Preservation: Rendering

```pascal
// Property: Preservation Checking — Pages with explicit @layout are unchanged
FOR ALL X WHERE NOT isBugCondition_Rendering(X) DO
  ASSERT renderPage(X) = renderPage'(X)
END FOR
```

### Bug Condition Function — Port Message Mismatch (Bug 5)

```pascal
FUNCTION isBugCondition_PortMessage(X)
  INPUT: X of type ScriptOutputMessage
  OUTPUT: boolean

  // Returns true when the script output message references a port
  // that differs from the actual docker-compose external port (8081)
  RETURN X.displayedPort ≠ X.actualDockerComposeExternalPort
END FUNCTION
```

### Property: Fix Checking — Port Message

```pascal
// Property: Fix Checking — Script output shows correct port
FOR ALL X WHERE isBugCondition_PortMessage(X) DO
  output ← executeStartScript'(X)
  ASSERT output.displayedPort = 8081
     AND output.displayedPort = output.actualDockerComposeExternalPort
END FOR
```

### Bug Condition Function — Docker Cleanup (Bug 6)

```pascal
FUNCTION isBugCondition_Cleanup(X)
  INPUT: X of type DeploymentUpdate
  OUTPUT: boolean

  // Returns true when start.sh is re-run (not first run)
  RETURN X.isRedeployment = true
END FUNCTION
```

### Property: Fix Checking — Docker Cleanup

```pascal
// Property: Fix Checking — Stale artifacts are cleaned up on redeploy
FOR ALL X WHERE isBugCondition_Cleanup(X) DO
  result ← executeStartScript'(X)
  ASSERT result.oldContainersStopped = true
     AND result.danglingImagesPruned = true
END FOR
```

### Preservation: Deployment

```pascal
// Property: Preservation Checking — First-run and other deployment behavior unchanged
FOR ALL X WHERE NOT isBugCondition_Branch(X)
           AND NOT isBugCondition_DockerCache(X)
           AND NOT isBugCondition_PortMessage(X)
           AND NOT isBugCondition_Cleanup(X) DO
  ASSERT deploy(X) = deploy'(X)
END FOR
```
