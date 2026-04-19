# Implementation Plan

- [x] 1. Write bug condition exploration test
  - **Property 1: Bug Condition** - Deployment Configuration Defects
  - **IMPORTANT**: Write this test BEFORE implementing any fixes
  - **GOAL**: Surface counterexamples that demonstrate the six deployment bugs exist in the unfixed code
  - **Scoped Approach**: Write a test script or test class that inspects the current source files for the known defects:
    - Assert `Routes.razor` contains `DefaultLayout` attribute on `RouteView` → expect FAIL (attribute is missing)
    - Assert `deploy/start.sh` contains `git pull origin MVP` → expect FAIL (contains `git pull origin main`)
    - Assert `deploy/start.sh` contains `--no-cache` in docker build command → expect FAIL (flag is absent)
    - Assert `deploy/start.sh` contains `docker-compose.*down` or `docker image prune` before build → expect FAIL (no cleanup)
    - Assert `deploy/start.sh` output message references port `8081` → expect FAIL (references `8080`)
    - Assert `deploy/bootstrap.sh` contains `git pull origin MVP` → expect FAIL (contains `git pull origin main`)
    - Assert `deploy/bootstrap.sh` contains `-b MVP` in git clone command → expect FAIL (flag is absent)
  - Run test on UNFIXED code — expect FAILURE (this confirms all six bugs exist)
  - Document counterexamples found for each defect
  - Mark task complete when test is written, run, and failures are documented
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

- [x] 2. Write preservation tests (BEFORE implementing fix)
  - **Property 2: Preservation** - Existing Behavior Unchanged
  - **IMPORTANT**: Follow observation-first methodology — observe behavior on UNFIXED code first
  - Run the existing test suite (`dotnet test`) and record that all 146 tests pass on unfixed code
  - Observe: `deploy/start.sh` still validates `.env` for `changeme` values (lines 11-17 unchanged)
  - Observe: `docker-compose.yml` port mapping is `"8081:8080"` (unchanged)
  - Observe: `docker-compose.yml` PostgreSQL health check is present (unchanged)
  - Observe: `deploy/bootstrap.sh` still installs Docker, Docker Compose, clones repo, creates `.env` (steps 1-4 unchanged)
  - Observe: `Routes.razor` `<NotFound>` block still renders `<h3>Page not found</h3>` (unchanged)
  - Write assertions capturing these observed behaviors as a baseline
  - Verify all preservation assertions pass on UNFIXED code
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

- [x] 3. Fix Routes.razor — Add DefaultLayout attribute
  - [x] 3.1 Add `DefaultLayout="typeof(Layout.MainLayout)"` to `RouteView` in `ClinicScheduler/ClinicScheduler.Shared/Routes.razor`
    - Change `<RouteView RouteData="@routeData" />` to `<RouteView RouteData="@routeData" DefaultLayout="typeof(Layout.MainLayout)" />`
    - This ensures all pages without an explicit `@layout` directive receive `MainLayout` with MudBlazor providers
    - _Bug_Condition: isBugCondition_Rendering(X) where X.page.hasExplicitLayoutDirective = false_
    - _Expected_Behavior: All pages receive MudThemeProvider, MudPopoverProvider, MudDialogProvider, MudSnackbarProvider via MainLayout_
    - _Preservation: Pages with explicit @layout MainLayout continue to render identically_
    - _Requirements: 2.4, 3.1, 3.2_

- [x] 4. Fix deploy/start.sh — Correct branch, add cleanup, no-cache build, fix port

  - [x] 4.1 Change `git pull origin main` to `git pull origin MVP`
    - _Bug_Condition: isBugCondition_Branch(X) where X.gitPullTargetBranch ≠ "MVP"_
    - _Expected_Behavior: Script pulls from MVP branch to get latest code including middleware fix_
    - _Requirements: 2.1_

  - [x] 4.2 Add Docker cleanup before rebuild — `docker-compose down` and `docker image prune -f`
    - Insert cleanup step after git pull and before docker build
    - Use `|| true` on cleanup commands so they don't fail on first run
    - _Bug_Condition: isBugCondition_Cleanup(X) where X.isRedeployment = true_
    - _Expected_Behavior: Old containers stopped and dangling images pruned before rebuild_
    - _Requirements: 2.6_

  - [x] 4.3 Replace `docker-compose up --build -d` with separate `docker-compose build --no-cache` then `docker-compose up -d`
    - Ensures Docker image is rebuilt from scratch, incorporating all source code changes
    - _Bug_Condition: isBugCondition_DockerCache(X) where X.buildUsesCache = true_
    - _Expected_Behavior: Docker image rebuilt without cache, running container reflects latest source_
    - _Requirements: 2.3_

  - [x] 4.4 Fix port in output message — change `:8080` to `:8081`
    - Change `echo "  URL:  http://${PUBLIC_IP}:8080"` to `echo "  URL:  http://${PUBLIC_IP}:8081"`
    - _Bug_Condition: isBugCondition_PortMessage(X) where X.displayedPort ≠ X.actualDockerComposeExternalPort_
    - _Expected_Behavior: Output message shows port 8081 matching docker-compose.yml mapping_
    - _Requirements: 2.5_

  - [x] 4.5 Update step numbering to reflect new 4-step flow (pull → cleanup → build → start)
    - _Preservation: .env validation logic (lines 11-17) remains unchanged_
    - _Requirements: 2.1, 2.3, 2.5, 2.6, 3.6_

