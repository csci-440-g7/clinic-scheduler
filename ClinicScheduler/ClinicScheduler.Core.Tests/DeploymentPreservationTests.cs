using System.Text.RegularExpressions;
using FluentAssertions;

namespace ClinicScheduler.Core.Tests;

/// <summary>
/// Preservation tests for the MudBlazor Docker deployment fix.
/// These tests capture baseline behaviors that must NOT change after the fix is applied.
/// All assertions here must pass on BOTH unfixed and fixed code.
///
/// **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8**
/// </summary>
public class DeploymentPreservationTests
{
    // Resolve paths relative to the test assembly location, walking up to the repo root.
    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "deploy")) &&
                Directory.Exists(Path.Combine(dir, "ClinicScheduler")))
            {
                return dir;
            }
            dir = Directory.GetParent(dir)?.FullName;
        }

        var fallback = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return fallback;
    }

    private static string ReadFile(string relativePath)
    {
        var fullPath = Path.Combine(RepoRoot, relativePath);
        File.Exists(fullPath).Should().BeTrue($"Expected file at {fullPath} to exist");
        return File.ReadAllText(fullPath);
    }

    // ── Requirement 3.6: start.sh .env validation ──────────────────────────

    /// <summary>
    /// Preservation: start.sh checks that .env file exists before proceeding.
    /// **Validates: Requirements 3.6**
    /// </summary>
    [Fact]
    public void StartSh_Should_Validate_EnvFile_Exists()
    {
        var content = ReadFile(Path.Combine("deploy", "start.sh"));

        content.Should().Contain("if [ ! -f \"$REPO_DIR/.env\" ]",
            "start.sh must check for .env file existence before starting");
    }

    /// <summary>
    /// Preservation: start.sh rejects startup when .env contains 'changeme' placeholder values.
    /// **Validates: Requirements 3.6**
    /// </summary>
    [Fact]
    public void StartSh_Should_Reject_Changeme_Values_In_Env()
    {
        var content = ReadFile(Path.Combine("deploy", "start.sh"));

        content.Should().Contain("grep -q \"changeme\"",
            "start.sh must check for 'changeme' placeholder values in .env");
        content.Should().Contain("ERROR: .env still contains placeholder",
            "start.sh must display an error when .env has changeme values");
    }

    // ── Requirement 3.8: docker-compose.yml port mapping ────────────────────

    /// <summary>
    /// Preservation: docker-compose.yml maps host port 8081 to container port 8080.
    /// **Validates: Requirements 3.8**
    /// </summary>
    [Fact]
    public void DockerCompose_Should_Map_Port_8081_To_8080()
    {
        var content = ReadFile("docker-compose.yml");

        content.Should().Contain("\"8081:8080\"",
            "docker-compose.yml must map host port 8081 to container port 8080");
    }

    // ── Requirement 3.3: PostgreSQL health check ────────────────────────────

    /// <summary>
    /// Preservation: docker-compose.yml has a PostgreSQL health check using pg_isready.
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Fact]
    public void DockerCompose_Should_Have_PostgreSQL_HealthCheck()
    {
        var content = ReadFile("docker-compose.yml");

        content.Should().Contain("healthcheck",
            "docker-compose.yml must define a healthcheck for the database service");
        content.Should().Contain("pg_isready",
            "docker-compose.yml healthcheck must use pg_isready to verify PostgreSQL is ready");
    }

    // ── Requirement 3.4: bootstrap.sh installs Docker, Compose, clones repo, creates .env ──

    /// <summary>
    /// Preservation: bootstrap.sh installs Docker.
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Fact]
    public void BootstrapSh_Should_Install_Docker()
    {
        var content = ReadFile(Path.Combine("deploy", "bootstrap.sh"));

        content.Should().Contain("sudo dnf install -y docker",
            "bootstrap.sh must install Docker via dnf");
        content.Should().Contain("sudo systemctl enable --now docker",
            "bootstrap.sh must enable and start the Docker service");
    }

    /// <summary>
    /// Preservation: bootstrap.sh installs Docker Compose standalone binary.
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Fact]
    public void BootstrapSh_Should_Install_DockerCompose()
    {
        var content = ReadFile(Path.Combine("deploy", "bootstrap.sh"));

        content.Should().Contain("docker-compose-linux-x86_64",
            "bootstrap.sh must download the Docker Compose standalone binary");
        content.Should().Contain("/usr/local/bin/docker-compose",
            "bootstrap.sh must install docker-compose to /usr/local/bin");
    }

    /// <summary>
    /// Preservation: bootstrap.sh clones the repository.
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Fact]
    public void BootstrapSh_Should_Clone_Repository()
    {
        var content = ReadFile(Path.Combine("deploy", "bootstrap.sh"));

        content.Should().Contain("git clone",
            "bootstrap.sh must clone the repository for fresh installs");
        content.Should().Contain("csci-440-g7/clinic-scheduler",
            "bootstrap.sh must clone from the correct GitHub repository");
    }

    /// <summary>
    /// Preservation: bootstrap.sh creates .env from .env.example.
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Fact]
    public void BootstrapSh_Should_Create_Env_From_Example()
    {
        var content = ReadFile(Path.Combine("deploy", "bootstrap.sh"));

        content.Should().Contain("cp \"$REPO_DIR/.env.example\" \"$ENV_FILE\"",
            "bootstrap.sh must copy .env.example to .env for fresh installs");
    }

    // ── Requirement 3.2: Routes.razor NotFound block ────────────────────────

    /// <summary>
    /// Preservation: Routes.razor NotFound block renders "Page not found" heading.
    /// **Validates: Requirements 3.2**
    /// </summary>
    [Fact]
    public void RoutesRazor_NotFound_Should_Render_PageNotFound_Heading()
    {
        var content = ReadFile(Path.Combine("ClinicScheduler", "ClinicScheduler.Shared", "Routes.razor"));

        content.Should().Contain("<NotFound>",
            "Routes.razor must have a <NotFound> block for unmatched routes");
        content.Should().Contain("<h3>Page not found</h3>",
            "Routes.razor NotFound block must render 'Page not found' heading");
    }

    // ── Requirement 3.7: Middleware ordering in Program.cs ──────────────────

    /// <summary>
    /// Preservation: Program.cs calls UseStaticFiles() before UseAuthentication() and UseAuthorization().
    /// This is the middleware ordering fix from commit 78e16f6 that must be preserved.
    /// **Validates: Requirements 3.7**
    /// </summary>
    [Fact]
    public void ProgramCs_Should_Have_UseStaticFiles_Before_Auth_Middleware()
    {
        var content = ReadFile(Path.Combine("ClinicScheduler", "ClinicScheduler.Web", "Program.cs"));

        // Find the positions of each middleware call
        var staticFilesIndex = content.IndexOf("UseStaticFiles()");
        var authenticationIndex = content.IndexOf("UseAuthentication()");
        var authorizationIndex = content.IndexOf("UseAuthorization()");

        staticFilesIndex.Should().BeGreaterThan(-1,
            "Program.cs must call UseStaticFiles()");
        authenticationIndex.Should().BeGreaterThan(-1,
            "Program.cs must call UseAuthentication()");
        authorizationIndex.Should().BeGreaterThan(-1,
            "Program.cs must call UseAuthorization()");

        staticFilesIndex.Should().BeLessThan(authenticationIndex,
            "UseStaticFiles() must be called before UseAuthentication() in the middleware pipeline");
        staticFilesIndex.Should().BeLessThan(authorizationIndex,
            "UseStaticFiles() must be called before UseAuthorization() in the middleware pipeline");
    }
}
