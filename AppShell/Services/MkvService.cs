using AppShell.Backend.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AppShell.Services;

/// <summary>
/// Implementation of MKV service using mkvpropedit.exe
/// </summary>
public class MkvService : IMkvService
{
    private readonly ILogger<MkvService> _logger;
    private readonly string _mkvPropEditPath;
    private List<MkvProperty>? _cachedAvailableProperties;

    public MkvService(ILogger<MkvService> logger)
    {
        _logger = logger;
        
        // Path to mkvpropedit.exe in the Resources folder
        _mkvPropEditPath = Path.Combine(AppContext.BaseDirectory, "Resources", "mkvpropedit.exe");
        
        if (!File.Exists(_mkvPropEditPath))
        {
            _logger.LogError("mkvpropedit.exe not found at: {Path}", _mkvPropEditPath);
            throw new FileNotFoundException($"mkvpropedit.exe not found at: {_mkvPropEditPath}");
        }

        _logger.LogInformation("MkvService initialized with mkvpropedit at: {Path}", _mkvPropEditPath);
    }

    public string MkvPropEditPath => _mkvPropEditPath;

    public async Task<List<MkvProperty>> GetAvailablePropertiesAsync()
    {
        if (_cachedAvailableProperties != null)
        {
            return _cachedAvailableProperties;
        }

        _logger.LogInformation("Getting available MKV properties from mkvpropedit");

        try
        {
            var result = await RunMkvPropEditAsync("--list-property-names");
            
            if (result.ExitCode != 0)
            {
                _logger.LogError("Failed to get property names. Exit code: {ExitCode}, Error: {Error}", 
                    result.ExitCode, result.StandardError);
                return new List<MkvProperty>();
            }

            _cachedAvailableProperties = ParsePropertyList(result.StandardOutput);
            _logger.LogInformation("Loaded {Count} available MKV properties", _cachedAvailableProperties.Count);
            
            return _cachedAvailableProperties;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available properties");
            return new List<MkvProperty>();
        }
    }

