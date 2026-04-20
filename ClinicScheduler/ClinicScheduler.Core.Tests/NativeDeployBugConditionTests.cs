using System.Text.RegularExpressions;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;

namespace ClinicScheduler.Core.Tests;

/// <summary>
/// Bug condition exploration tests for the native EC2 deployment fix.
/// These tests assert the EXPECTED (fixed) behavior against the CURRENT (unfixed) source files.
/// On unfixed code, tests should FAIL — confirming the deploy scripts are missing native deployment
/// requirements (Docker artifact cleanup, correct systemd interpolation, etc.).
///
/// **Validates: Requirements 1.1, 1.2, 2.1, 2.2**
/// </summary>
public class NativeDeployBugConditionTests
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

    /// <summary>
    /// The deploy/start-native.sh script must exist and contain a dotnet publish command
    /// for building the app natively on the host (not inside Docker).
    /// **Validates: Requirements 2.1, 2.2**
    /// </summary>
    [Fact]
    public void StartNativeSh_Should_Exist_And_Contain_DotnetPublish()
    {
        var content = ReadFile(Path.Combine("deploy", "start-native.sh"));

        content.Should().Contain("dotnet publish",
            "start-native.sh must use 'dotnet publish' to build the app natively on the host");
    }

    /// <summary>
    /// The deploy/start-native.sh script must create a systemd service unit with ExecStart
    /// pointing to dotnet running ClinicScheduler.Web.dll.
    /// **Validates: Requirements 2.1, 2.2**
    /// </summary>
    [Fact]
    public void StartNativeSh_Should_Create_Systemd_Unit_With_ExecStart()
    {
        var content = ReadFile(Path.Combine("deploy", "start-native.sh"));

        content.Should().Contain("ExecStart",
            "start-native.sh must define an ExecStart directive in the systemd service unit");

        // ExecStart must reference dotnet and ClinicScheduler.Web.dll
        var execStartMatch = Regex.IsMatch(content, @"ExecStart=.*dotnet.*ClinicScheduler\.Web\.dll");
        execStartMatch.Should().BeTrue(
            "ExecStart must point to 'dotnet ... ClinicScheduler.Web.dll' for native execution");
    }

    /// <summary>
    /// The deploy/start-native.sh script must start only the db service via Docker Compose,
    /// NOT the app service. The app runs natively, not in Docker.
    /// **Validates: Requirements 2.1, 2.2**
    /// </summary>
    [Fact]
    public void StartNativeSh_Should_Start_Only_Db_Service()
    {
        var content = ReadFile(Path.Combine("deploy", "start-native.sh"));

        // Must contain "up -d db" to start only the database service
        var startsDbOnly = Regex.IsMatch(content, @"(docker-compose|docker\s+compose).*up\s+-d\s+db");
        startsDbOnly.Should().BeTrue(
            "start-native.sh must start only the 'db' service via Docker Compose (not the full stack)");

        // Must NOT contain "up -d" without specifying "db" (which would start all services)
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("echo ") || trimmed.StartsWith("#"))
                continue;

            if (Regex.IsMatch(trimmed, @"(docker-compose|docker\s+compose).*up\s+-d\s*$"))
            {
                // Found a bare "up -d" without specifying a service — this would start the app too
                Assert.Fail(
                    "start-native.sh must NOT use bare 'docker-compose up -d' (starts all services including app). " +
                    "Use 'docker-compose up -d db' to start only PostgreSQL.");
            }
        }
    }

    /// <summary>
    /// The deploy/start-native.sh script must clean up failed Docker build artifacts
    /// (stale app containers/images from previous Docker-based deployments).
    /// It should contain docker compose down or docker image prune commands.
    /// **Validates: Requirements 2.2**
    /// </summary>
    [Fact]
    public void StartNativeSh_Should_Cleanup_Docker_Build_Artifacts()
    {
        var content = ReadFile(Path.Combine("deploy", "start-native.sh"));

        var lines = content.Split('\n');
        var hasCleanupCommand = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            // Skip echo lines and comments — we need actual commands
            if (trimmed.StartsWith("echo ") || trimmed.StartsWith("#"))
                continue;

            if (Regex.IsMatch(trimmed, @"(docker-compose|docker\s+compose).*down") ||
                Regex.IsMatch(trimmed, @"docker\s+image\s+prune") ||
                Regex.IsMatch(trimmed, @"docker\s+(rm|rmi|container\s+rm|image\s+rm)"))
            {
                hasCleanupCommand = true;
                break;
            }
        }

        hasCleanupCommand.Should().BeTrue(
            "start-native.sh must clean up failed Docker build artifacts " +
            "(docker compose down / docker image prune / docker rm) to reclaim disk space " +
            "and remove stale app containers from previous Docker-based deployments");
    }

    /// <summary>
    /// The deploy/bootstrap.sh script must install dotnet-sdk-10.0 (the native SDK)
    /// so the app can be built and run outside Docker.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Fact]
    public void BootstrapSh_Should_Install_DotnetSdk10()
    {
        var content = ReadFile(Path.Combine("deploy", "bootstrap.sh"));

        content.Should().Contain("dotnet-sdk-10.0",
            "bootstrap.sh must install dotnet-sdk-10.0 for native app builds on the EC2 host");
    }

    /// <summary>
    /// Property-based test: For any generated .env variable values (passwords, environment names),
    /// the systemd unit template in start-native.sh must use Host=localhost (not Host=db)
    /// in the connection string. Host=db is the Docker network hostname; Host=localhost is correct
    /// for native execution where the app connects to PostgreSQL's exposed port on the host.
    /// **Validates: Requirements 2.1, 2.2**
    /// </summary>
    [Property(MaxTest = 50)]
    public bool StartNativeSh_SystemdUnit_Should_Use_HostLocalhost_ForAnyEnvValues(
        NonEmptyString password,
        NonEmptyString envName)
    {
        var content = ReadFile(Path.Combine("deploy", "start-native.sh"));

        // The systemd unit section should contain Host=localhost in the connection string
        var hasHostLocalhost = content.Contains("Host=localhost");

        // It must NOT use Host=db (Docker network hostname) in the systemd unit
        var hasHostDb = Regex.IsMatch(content, @"Host=db[^a-zA-Z]|Host=db$");

        // For native deployment, the connection string MUST use Host=localhost
        // and must NOT use Host=db (Docker network hostname)
        return hasHostLocalhost && !hasHostDb;
    }
}
