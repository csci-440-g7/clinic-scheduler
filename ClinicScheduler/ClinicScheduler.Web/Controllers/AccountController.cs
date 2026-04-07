using ClinicScheduler.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClinicScheduler.Web.Controllers;

[Route("account")]
[AllowAnonymous]
public class AccountController(SignInManager<AppUser> signInManager) : Controller
{
    [HttpPost("login")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login(
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string? returnUrl)
    {
        var result = await signInManager.PasswordSignInAsync(
            email, password, isPersistent: false, lockoutOnFailure: false);

        if (result.Succeeded)
            return Redirect(returnUrl ?? "/");

        return Redirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
    }

    [HttpPost("logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Redirect("/login");
    }
}
