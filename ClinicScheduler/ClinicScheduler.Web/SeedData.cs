using ClinicScheduler.Core.Entities;
using ClinicScheduler.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace ClinicScheduler.Web;

/// <summary>
/// Seeds the database with sample data on first startup.
/// Only runs when all tables are empty — safe to leave in production.
/// </summary>
public static class SeedData
{
    public static async Task SeedAsync(ClinicDbContext db, UserManager<AppUser> userManager)
    {
        // Seed default admin account
        if (await userManager.FindByEmailAsync("admin@clinic.com") is null)
        {
            var admin = new AppUser
            {
                UserName = "admin@clinic.com",
                Email = "admin@clinic.com",
                DisplayName = "Administrator",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "Admin123!");
        }

        // Only seed clinic data if the database is completely empty
        if (db.Patients.Any()) return;

        // ── Locations ────────────────────────────────────────────────────────
        var downtown = new Location
        {
            Name = "Downtown Medical Center",
            Address = "123 Main Street",
            City = "Dallas",
            State = "TX",
            ZipCode = "75201",
            TimeZone = "America/Chicago"
        };
        var northside = new Location
        {
            Name = "Northside Clinic",
            Address = "456 Oak Avenue",
            City = "Dallas",
            State = "TX",
            ZipCode = "75234",
            TimeZone = "America/Chicago"
        };
        db.Locations.AddRange(downtown, northside);
        await db.SaveChangesAsync();

        // ── Rooms ────────────────────────────────────────────────────────────
        var roomA = new Room { Name = "Exam Room A", Capacity = 3, Description = "General therapy room", LocationId = downtown.Id };
        var roomB = new Room { Name = "Exam Room B", Capacity = 3, Description = "Physical therapy room with mat table", LocationId = downtown.Id };
        var roomC = new Room { Name = "Consultation Room", Capacity = 4, Description = "Private consultation and evaluation", LocationId = downtown.Id };
        var roomD = new Room { Name = "Therapy Suite 1", Capacity = 2, Description = "Quiet room for behavioral therapy", LocationId = northside.Id };
        db.Rooms.AddRange(roomA, roomB, roomC, roomD);
        await db.SaveChangesAsync();

        // ── Therapy Types ────────────────────────────────────────────────────
        var ptType = new TherapyType
        {
            Name = "Physical Therapy",
            Specialty = "Orthopedics",
            Description = "Rehabilitation of musculoskeletal injuries and post-surgical recovery.",
            ColorCode = "#4A90D9"
        };
        var cbtType = new TherapyType
        {
            Name = "Cognitive Behavioral Therapy",
            Specialty = "Mental Health",
            Description = "Short-term, goal-oriented psychotherapy for anxiety and depression.",
            ColorCode = "#7B68EE"
        };
        var otType = new TherapyType
        {
            Name = "Occupational Therapy",
            Specialty = "Rehabilitation",
            Description = "Helps patients develop, recover, and improve everyday living skills.",
            ColorCode = "#50C878"
        };
        var stType = new TherapyType
        {
            Name = "Speech Therapy",
            Specialty = "Communication",
            Description = "Treatment for communication disorders and swallowing difficulties.",
            ColorCode = "#FF8C00"
        };
        db.TherapyTypes.AddRange(ptType, cbtType, otType, stType);
        await db.SaveChangesAsync();

        // ── Therapists ───────────────────────────────────────────────────────
        var sarah = new Therapist
        {
            FirstName = "Sarah",
            LastName = "Mitchell",
            Email = "sarah.mitchell@clinic.com",
            Phone = "555-201-1001",
            Specialty = "Orthopedics"
        };
        var james = new Therapist
        {
            FirstName = "James",
            LastName = "Okafor",
            Email = "james.okafor@clinic.com",
            Phone = "555-201-1002",
            Specialty = "Mental Health"
        };
        var linda = new Therapist
        {
            FirstName = "Linda",
            LastName = "Nguyen",
            Email = "linda.nguyen@clinic.com",
            Phone = "555-201-1003",
            Specialty = "Rehabilitation"
        };
        db.Therapists.AddRange(sarah, james, linda);
        await db.SaveChangesAsync();

        // ── Patients ─────────────────────────────────────────────────────────
        var alice = new Patient
        {
            FirstName = "Alice",
            LastName = "Johnson",
            Email = "alice.johnson@email.com",
            Phone = "555-301-2001",
            DateOfBirth = new DateOnly(1985, 3, 14)
        };
        var bob = new Patient
        {
            FirstName = "Bob",
            LastName = "Martinez",
            Email = "bob.martinez@email.com",
            Phone = "555-301-2002",
            DateOfBirth = new DateOnly(1972, 7, 22)
        };
        var carol = new Patient
        {
            FirstName = "Carol",
            LastName = "Thompson",
            Email = "carol.thompson@email.com",
            Phone = "555-301-2003",
            DateOfBirth = new DateOnly(1990, 11, 5)
        };
        var david = new Patient
        {
            FirstName = "David",
            LastName = "Lee",
            Email = "david.lee@email.com",
            Phone = "555-301-2004",
            DateOfBirth = new DateOnly(2001, 1, 30)
        };
        var emma = new Patient
        {
            FirstName = "Emma",
            LastName = "Wilson",
            Email = "emma.wilson@email.com",
            Phone = "555-301-2005",
            DateOfBirth = new DateOnly(1967, 9, 18)
        };
        db.Patients.AddRange(alice, bob, carol, david, emma);
        await db.SaveChangesAsync();

        // ── Treatment Plans ──────────────────────────────────────────────────
        var alicePlan = new TreatmentPlan
        {
            PatientId = alice.Id,
            TherapistId = sarah.Id,
            FrequencyPerWeek = 3,
            TotalDays = 30,
            StartDate = new DateOnly(2026, 4, 1),
            TreatmentPlanTherapies =
            [
                new TreatmentPlanTherapy { TherapyTypeId = ptType.Id },
                new TreatmentPlanTherapy { TherapyTypeId = otType.Id }
            ]
        };
        var bobPlan = new TreatmentPlan
        {
            PatientId = bob.Id,
            TherapistId = james.Id,
            FrequencyPerWeek = 2,
            TotalDays = 20,
            StartDate = new DateOnly(2026, 4, 7),
            TreatmentPlanTherapies =
            [
                new TreatmentPlanTherapy { TherapyTypeId = cbtType.Id }
            ]
        };
        var carolPlan = new TreatmentPlan
        {
            PatientId = carol.Id,
            TherapistId = linda.Id,
            FrequencyPerWeek = 4,
            TotalDays = 50,
            StartDate = new DateOnly(2026, 3, 17),
            TreatmentPlanTherapies =
            [
                new TreatmentPlanTherapy { TherapyTypeId = otType.Id },
                new TreatmentPlanTherapy { TherapyTypeId = stType.Id }
            ]
        };
        db.TreatmentPlans.AddRange(alicePlan, bobPlan, carolPlan);
        await db.SaveChangesAsync();

        // ── Appointments ─────────────────────────────────────────────────────
        var today = DateTime.UtcNow.Date;
        var appointments = new List<Appointment>
        {
            // Alice – PT with Sarah this week
            new() { PatientId = alice.Id, TherapistId = sarah.Id, RoomId = roomA.Id, TreatmentPlanId = alicePlan.Id,
                    StartTime = today.AddDays(-1).AddHours(9),  EndTime = today.AddDays(-1).AddHours(10),
                    Status = AppointmentStatus.Completed, Notes = "Good progress on knee flexion." },
            new() { PatientId = alice.Id, TherapistId = sarah.Id, RoomId = roomA.Id, TreatmentPlanId = alicePlan.Id,
                    StartTime = today.AddHours(9),              EndTime = today.AddHours(10),
                    Status = AppointmentStatus.Scheduled },
            new() { PatientId = alice.Id, TherapistId = sarah.Id, RoomId = roomA.Id, TreatmentPlanId = alicePlan.Id,
                    StartTime = today.AddDays(2).AddHours(9),   EndTime = today.AddDays(2).AddHours(10),
                    Status = AppointmentStatus.Scheduled },

            // Bob – CBT with James
            new() { PatientId = bob.Id, TherapistId = james.Id, RoomId = roomD.Id, TreatmentPlanId = bobPlan.Id,
                    StartTime = today.AddDays(-2).AddHours(14), EndTime = today.AddDays(-2).AddHours(15),
                    Status = AppointmentStatus.Completed, Notes = "Discussed coping strategies." },
            new() { PatientId = bob.Id, TherapistId = james.Id, RoomId = roomD.Id, TreatmentPlanId = bobPlan.Id,
                    StartTime = today.AddDays(1).AddHours(14),  EndTime = today.AddDays(1).AddHours(15),
                    Status = AppointmentStatus.Scheduled },

            // Carol – OT with Linda
            new() { PatientId = carol.Id, TherapistId = linda.Id, RoomId = roomB.Id, TreatmentPlanId = carolPlan.Id,
                    StartTime = today.AddDays(-1).AddHours(11), EndTime = today.AddDays(-1).AddHours(12),
                    Status = AppointmentStatus.Missed },
            new() { PatientId = carol.Id, TherapistId = linda.Id, RoomId = roomB.Id, TreatmentPlanId = carolPlan.Id,
                    StartTime = today.AddHours(11),             EndTime = today.AddHours(12),
                    Status = AppointmentStatus.Scheduled },
            new() { PatientId = carol.Id, TherapistId = linda.Id, RoomId = roomB.Id, TreatmentPlanId = carolPlan.Id,
                    StartTime = today.AddDays(3).AddHours(11),  EndTime = today.AddDays(3).AddHours(12),
                    Status = AppointmentStatus.Scheduled },

            // David – one-off consult
            new() { PatientId = david.Id, TherapistId = sarah.Id, RoomId = roomC.Id,
                    StartTime = today.AddDays(1).AddHours(10),  EndTime = today.AddDays(1).AddHours(11),
                    Status = AppointmentStatus.Scheduled, Notes = "Initial evaluation." },

            // Emma – upcoming PT
            new() { PatientId = emma.Id, TherapistId = linda.Id, RoomId = roomA.Id,
                    StartTime = today.AddDays(2).AddHours(13),  EndTime = today.AddDays(2).AddHours(14),
                    Status = AppointmentStatus.Scheduled },
        };
        db.Appointments.AddRange(appointments);
        await db.SaveChangesAsync();
    }
}
