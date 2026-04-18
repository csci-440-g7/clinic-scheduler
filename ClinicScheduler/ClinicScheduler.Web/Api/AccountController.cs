using ClinicScheduler.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClinicScheduler.Web.Api;

/// <summary>Handles cookie-based authentication for the Blazor server app.</summary>
[Route("account")]
[AllowAnonymous]
public class AccountController(SignInManager<AppUser> signInManager) : Controller
{
    /// <summary>Signs in a user with email and password, then redirects to <paramref name="returnUrl"/>.</summary>
    [HttpPost("login")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login(
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string? returnUrl)
    {
        var result = await signInManager.PasswordSignInAsync(
            email, password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : Redirect("/");

        if (result.IsLockedOut)
            return Redirect($"/login?error=2&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");

        return Redirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
    }

    /// <summary>Signs the current user out and redirects to the login page.</summary>
    [HttpPost("logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Redirect("/login");
    }
}
