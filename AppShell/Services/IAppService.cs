namespace AppShell.Services;

/// <summary>
/// Interface for application business logic services.
/// This defines the contract between the Blazor bridge and backend functionality.
/// </summary>
public interface IAppService
{
}

/// <summary>
/// Operation result data transfer object
/// </summary>
public record OperationResult(
    bool Success,
    string Message,
    object? Data = null
);
