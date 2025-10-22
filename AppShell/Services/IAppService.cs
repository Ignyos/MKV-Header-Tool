namespace AppShell.Services;

/// <summary>
/// Interface for application business logic services.
/// This defines the contract between the Blazor bridge and backend functionality.
/// </summary>
public interface IAppService
{
    /// <summary>
    /// Get application information
    /// </summary>
    Task<AppInfo> GetAppInfoAsync();
}

/// <summary>
/// Application information data transfer object
/// </summary>
public record AppInfo(
    string Version,
    string Platform,
    DateTime StartTime
);

/// <summary>
/// Operation result data transfer object
/// </summary>
public record OperationResult(
    bool Success,
    string Message,
    object? Data = null
);
