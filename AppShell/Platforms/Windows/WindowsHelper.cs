using Microsoft.UI.Xaml;

namespace AppShell.Platforms.Windows
{
    public static class WindowsHelper
    {
        public static void ConfigureTitleBar(Microsoft.UI.Xaml.Window window)
        {
            // Hide the default title bar and extend content into title bar area
            window.ExtendsContentIntoTitleBar = true;
            window.SetTitleBar(null);
            window.Title = "MKV Header Tool";
        }
    }
}
