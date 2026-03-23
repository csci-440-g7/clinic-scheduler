using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ClinicScheduler.Shared.Services;
using ClinicScheduler.Web.Client.Services;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add device-specific services used by the ClinicScheduler.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

builder.Services.AddRadzenComponents();
await builder.Build().RunAsync();