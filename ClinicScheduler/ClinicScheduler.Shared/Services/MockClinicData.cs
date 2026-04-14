namespace ClinicScheduler.Shared.Services;

public static class MockClinicData
{
    /// <summary>Demo staff user shown in the staff dashboard title (e.g. "Jordan's Dashboard").</summary>
    public const string DemoStaffDisplayName = "Jordan";

    /// <summary>Short name for UI chrome (nav, landing).</summary>
    public const string ClinicShortName = "Riverside Pain Clinic";

    public sealed record Patient(
        string Id,
        string Name,
        DateTime Dob,
        string Phone,
        string Email,
        string Allergies,
        string Medications);
    public sealed record Doctor(string Id, string Name, string Specialty);
    public sealed record Appointment(
        string Id,
        DateTime Start,
        DateTime End,
        string Title,
        string Type,
        string PatientId,
        string DoctorId,
        string Location,
        string RoomNumber,
        string? MeetingNotes,
        string Status = "confirmed");
    public sealed record Note(string Id, DateTime Date, string PatientId, string DoctorId, string Author, string Text);

    private static DateTime WeekStartMonday(DateTime date)
    {
        var d = date.Date;
        var diff = ((int)d.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return d.AddDays(-diff);
    }

    private static readonly DateTime _baseWeekStart = WeekStartMonday(DateTime.Today);

    public static IReadOnlyList<Patient> Patients { get; } = new List<Patient>
    {
        new("p1", "Elena Vargas", new DateTime(1985, 2, 14), "555-1201", "evargas@example.com", "Codeine (nausea)", "Gabapentin 300 mg TID; acetaminophen PRN"),
        new("p2", "Marcus Webb", new DateTime(1990, 7, 3), "555-1202", "mwebb@example.com", "NSAIDs (GI)", "Cyclobenzaprine 10 mg QHS; lidocaine 5% patch"),
        new("p3", "Diane Okonkwo", new DateTime(1978, 11, 22), "555-1203", "dokonkwo@example.com", "Latex (mild)", "Duloxetine 60 mg daily"),
        new("p4", "Kevin Morales", new DateTime(1995, 5, 10), "555-1204", "kmorales@example.com", "None known", "Oxycodone 5 mg Q6H PRN; stool softener")
    };

    public static IReadOnlyList<Doctor> Doctors { get; } = new List<Doctor>
    {
        new("d1", "Dr. Morgan Chen", "Pain Medicine"),
        new("d2", "Dr. Priya Nair", "Physical Medicine & Rehabilitation")
    };

    public static IReadOnlyList<Appointment> Appointments { get; } = new List<Appointment>
    {
        // Base week
        new("a1",
            _baseWeekStart.AddDays(0).AddHours(9),
            _baseWeekStart.AddDays(0).AddHours(9.5),
            "Initial Pain Consultation",
            "medical",
            "p1",
            "d1",
            "Pain Clinic — Main",
            "201",
            "Bring imaging CD if outside facility. Pain diary for last 2 weeks."),

        new("a2",
            _baseWeekStart.AddDays(2).AddHours(14),
            _baseWeekStart.AddDays(2).AddHours(15),
            "Injection Planning Visit",
            "lab",
            "p1",
            "d1",
            "Interventional Suite",
            "IS-1",
            "Discuss fluoroscopy-guided epidural; review anticoagulation hold."),

        new("a3",
            _baseWeekStart.AddDays(4).AddHours(11),
            _baseWeekStart.AddDays(4).AddHours(12),
            "Follow-up",
            "medical",
            "p2",
            "d1",
            "Pain Clinic — Main",
            "204",
            "Check post-injection response; adjust PT plan and meds."),

        new("a4",
            _baseWeekStart.AddDays(1).AddHours(10),
            _baseWeekStart.AddDays(1).AddHours(10.5),
            "New Patient Intake",
            "intake",
            "p3",
            "d2",
            "Intake — West",
            "Desk 2",
            "PHQ-9, opioid risk screen, baseline function scores."),

        new("a5",
            _baseWeekStart.AddDays(3).AddHours(16),
            _baseWeekStart.AddDays(3).AddHours(16.5),
            "Opioid Stewardship Review",
            "medical",
            "p4",
            "d1",
            "Pain Clinic — Main",
            "310",
            "Reconcile PDMP; discuss taper goals and multimodal plan."),

        new("a6",
            _baseWeekStart.AddDays(5).AddHours(13),
            _baseWeekStart.AddDays(5).AddHours(14),
            "Prior Authorization Block",
            "admin",
            "p2",
            "d2",
            "Administration",
            "ADM-1",
            "Spinal cord stimulator PA; billing follow-up."),

        // Next week (so week navigation always has something)
        new("a7",
            _baseWeekStart.AddDays(7 + 0).AddHours(9.5),
            _baseWeekStart.AddDays(7 + 0).AddHours(10.5),
            "Fluoroscopy Suite Check-in",
            "medical",
            "p1",
            "d1",
            "Interventional Suite",
            "IS-2",
            "NPO if sedation; driver required. Arrive 30 min early."),

        new("a8",
            _baseWeekStart.AddDays(7 + 2).AddHours(15),
            _baseWeekStart.AddDays(7 + 2).AddHours(15.5),
            "PT Coordinated Follow-up",
            "lab",
            "p3",
            "d2",
            "Functional Restoration",
            "FR-3",
            "Review pacing, graded exposure; coordinate with pain psychology."),

        new("a9",
            _baseWeekStart.AddDays(7 + 4).AddHours(10),
            _baseWeekStart.AddDays(7 + 4).AddHours(11),
            "Imaging Review",
            "medical",
            "p4",
            "d1",
            "Pain Clinic — Main",
            "310",
            "Discuss MRI of lumbar spine; next steps for neuromodulation consult.")
    };

    public static IReadOnlyList<Note> Notes { get; } = new List<Note>
    {
        new("n1", _baseWeekStart.AddDays(0).AddHours(9.25), "p1", "d1", "Dr. Morgan Chen", "Chronic low back pain 6/10 today. Continue multimodal plan; discussed risks/benefits of injection."),
        new("n2", _baseWeekStart.AddDays(2).AddHours(14.25), "p1", "d1", "Dr. Morgan Chen", "Radicular symptoms improved after prior block. Patient agrees to trial epidural; hold NSAID 5 days pre-procedure."),
        new("n3", _baseWeekStart.AddDays(4).AddHours(11.25), "p2", "d1", "Dr. Morgan Chen", "Cervicalgia: adding home traction trial. Follow-up in 4 weeks or sooner if red flags."),
        new("n4", _baseWeekStart.AddDays(3).AddHours(16.15), "p4", "d1", "Dr. Morgan Chen", "Opioid agreement signed. PDMP reviewed. Constipation plan reinforced; behavioral health referral offered.")
    };

    /// <summary>Builds a title like "Jordan's Dashboard" from a display name.</summary>
    public static string DashboardTitleFor(string displayName)
    {
        var n = (displayName ?? "").Trim();
        if (string.IsNullOrEmpty(n))
        {
            return "Dashboard";
        }

        return n.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? $"{n}' Dashboard" : $"{n}'s Dashboard";
    }

    public static string FirstName(string fullName)
    {
        var t = (fullName ?? "").Trim();
        if (string.IsNullOrEmpty(t))
        {
            return "User";
        }

        var space = t.IndexOf(' ');
        return space < 0 ? t : t[..space];
    }
}
