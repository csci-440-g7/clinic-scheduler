using System.Text.RegularExpressions;
using FluentAssertions;

namespace ClinicScheduler.Core.Tests;

/// <summary>
/// Bug condition exploration tests for the MudBlazor Docker deployment fix.
/// These tests assert the EXPECTED (fixed) behavior against the CURRENT (unfixed) source files.
/// On unfixed code, all tests should FAIL — confirming the six deployment bugs exist.
///
/// **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 1.6**
/// </summary>
public class DeploymentBugConditionTests
{
    // Resolve paths relative to the test assembly location, walking up to the repo root.
    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        // Start from the test assembly's directory and walk up until we find the deploy/ folder
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

        // Fallback: try common relative paths from bin/Debug/net10.0
        var fallback = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return fallback;
    }

    private static string ReadFile(string relativePath)
    {
        var fullPath = Path.Combine(RepoRoot, relativePath);
        File.Exists(fullPath).Should().BeTrue($"Expected file at {fullPath} to exist");
        return File.ReadAllText(fullPath);
    }

    /// <summary>
    /// Bug 4 (Requirement 1.4): Routes.razor is missing DefaultLayout attribute on RouteView.
    /// Pages without an explicit @layout directive get no MudBlazor providers.
    /// Expected (fixed): RouteView has DefaultLayout="typeof(Layout.MainLayout)"
    /// </summary>
    [Fact]
    public void RoutesRazor_Should_Contain_DefaultLayout_Attribute()
    {
        var content = ReadFile(Path.Combine("ClinicScheduler", "ClinicScheduler.Shared", "Routes.razor"));

        content.Should().Contain("DefaultLayout",
            "RouteView must have a DefaultLayout attribute so pages without @layout get MainLayout");
    }

    /// <summary>
    /// Bug 1 (Requirement 1.1): start.sh pulls from 'main' instead of 'MVP'.
    /// The middleware fix lives on the MVP branch.
    /// Expected (fixed): git pull origin MVP
    /// </summary>
    [Fact]
    public void StartSh_Should_Pull_From_MVP_Branch()
    {
        var content = ReadFile(Path.Combine("deploy", "start.sh"));

        content.Should().Contain("pull origin MVP",
            "start.sh must pull from the MVP branch where the middleware fix lives");
    }

    /// <summary>
    /// Bug 3 (Requirement 1.3): start.sh uses Docker cache, preserving stale images.
    /// Expected (fixed): docker-compose build --no-cache
    /// </summary>
    [Fact]
    public void StartSh_Should_Use_NoCache_Docker_Build()
    {
        var content = ReadFile(Path.Combine("deploy", "start.sh"));

        content.Should().Contain("--no-cache",
            "start.sh must use --no-cache to force a fresh Docker image build");
    }

    /// <summary>
    /// Bug 6 (Requirement 1.6): start.sh does not clean up old containers/images before rebuild.
    /// Expected (fixed): docker-compose down or docker image prune before the build command
    /// The cleanup must be an actual command, not just an echo/comment referencing "down".
    /// </summary>
    [Fact]
    public void StartSh_Should_Contain_Docker_Cleanup_Before_Build()
    {
        var content = ReadFile(Path.Combine("deploy", "start.sh"));

        // Look for an actual docker-compose down command (not inside an echo statement)
        // and docker image prune command before the build step.
        // The unfixed script only has "docker-compose ... down" inside an echo "  Stop: ..." line.
        var lines = content.Split('\n');
        var hasCleanupCommand = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            // Skip echo lines and comments — we need actual commands
            if (trimmed.StartsWith("echo ") || trimmed.StartsWith("#"))
                continue;

            if (Regex.IsMatch(trimmed, @"docker-compose.*down") || trimmed.Contains("docker image prune"))
            {
                hasCleanupCommand = true;
                break;
            }
        }

        hasCleanupCommand.Should().BeTrue(
            "start.sh must have actual cleanup commands (docker-compose down / docker image prune) before rebuilding, not just echo references");
    }

    /// <summary>
    /// Bug 5 (Requirement 1.5): start.sh output message references port 8080 instead of 8081.
    /// docker-compose.yml maps 8081:8080, so the external port is 8081.
    /// Expected (fixed): output message shows port 8081
    /// </summary>
    [Fact]
    public void StartSh_Should_Reference_Port_8081_In_Output()
    {
        var content = ReadFile(Path.Combine("deploy", "start.sh"));

        // The URL output line should reference port 8081, not 8080
        content.Should().Contain(":8081",
            "start.sh output message must reference port 8081 to match docker-compose.yml mapping");
    }

    /// <summary>
    /// Bug 2 (Requirement 1.2): bootstrap.sh pulls from 'main' instead of 'MVP'.
    /// Expected (fixed): git pull origin MVP
    /// </summary>
    [Fact]
    public void BootstrapSh_Should_Pull_From_MVP_Branch()
    {
        var content = ReadFile(Path.Combine("deploy", "bootstrap.sh"));

        content.Should().Contain("pull origin MVP",
            "bootstrap.sh must pull from the MVP branch for existing repos");
    }

    /// <summary>
    /// Bug 2 (Requirement 1.2): bootstrap.sh clones without -b MVP flag.
    /// Expected (fixed): git clone -b MVP
    /// </summary>
    [Fact]
    public void BootstrapSh_Should_Clone_With_MVP_Branch_Flag()
    {
        var content = ReadFile(Path.Combine("deploy", "bootstrap.sh"));

        content.Should().Contain("-b MVP",
            "bootstrap.sh must clone with -b MVP to check out the correct branch on fresh installs");
    }
}
