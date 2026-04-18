using Microsoft.AspNetCore.Identity;

namespace ClinicScheduler.Infrastructure.Data;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
