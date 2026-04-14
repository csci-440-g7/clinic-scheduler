namespace ClinicScheduler.Shared.Services;

using Microsoft.JSInterop;

public class SessionState
{
    private const string AuthKey = "clinicScheduler.authenticated";
    private const string RoleKey = "clinicScheduler.role";

    private readonly IJSRuntime _js;
    private bool _loaded;

    public bool IsAuthenticated { get; private set; }
    public string? Role { get; private set; } // "patient" | "doctor" | "staff"

    public SessionState(IJSRuntime js)
    {
        _js = js;
    }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        try
        {
            var authStr = await _js.InvokeAsync<string?>("localStorage.getItem", AuthKey);
            var roleStr = await _js.InvokeAsync<string?>("localStorage.getItem", RoleKey);

            IsAuthenticated = string.Equals(authStr, "true", StringComparison.OrdinalIgnoreCase);
            Role = string.IsNullOrWhiteSpace(roleStr) ? null : roleStr;
            _loaded = true;
        }
        catch
        {
            // If JS is not available yet (prerender/server), just fall back to defaults.
            IsAuthenticated = false;
            Role = null;
            _loaded = false;
        }
    }

    public async Task SetPreAuthAsync(string role)
    {
        Role = role;
        IsAuthenticated = false;
        await PersistAsync();
    }

    public async Task AuthenticateAsync()
    {
        IsAuthenticated = true;
        await PersistAsync();
    }

    public async Task SignOutAsync()
    {
        IsAuthenticated = false;
        Role = null;
        await ClearAsync();
    }

    private async Task PersistAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", AuthKey, IsAuthenticated ? "true" : "false");
            await _js.InvokeVoidAsync("localStorage.setItem", RoleKey, Role ?? "");
        }
        catch
        {
            // ignore persistence errors in environments where JS storage isn't accessible
        }
    }

    private async Task ClearAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", AuthKey);
            await _js.InvokeVoidAsync("localStorage.removeItem", RoleKey);
        }
        catch
        {
            // ignore
        }
    }
}

