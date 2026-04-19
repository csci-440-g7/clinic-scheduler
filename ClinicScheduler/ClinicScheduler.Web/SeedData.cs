using ClinicScheduler.Core.Entities;
using ClinicScheduler.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace ClinicScheduler.Web;

/// <summary>
/// Seeds the database with sample data on first startup.
/// Only runs when all tables are empty — safe to leave in production.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>Seeds roles, demo users, and sample clinic data if the database is empty.</summary>
    public static async Task SeedAsync(ClinicDbContext db, UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager, string adminPassword)
    {
        // Seed roles
        foreach (var role in new[] { "Admin", "ClinicManager", "Therapist", "Staff", "Patient" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // Seed demo accounts
        await EnsureUser(userManager, "admin@clinic.com",          "Administrator",   adminPassword,    "Admin");
        await EnsureUser(userManager, "manager@clinic.com",        "Clinic Manager",  "Manager@1234",   "ClinicManager");
        await EnsureUser(userManager, "therapist@clinic.com",      "Demo Therapist",  "Therapist@1234", "Therapist");
        await EnsureUser(userManager, "staff@clinic.com",          "Staff Member",    "Staff@Clinic1",  "Staff");
        await EnsureUser(userManager, "patient@clinic.com",        "Demo Patient",    "Patient@1234",   "Patient");

        static async Task EnsureUser(UserManager<AppUser> um, string email, string displayName, string password, string role)
        {
            var user = await um.FindByEmailAsync(email);
            if (user is null)
            {
                user = new AppUser { UserName = email, Email = email, DisplayName = displayName, EmailConfirmed = true };
                await um.CreateAsync(user, password);
            }
            if (!await um.IsInRoleAsync(user, role))
                await um.AddToRoleAsync(user, role);
        }

        // Only seed clinic data if the database is completely empty
        if (db.Patients.Any()) return;

        // ── Locations ────────────────────────────────────────────────────────
        var downtown = new Location("Downtown Medical Center", "123 Main Street");
        downtown.UpdateDetails("Downtown Medical Center", "123 Main Street", "Dallas", "TX", "75201", "America/Chicago");

        var northside = new Location("Northside Clinic", "456 Oak Avenue");
        northside.UpdateDetails("Northside Clinic", "456 Oak Avenue", "Dallas", "TX", "75234", "America/Chicago");

        db.Locations.AddRange(downtown, northside);
        await db.SaveChangesAsync();

        // ── Rooms ────────────────────────────────────────────────────────────
        var roomA = new Room("Exam Room A", 3, downtown);
        roomA.UpdateDetails("Exam Room A", 3, "General therapy room");

        var roomB = new Room("Exam Room B", 3, downtown);
        roomB.UpdateDetails("Exam Room B", 3, "Physical therapy room with mat table");

        var roomC = new Room("Consultation Room", 4, downtown);
        roomC.UpdateDetails("Consultation Room", 4, "Private consultation and evaluation");

        var roomD = new Room("Therapy Suite 1", 2, northside);
        roomD.UpdateDetails("Therapy Suite 1", 2, "Quiet room for behavioral therapy");

        db.Rooms.AddRange(roomA, roomB, roomC, roomD);
        await db.SaveChangesAsync();

        // ── Therapy Types ────────────────────────────────────────────────────
        var ptType = new TherapyType(
            "Physical Therapy",
            "Rehabilitation of musculoskeletal injuries and post-surgical recovery.",
            "Orthopedics",
            "#4A90D9");

        var cbtType = new TherapyType(
            "Cognitive Behavioral Therapy",
            "Short-term, goal-oriented psychotherapy for anxiety and depression.",
            "Mental Health",
            "#7B68EE");

        var otType = new TherapyType(
            "Occupational Therapy",
            "Helps patients develop, recover, and improve everyday living skills.",
            "Rehabilitation",
            "#50C878");

        var stType = new TherapyType(
            "Speech Therapy",
            "Treatment for communication disorders and swallowing difficulties.",
            "Communication",
            "#FF8C00");

        db.TherapyTypes.AddRange(ptType, cbtType, otType, stType);
        await db.SaveChangesAsync();

        // ── Therapists ───────────────────────────────────────────────────────
        var sarah = new Therapist("Sarah", "Mitchell", "sarah.mitchell@clinic.com", "555-201-1001", "Orthopedics");
        var james = new Therapist("James", "Okafor", "james.okafor@clinic.com", "555-201-1002", "Mental Health");
        var linda = new Therapist("Linda", "Nguyen", "linda.nguyen@clinic.com", "555-201-1003", "Rehabilitation");

        db.Therapists.AddRange(sarah, james, linda);
        await db.SaveChangesAsync();

        // ── Patients ─────────────────────────────────────────────────────────
        var demoPatient = new Patient("Demo", "Patient", "patient@clinic.com", new DateOnly(1990, 1, 1), "555-301-2000");
        var alice = new Patient("Alice", "Johnson", "alice.johnson@email.com", new DateOnly(1985, 3, 14), "555-301-2001");
        var bob   = new Patient("Bob", "Martinez", "bob.martinez@email.com", new DateOnly(1972, 7, 22), "555-301-2002");
        var carol = new Patient("Carol", "Thompson", "carol.thompson@email.com", new DateOnly(1990, 11, 5), "555-301-2003");
        var david = new Patient("David", "Lee", "david.lee@email.com", new DateOnly(2001, 1, 30), "555-301-2004");
        var emma  = new Patient("Emma", "Wilson", "emma.wilson@email.com", new DateOnly(1967, 9, 18), "555-301-2005");

        db.Patients.AddRange(demoPatient, alice, bob, carol, david, emma);
        await db.SaveChangesAsync();

        // ── Treatment Plans ──────────────────────────────────────────────────
        var alicePlan = new TreatmentPlan(alice, sarah, 3, 30, new DateOnly(2026, 4, 1));
        alicePlan.AddTherapy(ptType);
        alicePlan.AddTherapy(otType);

        var bobPlan = new TreatmentPlan(bob, james, 2, 20, new DateOnly(2026, 4, 7));
        bobPlan.AddTherapy(cbtType);

        var carolPlan = new TreatmentPlan(carol, linda, 4, 50, new DateOnly(2026, 3, 17));
        carolPlan.AddTherapy(otType);
        carolPlan.AddTherapy(stType);

        db.TreatmentPlans.AddRange(alicePlan, bobPlan, carolPlan);
        await db.SaveChangesAsync();

        // ── Appointments ─────────────────────────────────────────────────────
        var today = DateTime.UtcNow.Date;
        var hour = TimeSpan.FromHours(1);

        var a1 = new Appointment(alice, sarah, roomA, today.AddDays(-1).AddHours(9), hour)
            { TreatmentPlanId = alicePlan.Id, Notes = "Good progress on knee flexion." };
        a1.Complete();

        var a2 = new Appointment(alice, sarah, roomA, today.AddHours(9), hour)
            { TreatmentPlanId = alicePlan.Id };

        var a3 = new Appointment(alice, sarah, roomA, today.AddDays(2).AddHours(9), hour)
            { TreatmentPlanId = alicePlan.Id };

        var a4 = new Appointment(bob, james, roomD, today.AddDays(-2).AddHours(14), hour)
            { TreatmentPlanId = bobPlan.Id, Notes = "Discussed coping strategies." };
        a4.Complete();

        var a5 = new Appointment(bob, james, roomD, today.AddDays(1).AddHours(14), hour)
            { TreatmentPlanId = bobPlan.Id };

        var a6 = new Appointment(carol, linda, roomB, today.AddDays(-1).AddHours(11), hour)
            { TreatmentPlanId = carolPlan.Id };
        a6.MarkAsMissed();

        var a7 = new Appointment(carol, linda, roomB, today.AddHours(11), hour)
            { TreatmentPlanId = carolPlan.Id };

        var a8 = new Appointment(carol, linda, roomB, today.AddDays(3).AddHours(11), hour)
            { TreatmentPlanId = carolPlan.Id };

        var a9 = new Appointment(david, sarah, roomC, today.AddDays(1).AddHours(10), hour)
            { Notes = "Initial evaluation." };

        var a10 = new Appointment(emma, linda, roomA, today.AddDays(2).AddHours(13), hour);

        db.Appointments.AddRange(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10);
        await db.SaveChangesAsync();
    }
}
