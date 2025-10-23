using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using System.Runtime.InteropServices;

namespace AppShell.Platforms.Windows
{
    public static class SystemTrayHelper
    {
        private static Microsoft.UI.Xaml.Window? _window;
        private static AppWindow? _appWindow;
        private static IntPtr _windowHandle;

        // Win32 API for setting window icon
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, int uType, int cxDesired, int cyDesired, int fuLoad);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WM_SETICON = 0x0080;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const int IMAGE_ICON = 1;
        private const int LR_LOADFROMFILE = 0x0010;

        public static void Initialize(Microsoft.UI.Xaml.Window window)
        {
            _window = window;
            
            // Get the AppWindow for more control
            _windowHandle = WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(_windowHandle);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            // Configure the window
            ConfigureWindow();
        }

        private static void ConfigureWindow()
        {
            if (_appWindow != null)
            {
                // Debug icon paths first
                DebugIconPaths();

                // Try to use our custom icon first
                var customIconPath = GetIconPath();
                bool iconSet = false;
                
                if (File.Exists(customIconPath))
                {
                    try
                    {
                        _appWindow.SetIcon(customIconPath);
                        System.Diagnostics.Debug.WriteLine($"✅ Custom AppWindow icon set successfully: {customIconPath}");
                        iconSet = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Failed to set custom AppWindow icon: {ex.Message}");
                        
                        // Try Win32 approach as fallback
                        TrySetWin32Icon(customIconPath);
                    }
                }
                
                if (!iconSet)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Custom icon not found, using default MAUI-generated icon: {customIconPath}");
                }

                // Configure title bar
                if (_appWindow.TitleBar != null)
                {
                    _appWindow.Title = "MKV Header Tool";
                }
            }
        }

        private static void TrySetWin32Icon(string iconPath)
        {
            if (_windowHandle != IntPtr.Zero && File.Exists(iconPath))
            {
                try
                {
                    // Load the icon from file
                    IntPtr hIcon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 32, 32, LR_LOADFROMFILE);
                    if (hIcon != IntPtr.Zero)
                    {
                        // Set both small and large icons
                        SendMessage(_windowHandle, WM_SETICON, ICON_SMALL, hIcon);
                        SendMessage(_windowHandle, WM_SETICON, ICON_BIG, hIcon);
                        System.Diagnostics.Debug.WriteLine($"✅ Win32 window icon set successfully: {iconPath}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Failed to load icon from file: {iconPath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Win32 icon setting failed: {ex.Message}");
                }
            }
        }

        public static void SetWindowIcon()
        {
            if (_appWindow != null)
            {
                var iconPath = GetIconPath();
                if (File.Exists(iconPath))
                {
                    try
                    {
                        _appWindow.SetIcon(iconPath);
                        System.Diagnostics.Debug.WriteLine($"✅ Window icon updated: {iconPath}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Failed to update window icon: {ex.Message}");
                        TrySetWin32Icon(iconPath);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Icon file not found for update: {iconPath}");
                }
            }
        }

        private static string GetIconPath()
        {
            // Build list of possible paths, handling packaged vs unpackaged scenarios
            var possiblePaths = new List<string>();

            // Try to get package path (only works for packaged apps)
            try
            {
                var packagePath = Package.Current.InstalledLocation.Path;
                possiblePaths.Add(Path.Combine(packagePath, "Resources", "AppIcon", "systray32.ico"));
                System.Diagnostics.Debug.WriteLine($"📦 Running as packaged app: {packagePath}");
            }
            catch (InvalidOperationException)
            {
                System.Diagnostics.Debug.WriteLine($"📁 Running as unpackaged app (debug mode)");
            }

            // Add common fallback paths
            possiblePaths.AddRange(new[]
            {
                // App domain base directory
                Path.Combine(AppContext.BaseDirectory, "Resources", "AppIcon", "systray32.ico"),
                // Direct in Resources folder
                Path.Combine(AppContext.BaseDirectory, "Resources", "systray32.ico"),
                // Build output directory
                Path.Combine(AppContext.BaseDirectory, "AppIcon", "systray32.ico"),
                // Relative to executable
                Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "Resources", "AppIcon", "systray32.ico")
            });

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Found icon at: {path}");
                    return path;
                }
            }

            // Return the first unpackaged path as fallback (for debugging)
            var fallbackPath = possiblePaths.FirstOrDefault() ?? Path.Combine(AppContext.BaseDirectory, "Resources", "AppIcon", "systray32.ico");
            System.Diagnostics.Debug.WriteLine($"⚠️ Icon not found, using fallback: {fallbackPath}");
            return fallbackPath;
        }

        public static void MinimizeToTray()
        {
            if (_window != null)
            {
                // For now, just minimize the window
                // Full system tray implementation would require additional packages
                if (_appWindow != null)
                {
                    _appWindow.Hide();
                }
            }
        }

        public static void RestoreFromTray()
        {
            if (_window != null && _appWindow != null)
            {
                _appWindow.Show();
                _window.Activate();
            }
        }

        /// <summary>
        /// Debug method to check icon file existence and paths
        /// </summary>
        public static void DebugIconPaths()
        {
            var iconPath = GetIconPath();
            System.Diagnostics.Debug.WriteLine($"🔍 Icon Debug Information:");
            System.Diagnostics.Debug.WriteLine($"   Target icon path: {iconPath}");
            System.Diagnostics.Debug.WriteLine($"   File exists: {File.Exists(iconPath)}");
            
            // Safe package location check
            string packageLocation = "N/A (unpackaged)";
            try
            {
                packageLocation = Package.Current.InstalledLocation.Path;
            }
            catch (InvalidOperationException)
            {
                // Running as unpackaged app (debug mode)
            }
            
            System.Diagnostics.Debug.WriteLine($"   Package location: {packageLocation}");
            System.Diagnostics.Debug.WriteLine($"   App base directory: {AppContext.BaseDirectory}");
            System.Diagnostics.Debug.WriteLine($"   Assembly location: {System.Reflection.Assembly.GetExecutingAssembly().Location}");
            
            // Check all possible locations
            var possiblePaths = new List<string>();
            
            // Add package path if available
            if (packageLocation != "N/A (unpackaged)")
            {
                possiblePaths.Add(Path.Combine(packageLocation, "Resources", "AppIcon", "systray32.ico"));
            }
            
            // Add other paths
            possiblePaths.AddRange(new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Resources", "AppIcon", "systray32.ico"),
                Path.Combine(AppContext.BaseDirectory, "Resources", "systray32.ico"),
                Path.Combine(AppContext.BaseDirectory, "AppIcon", "systray32.ico"),
                Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "Resources", "AppIcon", "systray32.ico")
            });

            System.Diagnostics.Debug.WriteLine($"   Checking possible paths:");
            foreach (var path in possiblePaths)
            {
                System.Diagnostics.Debug.WriteLine($"     {path}: {(File.Exists(path) ? "✅ EXISTS" : "❌ NOT FOUND")}");
            }
        }
    }
}