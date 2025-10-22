using Microsoft.Extensions.Logging;

namespace AppShell.Services;

/// <summary>
/// Default implementation of application business logic services.
/// This provides the backend functionality exposed through the JSInterop bridge.
/// </summary>
public class AppService : IAppService
{
    private readonly ILogger<AppService> _logger;
    private readonly DateTime _startTime;
    private readonly Dictionary<string, object> _preferences;

    public AppService(ILogger<AppService> logger)
    {
        _logger = logger;
        _startTime = DateTime.UtcNow;
        _preferences = new Dictionary<string, object>
        {
            { "theme", "light" },
            { "language", "en" },
            { "autoSave", true }
        };
    }
}
