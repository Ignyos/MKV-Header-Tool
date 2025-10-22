using Microsoft.Extensions.Logging;
using AppShell.Services;

#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using System.Runtime.InteropServices;
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
            // Set the application title using lifecycle events
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(windowsLifecycleBuilder =>
                {
                    windowsLifecycleBuilder.OnWindowCreated(window =>
                    {
                        // Just set the window title - use standard Windows title bar
                        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        var id = Win32Interop.GetWindowIdFromWindow(handle);
                        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
                        
                        appWindow.Title = "MKV Header Tool";
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
