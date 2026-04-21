namespace ClinicScheduler.Core.Entities;
using System.Text.RegularExpressions;

public partial class Therapist
{
    public int Id { get; set; }
    
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Specialty { get; set; }
    public string? NpiNumber { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TreatmentPlan> TreatmentPlans { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private Therapist()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
    }

    public Therapist(string firstName, string lastName, string email, string? phone = null, string? specialty = null, string? npiNumber = null)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        Specialty = specialty;
        SetNpiNumber(npiNumber);
    }

    public void UpdateContactInfo(string email, string? phone)
    {
        Email = email;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string firstName, string lastName, string? specialty)
    {
        FirstName = firstName;
        LastName = lastName;
        Specialty = specialty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetNpiNumber(string? npiNumber)
    {
        if (npiNumber is not null && !NpiNumberRegex().IsMatch(npiNumber))
            throw new ArgumentException("NPI number must be exactly 10 digits.", nameof(npiNumber));
        NpiNumber = npiNumber;
        UpdatedAt = DateTime.UtcNow;
    }

    [GeneratedRegex(@"^\d{10}$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex NpiNumberRegex();
}