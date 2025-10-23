using Microsoft.UI.Xaml;

namespace AppShell.Platforms.Windows
{
    public static class WindowsHelper
    {
        public static void ConfigureTitleBar(Microsoft.UI.Xaml.Window window)
        {
            // Keep the default title bar but customize it
            window.Title = "MKV Header Tool";
            
            // Initialize system tray functionality and set window icon
            SystemTrayHelper.Initialize(window);
        }
    }
}
