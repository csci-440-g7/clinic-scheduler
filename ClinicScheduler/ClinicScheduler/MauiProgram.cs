using Microsoft.Extensions.Logging;
using ClinicScheduler.Shared.Services;
using ClinicScheduler.Services;
using MudBlazor.Services;

namespace ClinicScheduler;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        // Add device-specific services used by the ClinicScheduler.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        builder.Services.AddMudServices();
        return builder.Build();
    }
}