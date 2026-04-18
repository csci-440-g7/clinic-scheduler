using ClinicScheduler.Shared.Services;

namespace ClinicScheduler.Web.Services;

/// <summary>Server-side implementation of <see cref="IFormFactor"/> for the ASP.NET Core host.</summary>
public class FormFactor : IFormFactor
{
    /// <inheritdoc/>
    public string GetFormFactor() => "Web";

    /// <inheritdoc/>
    public string GetPlatform() => Environment.OSVersion.ToString();
}