- [x] 5. Fix deploy/bootstrap.sh — Correct branch references

  - [x] 5.1 Change `git pull origin main` to `git pull origin MVP` in the existing-repo branch
    - _Bug_Condition: isBugCondition_Branch(X) where X.gitPullTargetBranch ≠ "MVP"_
    - _Expected_Behavior: Existing repo is updated from MVP branch_
    - _Requirements: 2.2_

  - [x] 5.2 Add `-b MVP` to `git clone` command for fresh installs
    - Change `git clone https://github.com/csci-440-g7/clinic-scheduler.git "$REPO_DIR"` to `git clone -b MVP https://github.com/csci-440-g7/clinic-scheduler.git "$REPO_DIR"`
    - _Bug_Condition: isBugCondition_Branch(X) where X.cloneBranch ≠ "MVP"_
    - _Expected_Behavior: Fresh clone checks out MVP branch directly_
    - _Preservation: Docker/Compose installation, .env setup, and output messages remain unchanged_
    - _Requirements: 2.2, 3.4_

  - [x] 5.3 Update bootstrap output message port from `8080` to `8081`
    - Change `echo "  App will be available at: http://${PUBLIC_IP}:8080"` to `echo "  App will be available at: http://${PUBLIC_IP}:8081"`
    - _Requirements: 2.5_

- [x] 6. Update DEPLOYMENT_NOTES.md — Correct branch and port references

  - [x] 6.1 Update Step 3 bootstrap curl URL from `main` to `MVP` branch
    - _Requirements: 2.2_

  - [x] 6.2 Update Step 5 description to note no-cache rebuild and Docker cleanup
    - _Requirements: 2.3, 2.6_

  - [x] 6.3 Update Step 6 verify URL from port `8080` to `8081`
    - _Requirements: 2.5_

  - [x] 6.4 Update security group table — change port `8080` to `8081`
    - _Requirements: 2.5_

  - [x] 6.5 Update architecture diagram and request flow — change port `8080` to `8081`
    - _Requirements: 2.5_

  - [x] 6.6 Update Access URL and all other port `8080` references to `8081`
    - _Requirements: 2.5_

- [x] 7. Verify fixes and run regression tests

  - [x] 7.1 Verify bug condition exploration test now passes
    - **Property 1: Expected Behavior** - Deployment Configuration Defects Fixed
    - **IMPORTANT**: Re-run the SAME test from task 1 — do NOT write a new test
    - The test from task 1 encodes the expected behavior for all six defects
    - When this test passes, it confirms all bug conditions are resolved
    - Run bug condition exploration test from step 1
    - **EXPECTED OUTCOME**: Test PASSES (confirms all bugs are fixed)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [x] 7.2 Verify preservation tests still pass
    - **Property 2: Preservation** - Existing Behavior Unchanged
    - **IMPORTANT**: Re-run the SAME tests from task 2 — do NOT write new tests
    - Run the existing test suite (`dotnet test`) and confirm all 146 tests still pass
    - Verify all preservation assertions still hold after fixes
    - **EXPECTED OUTCOME**: Tests PASS (confirms no regressions)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

- [x] 8. Checkpoint — Ensure all tests pass
  - Run full test suite: `dotnet test ClinicScheduler/ClinicScheduler.slnx`
  - Confirm all 146 existing tests pass
  - Confirm exploration test from task 1 now passes
  - Confirm preservation assertions from task 2 still pass
  - Verify no build warnings or errors
  - Ensure all tests pass, ask the user if questions arise
