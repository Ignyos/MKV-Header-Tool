namespace AppShell
{
  public partial class MainPage : ContentPage
  {
    public MainPage()
    {
      InitializeComponent();

#if WINDOWS
      // Configure Windows title bar customization when the page loads
      this.Loaded += OnPageLoaded;
#endif
    }

#if WINDOWS
    private void OnPageLoaded(object? sender, EventArgs e)
    {
      ConfigureTitleBar();
    }

    private void ConfigureTitleBar()
    {
      try
      {
        // Get the native window through the platform-specific handler
        var window = GetPlatformWindow();
        if (window != null)
        {
          // Configure title bar hiding
          window.ExtendsContentIntoTitleBar = true;
          window.SetTitleBar(null);
          window.Title = "Pure JS Frontend - MAUI Template";
          
          // Force a layout update
          window.Activate();
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Error configuring title bar: {ex.Message}");
      }
    }

    private Microsoft.UI.Xaml.Window? GetPlatformWindow()
    {
      // Try multiple approaches to get the window
      var mauiContext = this.Handler?.MauiContext;
      if (mauiContext?.Services != null)
      {
        var window = mauiContext.Services.GetService<Microsoft.UI.Xaml.Window>();
        if (window != null) return window;
      }

      // Alternative approach - get through the application
      var app = Microsoft.UI.Xaml.Application.Current as Microsoft.Maui.MauiWinUIApplication;
      if (app?.Application?.Windows?.Count > 0)
      {
        return app.Application.Windows[0].Handler?.PlatformView as Microsoft.UI.Xaml.Window;
      }

      return null;
    }
#endif
  }
}
