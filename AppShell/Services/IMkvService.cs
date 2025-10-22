using AppShell.Backend.Models;

namespace AppShell.Services;

/// <summary>
/// Service interface for working with MKV files using mkvpropedit
/// </summary>
public interface IMkvService
{
    /// <summary>
    /// Gets all available property names and their types from mkvpropedit
    /// </summary>
    /// <returns>List of all available MKV properties with their metadata</returns>
    Task<List<MkvProperty>> GetAvailablePropertiesAsync();

    /// <summary>
    /// Reads the current properties and track information from an MKV file
    /// </summary>
    /// <param name="filePath">Path to the MKV file</param>
    /// <returns>File information including current property values</returns>
    Task<MkvFileInfo> ReadFilePropertiesAsync(string filePath);

    /// <summary>
    /// Applies a list of property changes to an MKV file
    /// </summary>
    /// <param name="filePath">Path to the MKV file to modify</param>
    /// <param name="changes">List of changes to apply</param>
    /// <returns>Result of the edit operation</returns>
    Task<MkvEditResult> ApplyChangesAsync(string filePath, List<MkvPropertyChange> changes);

    /// <summary>
    /// Validates that an MKV file exists and is accessible
    /// </summary>
    /// <param name="filePath">Path to check</param>
    /// <returns>True if file is valid MKV, false otherwise</returns>
    Task<bool> IsValidMkvFileAsync(string filePath);

    /// <summary>
    /// Gets the path to the mkvpropedit executable
    /// </summary>
    string MkvPropEditPath { get; }
}