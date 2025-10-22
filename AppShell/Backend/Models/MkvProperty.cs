namespace AppShell.Backend.Models;

/// <summary>
/// Represents a single MKV property that can be modified
/// </summary>
public record MkvProperty(
    string Name,
    string DisplayName,
    string? CurrentValue,
    MkvPropertyType Type,
    string Section,
    string? Description = null,
    bool IsRequired = false,
    bool CanBeDeleted = true
)
{
    /// <summary>
    /// For boolean properties, gets whether the current value represents "true"
    /// </summary>
    public bool BooleanValue => Type == MkvPropertyType.Boolean && 
                               (CurrentValue == "1" || CurrentValue == "true" || CurrentValue == "yes");
}

/// <summary>
/// Types of MKV properties as returned by mkvpropedit --list-property-names
/// </summary>
public enum MkvPropertyType
{
    Boolean,
    String,
    Integer,
    UnsignedInteger,
    Float,
    Binary,
    Unknown
}

/// <summary>
/// Result of reading MKV file properties
/// </summary>
public record MkvFileInfo(
    string FilePath,
    List<MkvProperty> Properties,
    List<MkvTrackInfo> Tracks,
    string? SegmentTitle = null,
    bool IsValid = true,
    string? ErrorMessage = null
);

/// <summary>
/// Information about an MKV track
/// </summary>
public record MkvTrackInfo(
    int TrackNumber,
    string TrackType, // "video", "audio", "subtitle", "button"
    string? Name,
    string? Language,
    string? LanguageIetf,
    string? LanguageLegacy,
    bool IsDefault,
    bool IsEnabled,
    bool IsForced
);

/// <summary>
/// Represents a change to be made to an MKV property
/// </summary>
public record MkvPropertyChange(
    string PropertyName,
    string Section,
    MkvPropertyChangeType ChangeType,
    string? NewValue = null
);

/// <summary>
/// Types of changes that can be made to MKV properties
/// </summary>
public enum MkvPropertyChangeType
{
    Set,
    Delete,
    Add
}

/// <summary>
/// Result of applying changes to an MKV file
/// </summary>
public record MkvEditResult(
    bool Success,
    string? ErrorMessage = null,
    List<string> Warnings = null!,
    int ExitCode = 0
)
{
    public List<string> Warnings { get; init; } = Warnings ?? new List<string>();
}