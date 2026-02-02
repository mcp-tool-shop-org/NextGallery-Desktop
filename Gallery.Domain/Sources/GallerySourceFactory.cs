using Gallery.Domain.Index;

namespace Gallery.Domain.Sources;

/// <summary>
/// Factory for creating gallery sources based on path analysis.
/// </summary>
public sealed class GallerySourceFactory : IGallerySourceFactory
{
    private readonly IFileReader _fileReader;

    public GallerySourceFactory(IFileReader? fileReader = null)
    {
        _fileReader = fileReader ?? RealFileReader.Instance;
    }

    public SourceType DetectSourceType(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return SourceType.Folder;

        // Check for CodeComfy workspace
        var codecomfyIndex = Path.Combine(path, ".codecomfy", "outputs", "index.json");
        if (_fileReader.FileExists(codecomfyIndex))
            return SourceType.CodeComfy;

        // Check for ComfyUI output folder
        // ComfyUI typically has workflow JSON files alongside images
        var comfyWorkflow = Path.Combine(path, "workflow.json");
        if (_fileReader.FileExists(comfyWorkflow))
            return SourceType.ComfyUI;

        // Check for A1111/Forge output pattern
        // A1111 creates txt files with generation params
        var a1111Pattern = Directory.Exists(path) &&
            Directory.EnumerateFiles(path, "*.txt", SearchOption.TopDirectoryOnly)
                .Any(f => Path.GetFileName(f).StartsWith("00"));
        if (a1111Pattern)
            return SourceType.A1111;

        // Check for Fooocus outputs
        var fooocusMarker = Path.Combine(path, ".fooocus-version");
        if (_fileReader.FileExists(fooocusMarker))
            return SourceType.Fooocus;

        // Default to folder scanning
        return SourceType.Folder;
    }

    public IGallerySource CreateSource(string path, SourceType? forceType = null)
    {
        var sourceType = forceType ?? DetectSourceType(path);

        return sourceType switch
        {
            SourceType.CodeComfy => new CodeComfyGallerySource(path, _fileReader),
            SourceType.ComfyUI => new ComfyUIGallerySource(path, _fileReader),
            SourceType.A1111 => new A1111GallerySource(path, _fileReader),
            SourceType.Fooocus => new FooocusGallerySource(path, _fileReader),
            _ => new FolderGallerySource(path, _fileReader)
        };
    }
}

/// <summary>
/// Gallery source for ComfyUI native output folders.
/// Reads workflow.json files for metadata.
/// Uses composition with FolderGallerySource for base scanning.
/// </summary>
public sealed class ComfyUIGallerySource : IGallerySource
{
    private readonly FolderGallerySource _folderSource;

    public ComfyUIGallerySource(string rootPath, IFileReader? fileReader = null)
    {
        _folderSource = new FolderGallerySource(rootPath, fileReader);
    }

    public string SourceName => "ComfyUI";
    public string RootPath => _folderSource.RootPath;
    public string OutputsPath => _folderSource.OutputsPath;
    public bool SupportsPolling => _folderSource.SupportsPolling;
    public bool SupportsDeletion => _folderSource.SupportsDeletion;

    public IndexLoadResult Load(IReadOnlyList<JobRow>? lastKnownGood = null)
    {
        // TODO: Parse ComfyUI workflow.json files for richer metadata
        // For now, delegate to folder scanning
        return _folderSource.Load(lastKnownGood);
    }

    public bool HasChangedSince(DateTime utcTime) => _folderSource.HasChangedSince(utcTime);
    public string GetFullPath(FileRef file) => _folderSource.GetFullPath(file);
    public bool DeleteJob(JobRow job, bool deleteFiles) => _folderSource.DeleteJob(job, deleteFiles);
    public void Dispose() => _folderSource.Dispose();
}

/// <summary>
/// Gallery source for Automatic1111/Forge WebUI outputs.
/// Reads .txt sidecar files for generation parameters.
/// Uses composition with FolderGallerySource for base scanning.
/// </summary>
public sealed class A1111GallerySource : IGallerySource
{
    private readonly FolderGallerySource _folderSource;

    public A1111GallerySource(string rootPath, IFileReader? fileReader = null)
    {
        _folderSource = new FolderGallerySource(rootPath, fileReader);
    }

    public string SourceName => "A1111/Forge";
    public string RootPath => _folderSource.RootPath;
    public string OutputsPath => _folderSource.OutputsPath;
    public bool SupportsPolling => _folderSource.SupportsPolling;
    public bool SupportsDeletion => _folderSource.SupportsDeletion;

    public IndexLoadResult Load(IReadOnlyList<JobRow>? lastKnownGood = null)
    {
        // TODO: Parse A1111 .txt sidecar files for generation parameters
        // For now, delegate to folder scanning
        return _folderSource.Load(lastKnownGood);
    }

    public bool HasChangedSince(DateTime utcTime) => _folderSource.HasChangedSince(utcTime);
    public string GetFullPath(FileRef file) => _folderSource.GetFullPath(file);
    public bool DeleteJob(JobRow job, bool deleteFiles) => _folderSource.DeleteJob(job, deleteFiles);
    public void Dispose() => _folderSource.Dispose();
}

/// <summary>
/// Gallery source for Fooocus output folders.
/// Uses composition with FolderGallerySource for base scanning.
/// </summary>
public sealed class FooocusGallerySource : IGallerySource
{
    private readonly FolderGallerySource _folderSource;

    public FooocusGallerySource(string rootPath, IFileReader? fileReader = null)
    {
        _folderSource = new FolderGallerySource(rootPath, fileReader);
    }

    public string SourceName => "Fooocus";
    public string RootPath => _folderSource.RootPath;
    public string OutputsPath => _folderSource.OutputsPath;
    public bool SupportsPolling => _folderSource.SupportsPolling;
    public bool SupportsDeletion => _folderSource.SupportsDeletion;

    public IndexLoadResult Load(IReadOnlyList<JobRow>? lastKnownGood = null)
    {
        // TODO: Parse Fooocus metadata for richer information
        // For now, delegate to folder scanning
        return _folderSource.Load(lastKnownGood);
    }

    public bool HasChangedSince(DateTime utcTime) => _folderSource.HasChangedSince(utcTime);
    public string GetFullPath(FileRef file) => _folderSource.GetFullPath(file);
    public bool DeleteJob(JobRow job, bool deleteFiles) => _folderSource.DeleteJob(job, deleteFiles);
    public void Dispose() => _folderSource.Dispose();
}
