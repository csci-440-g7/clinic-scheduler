namespace ClinicScheduler.Core.Entities;

public class Therapist
{
    public int Id { get; set; }
    
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Specialty { get; set; }

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

    public Therapist(string firstName, string lastName, string email, string? phone = null, string? specialty = null)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        Specialty = specialty;
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
}