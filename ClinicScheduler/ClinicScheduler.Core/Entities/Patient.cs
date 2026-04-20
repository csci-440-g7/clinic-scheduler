namespace ClinicScheduler.Core.Entities;

/// <summary>
/// Represents a patient registered in the clinic system.
/// </summary>
public class Patient
{
    public int Id { get; set; }
    
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string? Phone { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string? Notes { get; private set; }

    public string FullName => $"{FirstName} {LastName}";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TreatmentPlan> TreatmentPlans { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private Patient()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
    }

    public Patient(string firstName, string lastName, string email, DateOnly dateOfBirth, string? phone = null)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        DateOfBirth = dateOfBirth;
        Phone = phone;
    }

    public void UpdateContactInfo(string email, string? phone)
    {
        Email = email;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string firstName, string lastName, DateOnly dateOfBirth)
    {
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}