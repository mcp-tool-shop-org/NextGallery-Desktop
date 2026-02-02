using Gallery.Domain.Index;

namespace Gallery.Domain.Sources;

/// <summary>
/// Gallery source for CodeComfy workspaces.
/// Reads from .codecomfy/outputs/index.json.
/// </summary>
public sealed class CodeComfyGallerySource : IGallerySource
{
    private readonly string _workspaceRoot;
    private readonly string _outputsPath;
    private readonly string _indexPath;
    private readonly IndexLoader _indexLoader;
    private readonly IFileReader _fileReader;
    private bool _disposed;

    public string SourceName => "CodeComfy Workspace";
    public string RootPath => _workspaceRoot;
    public string OutputsPath => _outputsPath;
    public bool SupportsPolling => true;
    public bool SupportsDeletion => true;

    public CodeComfyGallerySource(string workspaceRoot, IFileReader? fileReader = null)
    {
        _workspaceRoot = workspaceRoot ?? throw new ArgumentNullException(nameof(workspaceRoot));
        _fileReader = fileReader ?? RealFileReader.Instance;
        _indexLoader = new IndexLoader(_fileReader);
        _outputsPath = Path.Combine(_workspaceRoot, ".codecomfy", "outputs");
        _indexPath = Path.Combine(_outputsPath, "index.json");
    }

    public IndexLoadResult Load(IReadOnlyList<JobRow>? lastKnownGood = null)
    {
        return _indexLoader.Load(_workspaceRoot, lastKnownGood);
    }

    public bool HasChangedSince(DateTime utcTime)
    {
        try
        {
            if (!_fileReader.FileExists(_indexPath))
                return false;

            return _fileReader.GetLastWriteTimeUtc(_indexPath) > utcTime;
        }
        catch
        {
            return false;
        }
    }

    public string GetFullPath(FileRef file)
    {
        return Path.Combine(_outputsPath, file.RelativePath);
    }

    public bool DeleteJob(JobRow job, bool deleteFiles)
    {
        try
        {
            if (deleteFiles)
            {
                foreach (var file in job.Files)
                {
                    var fullPath = GetFullPath(file);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
            }

            // Note: We don't modify index.json here - that's managed by CodeComfy extension
            // The job will reappear on next index rebuild if we only removed from UI
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
