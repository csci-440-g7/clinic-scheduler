using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ClinicScheduler.Shared.Services;
using ClinicScheduler.Web.Client.Services;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add device-specific services used by the ClinicScheduler.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

builder.Services.AddMudServices();
await builder.Build().RunAsync();