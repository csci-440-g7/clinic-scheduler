using System.Text;
using ClinicScheduler.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ClinicScheduler.Infrastructure.Data;

/// <summary>
/// The EF Core DbContext - the "bridge" between your C# objects and the database.
/// Each DbSet becomes a table. EF Core tracks changes to objects and generates SQL.
/// </summary>
public class ClinicDbContext : IdentityDbContext<AppUser>
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
    public DbSet<AppointmentRequest> AppointmentRequests => Set<AppointmentRequest>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CancelAppointmentRequest> CancelAppointmentRequests => Set<CancelAppointmentRequest>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<ScheduleConflict> ScheduleConflicts => Set<ScheduleConflict>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TreatmentPlanTherapy>()
            .HasKey(tpt => new { tpt.TreatmentPlanId, tpt.TherapyTypeId });

        modelBuilder.Entity<TreatmentPlan>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_TreatmentPlan_Frequency",
                "\"FrequencyPerWeek\" IN (2, 3, 4)"));

        modelBuilder.Entity<TreatmentPlan>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_TreatmentPlan_TotalDays",
                "\"TotalDays\" IN (20, 30, 50)"));

        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.Email)
            .IsUnique();

        modelBuilder.Entity<Therapist>()
            .HasIndex(t => t.Email)
            .IsUnique();

        modelBuilder.Entity<Therapist>()
            .HasIndex(t => t.NpiNumber)
            .IsUnique()
            .HasFilter("\"NpiNumber\" IS NOT NULL");

        modelBuilder.Entity<Location>()
            .HasMany(l => l.TimeSlots)
            .WithOne(ts => ts.Location)
            .HasForeignKey(ts => ts.LocationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Appointment>()
            .HasMany(a => a.ScheduleConflicts)
            .WithOne(sc => sc.Appointment)
            .HasForeignKey(sc => sc.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
    
    /// <summary>
    /// Automatically update the UpdatedAt timestamp and create audit log entries on save.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries()
                     .Where(e => e.State is EntityState.Modified))
        {
            if (entry.Entity.GetType().GetProperty("UpdatedAt") is { } prop)
            {
                prop.SetValue(entry.Entity, DateTime.UtcNow);
            }
        }

        try
        {
            CreateAuditLogEntries();
        }
        catch
        {
            // Audit logging is best-effort; failures must not block the primary save.
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private static readonly HashSet<string> ExcludedTypeNames =
    [
        nameof(AuditLog),
        nameof(Notification),
        nameof(IdentityRole),
        nameof(IdentityUserRole<string>),
        nameof(IdentityUserClaim<string>),
        nameof(IdentityUserLogin<string>),
        nameof(IdentityUserToken<string>),
        nameof(IdentityRoleClaim<string>),
    ];

    private static bool IsExcludedFromAudit(EntityEntry entry)
    {
        var type = entry.Entity.GetType();
        return ExcludedTypeNames.Contains(type.Name)
               || typeof(IdentityUser).IsAssignableFrom(type);
    }

    private void CreateAuditLogEntries()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => !IsExcludedFromAudit(e))
            .ToList();

        foreach (var entry in entries)
        {
            var entityName = entry.Entity.GetType().Name;
            var entityId = GetEntityId(entry);
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Modified => AuditAction.Modified,
                EntityState.Deleted => AuditAction.Deleted,
                _ => throw new InvalidOperationException()
            };
            var changeSummary = BuildChangeSummary(entry);

            var auditLog = new AuditLog(entityName, entityId, action, changeSummary);
            AuditLogs.Add(auditLog);
        }
    }

    private static string GetEntityId(EntityEntry entry)
    {
        var idProperty = entry.Entity.GetType().GetProperty("Id");
        if (idProperty is not null)
        {
            var value = idProperty.GetValue(entry.Entity);
            return value?.ToString() ?? "0";
        }

        return "unknown";
    }

    private static string? BuildChangeSummary(EntityEntry entry)
    {
        return entry.State switch
        {
            EntityState.Modified => BuildModifiedSummary(entry),
            EntityState.Added => BuildAddedSummary(entry),
            EntityState.Deleted => BuildDeletedSummary(entry),
            _ => null
        };
    }

    private static string? BuildModifiedSummary(EntityEntry entry)
    {
        var sb = new StringBuilder();
        foreach (var prop in entry.Properties.Where(p => p.IsModified))
        {
            if (sb.Length > 0) sb.Append("; ");
            sb.Append($"{prop.Metadata.Name}: {prop.OriginalValue} → {prop.CurrentValue}");
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    private static string? BuildAddedSummary(EntityEntry entry)
    {
        var sb = new StringBuilder();
        foreach (var prop in entry.Properties)
        {
            if (prop.CurrentValue is null) continue;
            if (sb.Length > 0) sb.Append("; ");
            sb.Append($"{prop.Metadata.Name}: {prop.CurrentValue}");
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    private static string? BuildDeletedSummary(EntityEntry entry)
    {
        var sb = new StringBuilder();
        foreach (var prop in entry.Properties)
        {
            if (prop.OriginalValue is null) continue;
            if (sb.Length > 0) sb.Append("; ");
            sb.Append($"{prop.Metadata.Name}: {prop.OriginalValue}");
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }
}