    public async Task<MkvFileInfo> ReadFilePropertiesAsync(string filePath)
    {
        _logger.LogInformation("Reading properties from MKV file: {FilePath}", filePath);

        if (!await IsValidMkvFileAsync(filePath))
        {
            return new MkvFileInfo(filePath, new List<MkvProperty>(), new List<MkvTrackInfo>(), 
                IsValid: false, ErrorMessage: "File is not a valid MKV file");
        }

        try
        {
            // Get detailed file information using mkvmerge -J (JSON format)
            var identifyResult = await RunMkvMergeAsync($"-J \"{filePath}\"");
            
            if (identifyResult.ExitCode != 0)
            {
                return new MkvFileInfo(filePath, new List<MkvProperty>(), new List<MkvTrackInfo>(),
                    IsValid: false, ErrorMessage: $"Failed to identify file: {identifyResult.StandardError}");
            }

            var tracks = ParseTrackInfo(identifyResult.StandardOutput);
            
            // Get all available properties that can be edited
            var availableProperties = await GetAvailablePropertiesAsync();
            
            // Filter to commonly used boolean properties for the initial implementation
            var booleanProperties = availableProperties
                .Where(p => p.Type == MkvPropertyType.Boolean)
                .Where(p => p.Name.StartsWith("flag-") || 
                           p.Name.Contains("default") || 
                           p.Name.Contains("enabled") ||
                           p.Name.Contains("forced"))
                .ToList();

            // Set default values for boolean properties (they start as false/0)
            var properties = booleanProperties.Select(prop => 
                prop with { CurrentValue = "0" }).ToList();

            _logger.LogInformation("Successfully read MKV file with {TrackCount} tracks and {PropertyCount} properties", 
                tracks.Count, properties.Count);

            return new MkvFileInfo(filePath, properties, tracks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file properties");
            return new MkvFileInfo(filePath, new List<MkvProperty>(), new List<MkvTrackInfo>(),
                IsValid: false, ErrorMessage: ex.Message);
        }
    }

    public async Task<MkvEditResult> ApplyChangesAsync(string filePath, List<MkvPropertyChange> changes)
    {
        _logger.LogInformation("Applying {Count} changes to MKV file: {FilePath}", changes.Count, filePath);

        if (!changes.Any())
        {
            return new MkvEditResult(true);
        }

        try
        {
            var args = new List<string> { $"\"{filePath}\"" };
            
            // Group changes by section and build command arguments
            var changesBySection = changes.GroupBy(c => c.Section);
            
            foreach (var sectionGroup in changesBySection)
            {
                // Add edit selector for this section
                if (sectionGroup.Key != "info") // info is default, no need to specify
                {
                    args.Add($"--edit {sectionGroup.Key}");
                }

                // Add all changes for this section
                foreach (var change in sectionGroup)
                {
                    switch (change.ChangeType)
                    {
                        case MkvPropertyChangeType.Set:
                            args.Add($"--set {change.PropertyName}={change.NewValue}");
                            break;
                        case MkvPropertyChangeType.Delete:
                            args.Add($"--delete {change.PropertyName}");
                            break;
                        case MkvPropertyChangeType.Add:
                            args.Add($"--add {change.PropertyName}={change.NewValue}");
                            break;
                    }
                }
            }

            var commandLine = string.Join(" ", args);
            _logger.LogInformation("Executing mkvpropedit with args: {Args}", commandLine);

            var result = await RunMkvPropEditAsync(commandLine);
            
            var warnings = ParseWarnings(result.StandardOutput + result.StandardError);
            
            if (result.ExitCode == 0)
            {
                _logger.LogInformation("Successfully applied changes to MKV file");
                return new MkvEditResult(true, Warnings: warnings);
            }
            else if (result.ExitCode == 1)
            {
                _logger.LogWarning("Changes applied with warnings. Exit code: {ExitCode}", result.ExitCode);
                return new MkvEditResult(true, "Operation completed with warnings", warnings, result.ExitCode);
            }
            else
            {
                _logger.LogError("Failed to apply changes. Exit code: {ExitCode}, Error: {Error}", 
                    result.ExitCode, result.StandardError);
                return new MkvEditResult(false, result.StandardError, warnings, result.ExitCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying changes");
            return new MkvEditResult(false, ex.Message);
        }
    }

    public async Task<bool> IsValidMkvFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("File does not exist: {FilePath}", filePath);
            return false;
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension != ".mkv")
        {
            _logger.LogDebug("File does not have .mkv extension: {FilePath}", filePath);
            return false;
        }

        try
        {
            // Use mkvmerge --identify to validate the file
            // This is the proper way to check if a file is a valid MKV
            var result = await RunMkvMergeAsync($"--identify \"{filePath}\"");
            
            var isValid = result.ExitCode == 0 && !string.IsNullOrEmpty(result.StandardOutput);
            
            _logger.LogDebug("MKV file validation for {FilePath}: ExitCode={ExitCode}, Valid={IsValid}, Output='{Output}', Error='{Error}'", 
                filePath, result.ExitCode, isValid,
                result.StandardOutput?.Length > 0 ? result.StandardOutput.Substring(0, Math.Min(100, result.StandardOutput.Length)) : "", 
                result.StandardError?.Length > 0 ? result.StandardError.Substring(0, Math.Min(100, result.StandardError.Length)) : "");
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating MKV file: {FilePath}", filePath);
            return false;
        }
    }

    private async Task<ProcessResult> RunMkvPropEditAsync(string arguments)
    {
        return await RunProcessAsync(_mkvPropEditPath, arguments);
    }

    private async Task<ProcessResult> RunMkvMergeAsync(string arguments)
    {
        var mkvMergePath = Path.Combine(Path.GetDirectoryName(_mkvPropEditPath)!, "mkvmerge.exe");
        
        if (!File.Exists(mkvMergePath))
        {
            _logger.LogWarning("mkvmerge.exe not found at: {Path}, trying system PATH", mkvMergePath);
            // Try to use system PATH
            mkvMergePath = "mkvmerge";
        }
        else
        {
            _logger.LogDebug("Using mkvmerge.exe at: {Path}", mkvMergePath);
        }

        return await RunProcessAsync(mkvMergePath, arguments);
    }

    private async Task<ProcessResult> RunProcessAsync(string fileName, string arguments)
    {
#if IOS
        // Process.Start() is not supported on iOS
        // MKV files are primarily desktop/server files, so this is acceptable
        _logger.LogWarning("Process execution not supported on iOS platform");
        return new ProcessResult(-1, "", "Process execution not supported on iOS");
#else
        using var process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        var output = string.Empty;
        var error = string.Empty;

        process.OutputDataReceived += (sender, e) => {
            if (e.Data != null) output += e.Data + Environment.NewLine;
        };
        
        process.ErrorDataReceived += (sender, e) => {
            if (e.Data != null) error += e.Data + Environment.NewLine;
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return new ProcessResult(process.ExitCode, output.TrimEnd(), error.TrimEnd());
#endif
    }

    private List<MkvProperty> ParsePropertyList(string output)
    {
        var properties = new List<MkvProperty>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                continue;

            // Parse format: "property_name (type): description"
            var match = Regex.Match(trimmedLine, @"^(\S+)\s+\(([^)]+)\):\s*(.*)$");
            if (match.Success)
            {
                var name = match.Groups[1].Value;
                var typeStr = match.Groups[2].Value;
                var description = match.Groups[3].Value;

                var type = typeStr.ToLowerInvariant() switch
                {
                    "boolean" => MkvPropertyType.Boolean,
                    "string" => MkvPropertyType.String,
                    "integer" => MkvPropertyType.Integer,
                    "unsigned integer" => MkvPropertyType.UnsignedInteger,
                    "float" => MkvPropertyType.Float,
                    "binary" => MkvPropertyType.Binary,
                    _ => MkvPropertyType.Unknown
                };

                // Determine section based on property name patterns
                var section = DeterminePropertySection(name);

                properties.Add(new MkvProperty(
                    Name: name,
                    DisplayName: FormatDisplayName(name),
                    CurrentValue: null,
                    Type: type,
                    Section: section,
                    Description: description
                ));
            }
        }

        return properties;
    }

    private string DeterminePropertySection(string propertyName)
    {
        // Common track properties start with these prefixes
        if (propertyName.StartsWith("flag-") || 
            propertyName.StartsWith("language") ||
            propertyName.StartsWith("name") ||
            propertyName.StartsWith("codec") ||
            propertyName.Contains("track"))
        {
            return "track";
        }

        // Default to segment info
        return "info";
    }

    private string FormatDisplayName(string propertyName)
    {
        return propertyName
            .Replace("-", " ")
            .Replace("_", " ")
            .Split(' ')
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant())
            .Aggregate((a, b) => a + " " + b);
    }

