namespace ClinicScheduler.Core.Entities;
using System.Text.RegularExpressions;

/// <summary>
/// Represents an entry in the "Therapy Description Bank" — a standardized therapy session type.
/// </summary>
public partial class TherapyType
{
    public int Id { get; set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? Specialty { get; private set; }
    
    /// <summary>
    /// Hex color code for dashboard color-coding (e.g., "#4CAF50").
    /// </summary>
    public string? ColorCode { get; private set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TreatmentPlanTherapy> TreatmentPlanTherapies { get; set; } = [];

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private TherapyType()
    {
        Name = string.Empty;
    }

    public TherapyType(string name, string? description, string? specialty, string? colorCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Description = description;
        Specialty = specialty;
        SetColorCode(colorCode);
    }

    public void UpdateDetails(string name, string? description, string? specialty, string? colorCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Description = description;
        Specialty = specialty;
        SetColorCode(colorCode);
        UpdatedAt = DateTime.UtcNow;
    }

    private void SetColorCode(string? colorCode)
    {
        if (colorCode is not null && !IsValidHexColor(colorCode))
        {
            throw new ArgumentException("Color code must be a valid 3 or 6-digit hex string (e.g., '#RRGGBB').", nameof(colorCode));
        }
        ColorCode = colorCode;
    }

    private static bool IsValidHexColor(string color)
    {
        // Matches #RRGGBB or #RGB
        return HexColorRegex().IsMatch(color);
    }

    [GeneratedRegex(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex HexColorRegex();
}