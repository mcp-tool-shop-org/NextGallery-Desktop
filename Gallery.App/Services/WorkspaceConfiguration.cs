using Gallery.Domain.Sources;

namespace Gallery.App.Services;

/// <summary>
/// Holds the active workspace/folder path configuration.
/// Set during app initialization based on launch parameters.
/// </summary>
public sealed class WorkspaceConfiguration
{
    /// <summary>
    /// The root path for the gallery source.
    /// Null if no path specified (will show folder picker).
    /// </summary>
    public string? WorkspacePath { get; set; }

    /// <summary>
    /// Force a specific source type instead of auto-detection.
    /// Null means auto-detect based on folder contents.
    /// </summary>
    public SourceType? ForcedSourceType { get; set; }

    /// <summary>
    /// Whether a workspace/folder path has been configured.
    /// </summary>
    public bool HasWorkspace => !string.IsNullOrEmpty(WorkspacePath);
}
