using ClinicScheduler.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicScheduler.Infrastructure.Data;

/// <summary>
/// The EF Core DbContext - the "bridge" between your C# objects and the database.
/// Each DbSet becomes a table. EF Core tracks changes to objects and generates SQL.
/// </summary>
public class ClinicDbContext : DbContext
{
    public ClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Therapist> Therapists => Set<Therapist>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<TherapyType> TherapyTypes => Set<TherapyType>();
    public DbSet<TreatmentPlan> TreatmentPlans => Set<TreatmentPlan>();
    public DbSet<TreatmentPlanTherapy> TreatmentPlanTherapies => Set<TreatmentPlanTherapy>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure the composite primary key for the many-to-many join table
        modelBuilder.Entity<TreatmentPlanTherapy>()
            .HasKey(tpt => new { tpt.TreatmentPlanId, tpt.TherapyTypeId });
        
        // Treatment plan validation: frequency must be 2, 3, or 4
        modelBuilder.Entity<TreatmentPlan>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_TreatmentPlan_Frequency",
                "\"FrequencyPerWeek\" IN  (2, 3, 4)"));
        
        // Treatment plan validation: total days must be 20, 30, or 50
        modelBuilder.Entity<TreatmentPlan>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_TreatmentPlan_TotalDays",
                "\"TotalDays\" IN (20, 30, 50)"));

        // Ensure email uniqueness
        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.Email)
            .IsUnique();

        modelBuilder.Entity<Therapist>()
            .HasIndex(t => t.Email)
            .IsUnique();
    }
    
    /// <summary>
    /// Automatically update the UpdatedAt timestamp on save.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries()
                     .Where(e => e.State is EntityState.Modified))
        {
            if (entry.Entity.GetType().GetProperty("UpdatedAt") is { } prop)
            {
                prop.SetValue(entry.Entity, DateTime.UtcNow);
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}