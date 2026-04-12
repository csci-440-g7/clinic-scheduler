using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ClinicScheduler.Core.Services;
using ClinicScheduler.Web.Components;
using ClinicScheduler.Shared.Services;
using ClinicScheduler.Web.Services;
using ClinicScheduler.Core.Interfaces;
using ClinicScheduler.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Register the Database Context
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnectionString))
{
    throw new InvalidOperationException(
        "The connection string 'DefaultConnection' is missing or empty. Please configure a valid connection string in appsettings.json or environment configuration.");
}

builder.Services.AddDbContext<ClinicDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register the repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Register business logic services
builder.Services.AddScoped<AppointmentSchedulingService>();

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
builder.Services.AddControllers();

// Add device-specific services used by the ClinicScheduler.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddScoped<SessionState>();
builder.Services.AddScoped<ClinicDataStore>();

builder.Services.AddMudServices();
var app = builder.Build();

// Auto-apply EF migrations on startup (safe to run repeatedly; no-ops when up-to-date)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
    db.Database.Migrate();
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

app.UseAntiforgery();

app.MapStaticAssets();

// Map API endpoints
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(ClinicScheduler.Shared._Imports).Assembly,
        typeof(ClinicScheduler.Web.Client._Imports).Assembly);

app.Run();