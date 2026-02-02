using Gallery.Domain.Index;

namespace Gallery.Domain.Sources;

/// <summary>
/// Abstraction for gallery data sources.
/// Allows plugging in different backends: folder scanning, CodeComfy index, Fooocus, etc.
/// </summary>
public interface IGallerySource : IDisposable
{
    /// <summary>
    /// Human-readable name of this source (e.g., "Local Folder", "CodeComfy Workspace").
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// The root path being browsed.
    /// </summary>
    string RootPath { get; }

    /// <summary>
    /// Path where output files are stored (for "Open Outputs Folder" command).
    /// </summary>
    string OutputsPath { get; }

    /// <summary>
    /// Whether this source supports polling for changes.
    /// </summary>
    bool SupportsPolling { get; }

    /// <summary>
    /// Whether this source supports file deletion.
    /// </summary>
    bool SupportsDeletion { get; }

    /// <summary>
    /// Load gallery items from this source.
    /// </summary>
    /// <param name="lastKnownGood">Previous valid items for transient error recovery</param>
    /// <returns>Load result with state, items, and any warnings</returns>
    IndexLoadResult Load(IReadOnlyList<JobRow>? lastKnownGood = null);

    /// <summary>
    /// Check if the source has been modified since the given time.
    /// Used for efficient polling.
    /// </summary>
    bool HasChangedSince(DateTime utcTime);

    /// <summary>
    /// Get the full path for a file reference.
    /// </summary>
    string GetFullPath(FileRef file);

    /// <summary>
    /// Delete a job and optionally its files.
    /// </summary>
    /// <param name="job">The job to delete</param>
    /// <param name="deleteFiles">Whether to also delete the actual files</param>
    /// <returns>True if deletion was successful</returns>
    bool DeleteJob(JobRow job, bool deleteFiles);
}

/// <summary>
/// Factory for creating gallery sources from paths or URIs.
/// </summary>
public interface IGallerySourceFactory
{
    /// <summary>
    /// Detect the appropriate source type for a given path.
    /// </summary>
    SourceType DetectSourceType(string path);

    /// <summary>
    /// Create a gallery source for the given path.
    /// </summary>
    IGallerySource CreateSource(string path, SourceType? forceType = null);
}

/// <summary>
/// Types of gallery sources.
/// </summary>
public enum SourceType
{
    /// <summary>Plain folder with images/videos - no metadata index.</summary>
    Folder,

    /// <summary>CodeComfy workspace with .codecomfy/outputs/index.json.</summary>
    CodeComfy,

    /// <summary>ComfyUI native output folder.</summary>
    ComfyUI,

    /// <summary>Automatic1111/Forge WebUI outputs.</summary>
    A1111,

    /// <summary>Fooocus output folder.</summary>
    Fooocus,

    /// <summary>Custom index format.</summary>
    Custom
}
