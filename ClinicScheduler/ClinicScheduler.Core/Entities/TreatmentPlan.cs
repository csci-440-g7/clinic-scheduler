namespace ClinicScheduler.Core.Entities;

/// <summary>
/// A treatment plan assigned to a patient, specifying frequency and duration of therapy.
/// </summary>
public class TreatmentPlan
{
    public int Id { get; set; }
    
    public int PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    
    public int TherapistId { get; private set; }
    public Therapist Therapist { get; private set; } = null!;
    
    /// <summary>
    /// How many days per week: 2, 3, or 4.
    /// </summary>
    public int FrequencyPerWeek { get; private set; }
    
    /// <summary>
    /// Total treatment duration in days: 20, 30, or 50.
    /// </summary>
    public int TotalDays { get; private set; }
    
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Many-to-many: a treatment plan can include multiple therapy types
    public ICollection<TreatmentPlanTherapy> TreatmentPlanTherapies { get; set; } = [];

    private static readonly int[] ValidFrequencies = [2, 3, 4];
    private static readonly int[] ValidDurations = [20, 30, 50];

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private TreatmentPlan() { }

    public TreatmentPlan(Patient patient, Therapist therapist, int frequencyPerWeek, int totalDays, DateOnly startDate)
    {
        if (!ValidFrequencies.Contains(frequencyPerWeek))
            throw new ArgumentException($"Frequency must be one of the following: {string.Join(", ", ValidFrequencies)}.", nameof(frequencyPerWeek));

        if (!ValidDurations.Contains(totalDays))
            throw new ArgumentException($"Total days must be one of the following: {string.Join(", ", ValidDurations)}.", nameof(totalDays));

        Patient = patient;
        PatientId = patient.Id;
        Therapist = therapist;
        TherapistId = therapist.Id;
        FrequencyPerWeek = frequencyPerWeek;
        TotalDays = totalDays;
        StartDate = startDate;
        
        EndDate = CalculateEndDate(startDate, totalDays, frequencyPerWeek);
    }

    private static DateOnly CalculateEndDate(DateOnly startDate, int totalDays, int frequencyPerWeek)
    {
        double numberOfWeeks = (double)totalDays / frequencyPerWeek;
        int totalCalendarDays = (int)Math.Ceiling(numberOfWeeks) * 7;
        return startDate.AddDays(totalCalendarDays);
    }

    public void UpdateSchedule(int frequencyPerWeek, int totalDays, DateOnly startDate)
    {
        if (!ValidFrequencies.Contains(frequencyPerWeek))
            throw new ArgumentException($"Frequency must be one of the following: {string.Join(", ", ValidFrequencies)}.", nameof(frequencyPerWeek));

        if (!ValidDurations.Contains(totalDays))
            throw new ArgumentException($"Total days must be one of the following: {string.Join(", ", ValidDurations)}.", nameof(totalDays));

        FrequencyPerWeek = frequencyPerWeek;
        TotalDays = totalDays;
        StartDate = startDate;
        EndDate = CalculateEndDate(startDate, totalDays, frequencyPerWeek);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeTherapist(Therapist newTherapist)
    {
        ArgumentNullException.ThrowIfNull(newTherapist);
        Therapist = newTherapist;
        TherapistId = newTherapist.Id;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddTherapy(TherapyType therapyType)
    {
        ArgumentNullException.ThrowIfNull(therapyType);

        if (TreatmentPlanTherapies.Any(tpt => tpt.TherapyTypeId == therapyType.Id))
        {
            return;
        }
        TreatmentPlanTherapies.Add(new TreatmentPlanTherapy(this, therapyType));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveTherapy(TherapyType therapyType)
    {
        var therapyToRemove = TreatmentPlanTherapies.FirstOrDefault(tpt => tpt.TherapyTypeId == therapyType.Id);
        if (therapyToRemove is not null)
        {
            TreatmentPlanTherapies.Remove(therapyToRemove);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Extends the treatment plan end date by 7 calendar days to account for a missed session.
    /// </summary>
    public void ExtendForMissedSession()
    {
        EndDate = EndDate.AddDays(7);
        UpdatedAt = DateTime.UtcNow;
    }
}