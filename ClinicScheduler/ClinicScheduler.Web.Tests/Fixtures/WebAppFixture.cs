using ClinicScheduler.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Testcontainers.PostgreSql;

namespace ClinicScheduler.Web.Tests.Fixtures;

/// <summary>
/// Shared fixture that starts a real PostgreSQL container once per test collection,
/// then boots the full ASP.NET Core app via WebApplicationFactory.
/// </summary>
[CollectionDefinition("WebApp")]
public class WebAppCollection : ICollectionFixture<WebAppFixture> { }

public class WebAppFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("clinic_test")
        .WithUsername("postgres")
        .WithPassword("testpassword")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    // Replace the real DbContext registration with one that points
                    // at the Testcontainers PostgreSQL instance.
                    var descriptors = services
                        .Where(d => d.ServiceType == typeof(DbContextOptions<ClinicDbContext>))
                        .ToList();
                    foreach (var d in descriptors)
                        services.Remove(d);

                    services.AddDbContext<ClinicDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));
                });

                // ConfigureTestServices runs AFTER the app's Program.cs services,
                // so it can override the Identity authentication defaults.
                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication("TestScheme")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });

                    // Force all default schemes to TestScheme, overriding Identity's cookie defaults
                    services.PostConfigure<AuthenticationOptions>(options =>
                    {
                        options.DefaultAuthenticateScheme = "TestScheme";
                        options.DefaultChallengeScheme = "TestScheme";
                        options.DefaultScheme = "TestScheme";
                    });
                });
            });

        // Ensure the database schema is created.
        // Program.cs calls db.Database.Migrate() which silently fails in Testing
        // when no migration files exist. EnsureCreated() creates the schema from the model.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        Client = Factory.CreateClient();
    }

    /// <summary>
    /// Truncates all data tables so each test class starts with a clean database.
    /// Truncation order respects FK constraints (children before parents).
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        await db.Database.ExecuteSqlRawAsync(@"
            TRUNCATE TABLE
                ""ScheduleConflicts"",
                ""Appointments"",
                ""TreatmentPlanTherapies"",
                ""TreatmentPlans"",
                ""TimeSlots"",
                ""Rooms"",
                ""Locations"",
                ""TherapyTypes"",
                ""Therapists"",
                ""Patients""
            RESTART IDENTITY CASCADE;
        ");
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