    private List<MkvTrackInfo> ParseTrackInfo(string output)
    {
        var tracks = new List<MkvTrackInfo>();
        
        try
        {
            // Parse JSON output from mkvmerge -J
            var jsonDoc = JsonDocument.Parse(output);
            var tracksArray = jsonDoc.RootElement.GetProperty("tracks");

            foreach (var trackElement in tracksArray.EnumerateArray())
            {
                var trackId = trackElement.GetProperty("id").GetInt32();
                var trackType = trackElement.GetProperty("type").GetString()?.ToLowerInvariant() ?? "unknown";
                
                var properties = trackElement.GetProperty("properties");
                
                // Extract track name
                string? trackName = null;
                if (properties.TryGetProperty("track_name", out var nameElement))
                {
                    trackName = nameElement.GetString();
                }
                
                // Extract language (both IETF and legacy), and a selected display value
                string? languageIetf = null;
                string? languageLegacy = null;
                string? language = null;
                if (properties.TryGetProperty("language_ietf", out var ietfLangElement))
                {
                    languageIetf = ietfLangElement.GetString();
                    if (!string.IsNullOrEmpty(languageIetf) && languageIetf != "und")
                    {
                        language = languageIetf;
                    }
                }
                if (properties.TryGetProperty("language", out var legacyLangElement))
                {
                    languageLegacy = legacyLangElement.GetString();
                    if (string.IsNullOrEmpty(language) && !string.IsNullOrEmpty(languageLegacy) && languageLegacy != "und")
                    {
                        language = languageLegacy;
                    }
                }
                
                // Extract flags
                var isDefault = properties.TryGetProperty("default_track", out var defaultElement) && 
                              defaultElement.GetBoolean();
                var isEnabled = properties.TryGetProperty("enabled_track", out var enabledElement) && 
                              enabledElement.GetBoolean();
                var isForced = properties.TryGetProperty("forced_track", out var forcedElement) && 
                             forcedElement.GetBoolean();

                tracks.Add(new MkvTrackInfo(
                    TrackNumber: trackId,
                    TrackType: trackType,
                    Name: trackName,
                    Language: language,
                    LanguageIetf: languageIetf,
                    LanguageLegacy: languageLegacy,
                    IsDefault: isDefault,
                    IsEnabled: isEnabled,
                    IsForced: isForced
                ));
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON output from mkvmerge identify");
            // Fallback to basic parsing if JSON fails
            return ParseTracksFromBasicIdentifyOutput(output);
        }

        return tracks;
    }

    private List<MkvTrackInfo> ParseTracksFromBasicIdentifyOutput(string output)
    {
        var tracks = new List<MkvTrackInfo>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Parse mkvmerge identify output format
            var trackMatch = Regex.Match(line, @"Track ID (\d+): (\w+)");
            if (trackMatch.Success)
            {
                var trackNumber = int.Parse(trackMatch.Groups[1].Value);
                var trackType = trackMatch.Groups[2].Value.ToLowerInvariant();

                tracks.Add(new MkvTrackInfo(
                    TrackNumber: trackNumber,
                    TrackType: trackType,
                    Name: null,
                    Language: null,
                    LanguageIetf: null,
                    LanguageLegacy: null,
                    IsDefault: false,
                    IsEnabled: true,
                    IsForced: false
                ));
            }
        }

        return tracks;
    }

    // private async Task<List<MkvProperty>> BuildCurrentPropertiesFromTracks(List<MkvTrackInfo> tracks)
    // {
    //     var properties = new List<MkvProperty>();
    //     var availableProperties = await GetAvailablePropertiesAsync();

    //     // Filter to boolean properties that make sense for the UI
    //     var booleanProperties = availableProperties
    //         .Where(p => p.Type == MkvPropertyType.Boolean)
    //         .Where(p => p.Name.StartsWith("flag-") || p.Name.Contains("default") || p.Name.Contains("enabled"))
    //         .ToList();

    //     foreach (var prop in booleanProperties)
    //     {
    //         properties.Add(prop with { CurrentValue = "0" }); // Default to false
    //     }

    //     return properties;
    // }

    private List<string> ParseWarnings(string output)
    {
        var warnings = new List<string>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.Trim().StartsWith("Warning:", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add(line.Trim());
            }
        }

        return warnings;
    }

    private record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}