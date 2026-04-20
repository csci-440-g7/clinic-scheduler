using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ClinicScheduler.Infrastructure.Data;

/// <summary>
/// Design-time factory for creating ClinicDbContext instances during EF Core migrations.
/// </summary>
public class ClinicDbContextFactory : IDesignTimeDbContextFactory<ClinicDbContext>
{
    public ClinicDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "ClinicScheduler.Web"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ClinicDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

        return new ClinicDbContext(optionsBuilder.Options);
    }
}
