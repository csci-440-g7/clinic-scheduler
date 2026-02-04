using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ClinicScheduler.Shared.Services;
using ClinicScheduler.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add device-specific services used by the ClinicScheduler.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

await builder.Build().RunAsync();