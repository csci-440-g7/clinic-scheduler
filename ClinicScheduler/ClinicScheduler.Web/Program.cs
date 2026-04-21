using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ClinicScheduler.Web;
using ClinicScheduler.Core.Services;
using ClinicScheduler.Web.Components;
using ClinicScheduler.Shared.Services;
using ClinicScheduler.Web.Services;
using ClinicScheduler.Core.Interfaces;
using ClinicScheduler.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Register the Database Context
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnectionString) && !builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException(
        "The connection string 'DefaultConnection' is missing or empty. Please configure a valid connection string in appsettings.json or environment configuration.");
}

builder.Services.AddDbContextFactory<ClinicDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
// Also register ClinicDbContext directly (scoped) for controllers and services that need it
builder.Services.AddDbContext<ClinicDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register the repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Register business logic services
builder.Services.AddScoped<AppointmentSchedulingService>();
builder.Services.AddScoped<MissedAppointmentService>();
builder.Services.AddScoped<AppointmentNotificationService>();

// Background services
builder.Services.AddHostedService<AppointmentReminderService>();

// ASP.NET Core Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // Baseline: all environments
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;

    // Elevated: production only
    if (builder.Environment.IsProduction())
    {
        options.Password.RequiredLength = 10;
    }

    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ClinicDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization();

builder.Services.AddCascadingAuthenticationState();

// CORS — allow same-origin in production; configure AllowedOrigins in appsettings for external clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("AppPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
            if (origins.Length > 0)
                policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
        }
    });
});

// Add API Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, CancellationToken) =>
    {
        if (context.JsonTypeInfo.Type.IsEnum)
        {
            schema.Type = JsonSchemaType.String;
            schema.Enum = context.JsonTypeInfo.Type
                .GetEnumNames()
                .Select(name => JsonValue.Create(name))
                .Cast<JsonNode>()
                .ToArray();
        }

        return Task.CompletedTask;
    });
    options.AddDocumentTransformer((document, AppContext, CancellationToken) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Clinic Scheduler API",
            Version = "v1"
        };
        
        return Task.CompletedTask;
    });
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Add device-specific services used by the ClinicScheduler.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

builder.Services.AddMudServices();
var app = builder.Build();

// Auto-apply EF migrations on startup (safe to run repeatedly; no-ops when up-to-date)
// In development, handle database errors gracefully to allow testing without a database
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Database migration skipped: {Message}", ex.Message);
        }

        try
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var adminPassword = app.Configuration["SeedAdmin:Password"]
                ?? throw new InvalidOperationException("SeedAdmin:Password is not configured.");
            await DatabaseSeeder.SeedAsync(db, userManager, roleManager, adminPassword);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Database seed skipped: {Message}", ex.Message);
        }
    }
}
else
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var adminPassword = app.Configuration["SeedAdmin:Password"]
            ?? throw new InvalidOperationException(
                "SeedAdmin:Password must be set via environment variable (SeedAdmin__Password) in production.");
        db.Database.Migrate();
        await DatabaseSeeder.SeedAsync(db, userManager, roleManager, adminPassword);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();

    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ClinicScheduler API v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// HTTPS termination is handled by the load balancer in production; skip redirect in container
if (!app.Environment.IsProduction())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseBlazorFrameworkFiles();
app.UseCors("AppPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets().AllowAnonymous();

// Map API endpoints
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(ClinicScheduler.Shared._Imports).Assembly,
        typeof(ClinicScheduler.Web.Client._Imports).Assembly);

app.Run();