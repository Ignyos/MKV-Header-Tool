using Microsoft.Extensions.Logging;
using AppShell.Services;

#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using System.Runtime.InteropServices;
using AppShell.Platforms.Windows;
#endif

namespace AppShell
{

    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if WINDOWS
            // Configure Windows platform using helper
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(windowsLifecycleBuilder =>
                {
                    windowsLifecycleBuilder.OnWindowCreated(window =>
                    {
                        // Use our custom Windows helper for full configuration
                        WindowsHelper.ConfigureTitleBar(window);
                    });
                });
            });
#endif

            // Add Blazor Hybrid services
            builder.Services.AddMauiBlazorWebView();

            // Register application services
            builder.Services.AddScoped<IAppService, AppService>();
            builder.Services.AddScoped<IMkvService, MkvService>();
            builder.Services.AddScoped<JsBridgeService>();

            // Register pages
            builder.Services.AddTransient<MainPage>();

#if DEBUG
    		builder.Logging.AddDebug();
            builder.Services.AddBlazorWebViewDeveloperTools();
#endif

            return builder.Build();
        }
    }
}
