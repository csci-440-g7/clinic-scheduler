using System.Text.RegularExpressions;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;

namespace ClinicScheduler.Core.Tests;

/// <summary>
/// Preservation property tests for the native EC2 deployment fix.
/// These tests capture the CURRENT (unfixed) baseline behavior that MUST be preserved
/// after the fix is applied. All tests should PASS on both unfixed and fixed code.
///
/// **Validates: Requirements 3.1, 3.2, 3.3, 3.4**
/// </summary>
public class NativeDeployPreservationTests
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

    // ─── Property: docker-compose.yml PostgreSQL configuration is preserved ───

    /// <summary>
    /// docker-compose.yml must always contain the postgres:17-alpine image for the db service.
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Fact]
    public void DockerCompose_Should_Use_Postgres17Alpine_Image()
    {
        var content = ReadFile("docker-compose.yml");

        content.Should().Contain("postgres:17-alpine",
            "docker-compose.yml must use the postgres:17-alpine image for the db service");
    }

    /// <summary>
    /// docker-compose.yml must always define the clinic_db_data named volume for PostgreSQL persistence.
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Fact]
    public void DockerCompose_Should_Define_ClinicDbData_Volume()
    {
        var content = ReadFile("docker-compose.yml");

        content.Should().Contain("clinic_db_data",
            "docker-compose.yml must define the clinic_db_data volume for PostgreSQL data persistence");

        // Verify it's used in the db service volumes section
        content.Should().Contain("clinic_db_data:/var/lib/postgresql/data",
            "docker-compose.yml must mount clinic_db_data to /var/lib/postgresql/data");
    }

    /// <summary>
    /// docker-compose.yml must always include a pg_isready health check for the db service.
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Fact]
    public void DockerCompose_Should_Have_PgIsReady_HealthCheck()
    {
        var content = ReadFile("docker-compose.yml");

        content.Should().Contain("pg_isready",
            "docker-compose.yml must include a pg_isready health check for the db service");
    }

    /// <summary>
    /// docker-compose.yml must always map port 5432:5432 for PostgreSQL access.
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Fact]
    public void DockerCompose_Should_Map_Postgres_Port_5432()
    {
        var content = ReadFile("docker-compose.yml");

        var hasPortMapping = Regex.IsMatch(content, @"""5432:5432""");
        hasPortMapping.Should().BeTrue(
            "docker-compose.yml must map port 5432:5432 for PostgreSQL access");
    }

    /// <summary>
    /// Property: docker-compose.yml always contains all four PostgreSQL preservation elements.
    /// Uses FsCheck to re-verify across multiple invocations that the file content is stable.
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Property(MaxTest = 10)]
    public bool DockerCompose_PostgresConfig_Always_Contains_Required_Elements(PositiveInt _seed)
    {
        var content = ReadFile("docker-compose.yml");

        var hasImage = content.Contains("postgres:17-alpine");
        var hasVolume = content.Contains("clinic_db_data");
        var hasHealthCheck = content.Contains("pg_isready");
        var hasPortMapping = Regex.IsMatch(content, @"""5432:5432""");

        return hasImage && hasVolume && hasHealthCheck && hasPortMapping;
    }

    // ─── Property: docker-compose.yml app service port mapping is preserved ───

    /// <summary>
    /// docker-compose.yml app service must map host port 8081 to container port 8080.
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Fact]
    public void DockerCompose_AppService_Should_Map_Port_8081_To_8080()
    {
        var content = ReadFile("docker-compose.yml");

        var hasAppPortMapping = Regex.IsMatch(content, @"""8081:8080""");
        hasAppPortMapping.Should().BeTrue(
            "docker-compose.yml app service must map port 8081:8080");
    }

    /// <summary>
    /// Property: docker-compose.yml app service port mapping 8081:8080 is always present.
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Property(MaxTest = 10)]
    public bool DockerCompose_AppPortMapping_Always_8081_To_8080(PositiveInt _seed)
    {
        var content = ReadFile("docker-compose.yml");
        return Regex.IsMatch(content, @"""8081:8080""");
    }

    // ─── Property: .env.example contains all required variable names ───

    /// <summary>
    /// .env.example must contain the POSTGRES_PASSWORD variable name.
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Fact]
    public void EnvExample_Should_Contain_PostgresPassword()
    {
        var content = ReadFile(".env.example");

        content.Should().Contain("POSTGRES_PASSWORD",
            ".env.example must define the POSTGRES_PASSWORD variable");
    }

    /// <summary>
    /// .env.example must contain the SEED_ADMIN_PASSWORD variable name.
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Fact]
    public void EnvExample_Should_Contain_SeedAdminPassword()
    {
        var content = ReadFile(".env.example");

        content.Should().Contain("SEED_ADMIN_PASSWORD",
            ".env.example must define the SEED_ADMIN_PASSWORD variable");
    }

    /// <summary>
    /// .env.example must contain the ASPNETCORE_ENVIRONMENT variable name.
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Fact]
    public void EnvExample_Should_Contain_AspNetCoreEnvironment()
    {
        var content = ReadFile(".env.example");

        content.Should().Contain("ASPNETCORE_ENVIRONMENT",
            ".env.example must define the ASPNETCORE_ENVIRONMENT variable");
    }

    /// <summary>
    /// Property: .env.example always contains all three required variable names.
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Property(MaxTest = 10)]
    public bool EnvExample_Always_Contains_All_Required_Variables(PositiveInt _seed)
    {
        var content = ReadFile(".env.example");

        var hasPostgresPassword = content.Contains("POSTGRES_PASSWORD");
        var hasSeedAdminPassword = content.Contains("SEED_ADMIN_PASSWORD");
        var hasAspNetCoreEnv = content.Contains("ASPNETCORE_ENVIRONMENT");

        return hasPostgresPassword && hasSeedAdminPassword && hasAspNetCoreEnv;
    }

    // ─── Property: deploy/start.sh (Docker fallback) is not modified ───

    /// <summary>
    /// deploy/start.sh must validate that .env exists (Docker fallback script preserved).
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Fact]
    public void StartSh_Should_Check_EnvFileExists()
    {
        var content = ReadFile(Path.Combine("deploy", "start.sh"));

        content.Should().Contain(".env",
            "deploy/start.sh must check for .env file existence");

        // Verify the specific check pattern
        var hasEnvCheck = content.Contains("! -f") && content.Contains(".env");
        hasEnvCheck.Should().BeTrue(
            "deploy/start.sh must use '! -f' to check .env file existence");
    }

    /// <summary>
    /// deploy/start.sh must reject .env files containing 'changeme' placeholders.
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Fact]
    public void StartSh_Should_Reject_Changeme_Placeholders()
    {
        var content = ReadFile(Path.Combine("deploy", "start.sh"));

        content.Should().Contain("changeme",
            "deploy/start.sh must check for and reject 'changeme' placeholder values");
    }

    /// <summary>
    /// deploy/start.sh must contain docker-compose commands for the Docker-based deployment path.
    /// **Validates: Requirements 3.1, 3.3**
    /// </summary>
    [Fact]
    public void StartSh_Should_Contain_DockerCompose_Commands()
    {
        var content = ReadFile(Path.Combine("deploy", "start.sh"));

        var hasDockerCompose = content.Contains("docker-compose") || content.Contains("docker compose");
        hasDockerCompose.Should().BeTrue(
            "deploy/start.sh must contain docker-compose commands for the Docker fallback path");

        // Must contain 'up' command to start services
        var hasUpCommand = Regex.IsMatch(content, @"(docker-compose|docker\s+compose).*up");
        hasUpCommand.Should().BeTrue(
            "deploy/start.sh must contain a docker-compose up command");
    }

    /// <summary>
    /// Property: deploy/start.sh always preserves .env validation and docker-compose commands.
    /// **Validates: Requirements 3.1, 3.3, 3.4**
    /// </summary>
    [Property(MaxTest = 10)]
    public bool StartSh_Always_Preserves_EnvChecks_And_DockerCompose(PositiveInt _seed)
    {
        var content = ReadFile(Path.Combine("deploy", "start.sh"));

        var hasEnvCheck = content.Contains("! -f") && content.Contains(".env");
        var hasChangemeCheck = content.Contains("changeme");
        var hasDockerCompose = content.Contains("docker-compose") || content.Contains("docker compose");
        var hasUpCommand = Regex.IsMatch(content, @"(docker-compose|docker\s+compose).*up");

        return hasEnvCheck && hasChangemeCheck && hasDockerCompose && hasUpCommand;
    }

    // ─── Property: Connection string in docker-compose.yml uses Host=db ───

    /// <summary>
    /// Property: For any generated password string, the connection string pattern in
    /// docker-compose.yml uses Host=db for the Docker path (unchanged).
    /// The Docker-based deployment connects to PostgreSQL via the Docker network hostname 'db',
    /// not 'localhost'. This must be preserved so the Docker fallback path continues to work.
    /// **Validates: Requirements 3.2**
    /// </summary>
    [Property(MaxTest = 50)]
    public bool DockerCompose_ConnectionString_Always_Uses_HostDb_ForAnyPassword(
        NonEmptyString password)
    {
        var content = ReadFile("docker-compose.yml");

        // The connection string in docker-compose.yml must use Host=db
        // (Docker network hostname for the PostgreSQL container)
        var hasHostDb = content.Contains("Host=db;");

        // It must NOT use Host=localhost in docker-compose.yml
        // (localhost is for the native deployment path, not Docker)
        var hasHostLocalhost = content.Contains("Host=localhost");

        return hasHostDb && !hasHostLocalhost;
    }
}
