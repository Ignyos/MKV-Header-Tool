using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using AppShell.Backend.Models;
using System.Linq;
using System.Collections.Generic;
using System.IO;
#if WINDOWS
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
#endif
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;

namespace AppShell.Services;

/// <summary>
/// JSInterop bridge service that exposes C# backend functionality to JavaScript frontend.
/// This is the main bridge between the frontend and backend in the Blazor Hybrid architecture.
/// </summary>
public class JsBridgeService
{
    private readonly IAppService _appService;
    private readonly IMkvService _mkvService;
    private readonly ILogger<JsBridgeService> _logger;

    public JsBridgeService(IAppService appService, IMkvService mkvService, ILogger<JsBridgeService> logger)
    {
        _appService = appService;
        _mkvService = mkvService;
        _logger = logger;
    }

    /// <summary>
    /// Get application information for the frontend
    /// </summary>
    [JSInvokable]
    public async Task<string> GetAppInfoAsync()
    {
        try
        {
            _logger.LogInformation("Bridge: Getting app info");
            var appInfo = await _appService.GetAppInfoAsync();
            return JsonSerializer.Serialize(appInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting app info");
            throw;
        }
    }

    // MKV-specific bridge methods

    /// <summary>
    /// Gets all available MKV properties from mkvpropedit
    /// </summary>
    [JSInvokable]
    public async Task<string> GetAvailableMkvPropertiesAsync()
    {
        try
        {
            _logger.LogInformation("Bridge: Getting available MKV properties");
            var properties = await _mkvService.GetAvailablePropertiesAsync();
            return JsonSerializer.Serialize(properties);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available MKV properties");
            throw;
        }
    }

    /// <summary>
    /// Opens a native file picker to select one or more MKV files. Returns JSON array of absolute file paths.
    /// </summary>
    [JSInvokable]
    public async Task<string> PickMkvFilesAsync()
    {
        try
        {
            _logger.LogInformation("Bridge: Picking MKV files via file picker");

            var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".mkv" } },
                { DevicePlatform.MacCatalyst, new[] { "public.movie" } },
                { DevicePlatform.iOS, new[] { "public.movie" } },
                { DevicePlatform.Android, new[] { "video/*" } },
                { DevicePlatform.Tizen, new[] { "*/*" } }
            });

            var options = new PickOptions
            {
                PickerTitle = "Select MKV file(s)",
                FileTypes = fileTypes
            };

            IEnumerable<FileResult> picked;

            try
            {
                picked = await FilePicker.Default.PickMultipleAsync(options) ?? Array.Empty<FileResult>();
            }
            catch
            {
                // Some platforms do not support multiple selection; fall back to single
                var single = await FilePicker.Default.PickAsync(options);
                picked = single != null ? new[] { single } : Array.Empty<FileResult>();
            }

            var paths = picked
                .Select(fr => fr.FullPath)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Where(p => p!.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToList()!;

            var items = paths.Select(p => new FileListItem(p!, Path.GetFileName(p!))).ToList();
            return JsonSerializer.Serialize(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during file picking");
            return JsonSerializer.Serialize(Array.Empty<FileListItem>());
        }
    }

    /// <summary>
    /// Opens a native folder picker (Windows) and returns JSON array of MKV files in that folder (non-recursive).
    /// </summary>
    [JSInvokable]
    public async Task<string> PickMkvFolderAsync()
    {
        try
        {
            _logger.LogInformation("Bridge: Picking folder to enumerate MKV files");

            string? folderPath = await PickFolderAsync();
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return JsonSerializer.Serialize(Array.Empty<string>());
            }

            var files = Directory.EnumerateFiles(folderPath, "*.mkv", SearchOption.TopDirectoryOnly)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .Select(p => new FileListItem(p, Path.GetFileName(p)))
                .ToArray();

            return JsonSerializer.Serialize(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during folder picking");
            return JsonSerializer.Serialize(Array.Empty<FileListItem>());
        }
    }

#if WINDOWS
    private async Task<string?> PickFolderAsync()
    {
        // Windows-specific folder picker via WinRT
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");

        // Get the active WinUI window handle
        var window = Application.Current?.Windows?.FirstOrDefault();
        if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window appWindow)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(appWindow);
            InitializeWithWindow.Initialize(picker, hwnd);
        }

        StorageFolder? folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
#else
    private Task<string?> PickFolderAsync()
    {
        // Not supported on this platform via folder picker
        return Task.FromResult<string?>(null);
    }
#endif

    private record FileListItem(string FullPath, string FileName);

    /// <summary>
    /// Reads properties from an MKV file
    /// </summary>
    [JSInvokable]
    public async Task<string> ReadMkvFileAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Bridge: Reading MKV file {FilePath}", filePath);
            var fileInfo = await _mkvService.ReadFilePropertiesAsync(filePath);
            return JsonSerializer.Serialize(fileInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading MKV file");
            var errorResult = new MkvFileInfo(filePath, new List<MkvProperty>(), new List<MkvTrackInfo>(),
                IsValid: false, ErrorMessage: ex.Message);
            return JsonSerializer.Serialize(errorResult);
        }
    }

    /// <summary>
    /// Validates if a file is a valid MKV file
    /// </summary>
    [JSInvokable]
    public async Task<bool> IsValidMkvFileAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Bridge: Validating MKV file {FilePath}", filePath);
            return await _mkvService.IsValidMkvFileAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating MKV file");
            return false;
        }
    }

    /// <summary>
    /// Applies changes to an MKV file
    /// </summary>
    [JSInvokable]
    public async Task<string> ApplyMkvChangesAsync(string filePath, string changesJson)
    {
        try
        {
            _logger.LogInformation("Bridge: Applying changes to MKV file {FilePath}", filePath);
            var changes = JsonSerializer.Deserialize<List<MkvPropertyChange>>(changesJson);
            var result = await _mkvService.ApplyChangesAsync(filePath, changes ?? new List<MkvPropertyChange>());
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying MKV changes");
            var errorResult = new MkvEditResult(false, ex.Message);
            return JsonSerializer.Serialize(errorResult);
        }
    }
}
