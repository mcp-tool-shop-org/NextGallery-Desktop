using System.Security.Cryptography;
using System.Text;
using Gallery.Domain.Index;

namespace Gallery.Domain.Sources;

/// <summary>
/// Gallery source that scans a folder for images and videos.
/// No index file required - discovers files by scanning directory.
/// </summary>
public sealed class FolderGallerySource : IGallerySource
{
    private readonly string _rootPath;
    private readonly IFileReader _fileReader;
    private DateTime _lastScanTime;
    private bool _disposed;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".gif", ".tiff", ".tif"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".mov", ".avi", ".mkv"
    };

    public string SourceName => "Local Folder";
    public string RootPath => _rootPath;
    public string OutputsPath => _rootPath;
    public bool SupportsPolling => true;
    public bool SupportsDeletion => true;

    public FolderGallerySource(string rootPath, IFileReader? fileReader = null)
    {
        _rootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
        _fileReader = fileReader ?? RealFileReader.Instance;
    }

    public IndexLoadResult Load(IReadOnlyList<JobRow>? lastKnownGood = null)
    {
        // Validate root path
        if (!_fileReader.PathExists(_rootPath))
        {
            return new IndexLoadResult
            {
                State = new GalleryState.Fatal
                {
                    Message = $"Folder not found: {_rootPath}",
                    Reason = FatalReason.WorkspaceNotFound
                }
            };
        }

        if (!_fileReader.DirectoryExists(_rootPath))
        {
            return new IndexLoadResult
            {
                State = new GalleryState.Fatal
                {
                    Message = $"Path is not a folder: {_rootPath}",
                    Reason = FatalReason.WorkspaceNotDirectory
                }
            };
        }

        try
        {
            var items = ScanFolder(_rootPath);
            _lastScanTime = DateTime.UtcNow;

            if (items.Count == 0)
            {
                return new IndexLoadResult
                {
                    State = new GalleryState.Empty
                    {
                        Title = "No images found",
                        Subtitle = "Add images to this folder to see them here."
                    }
                };
            }

            // Sort by creation date, newest first
            var sorted = items.OrderByDescending(j => j.CreatedAt).ToList();

            return new IndexLoadResult
            {
                State = new GalleryState.List { Items = sorted },
                LastKnownGood = sorted
            };
        }
        catch (UnauthorizedAccessException)
        {
            return WithWarningOrLastKnown("Permission denied accessing folder", lastKnownGood);
        }
        catch (IOException ex)
        {
            return WithWarningOrLastKnown($"Error scanning folder: {ex.Message}", lastKnownGood);
        }
    }

    private List<JobRow> ScanFolder(string path)
    {
        var items = new List<JobRow>();
        var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);

        foreach (var filePath in files)
        {
            var ext = Path.GetExtension(filePath);
            JobKind? kind = null;

            if (ImageExtensions.Contains(ext))
                kind = JobKind.Image;
            else if (VideoExtensions.Contains(ext))
                kind = JobKind.Video;

            if (kind == null) continue;

            try
            {
                var fileInfo = new FileInfo(filePath);
                var relativePath = Path.GetRelativePath(_rootPath, filePath);
                var jobId = GenerateJobId(relativePath);

                var fileRef = new FileRef
                {
                    RelativePath = relativePath,
                    Sha256 = jobId, // Use path hash as pseudo-hash
                    SizeBytes = fileInfo.Length
                };

                // Try to extract metadata from filename or file
                var (prompt, seed) = ExtractMetadataFromPath(filePath);

                var job = new JobRow
                {
                    JobId = jobId,
                    CreatedAt = fileInfo.CreationTimeUtc,
                    Kind = kind.Value,
                    Files = new[] { fileRef },
                    Seed = seed,
                    Prompt = prompt ?? Path.GetFileNameWithoutExtension(filePath),
                    PresetId = "unknown"
                };

                items.Add(job);
            }
            catch
            {
                // Skip files we can't access
            }
        }

        return items;
    }

    private static string GenerateJobId(string relativePath)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(relativePath));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static (string? prompt, long seed) ExtractMetadataFromPath(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        // Try to extract seed from common patterns:
        // - "image_12345.png" -> seed 12345
        // - "00001-12345.png" -> seed 12345
        // - "a cat sitting-seed_12345.png" -> seed 12345

        long seed = 0;
        string? prompt = null;

        // Pattern: ends with _<number> or -<number>
        var match = System.Text.RegularExpressions.Regex.Match(
            fileName,
            @"[-_](\d{4,})$");

        if (match.Success && long.TryParse(match.Groups[1].Value, out var parsedSeed))
        {
            seed = parsedSeed;
            prompt = fileName.Substring(0, match.Index).Trim('-', '_', ' ');
        }

        // Pattern: starts with <number>-
        if (seed == 0)
        {
            match = System.Text.RegularExpressions.Regex.Match(
                fileName,
                @"^\d+-(\d+)[-_]?(.*)$");

            if (match.Success && long.TryParse(match.Groups[1].Value, out parsedSeed))
            {
                seed = parsedSeed;
                prompt = match.Groups[2].Value.Trim('-', '_', ' ');
            }
        }

        // Generate random seed if not found
        if (seed == 0)
        {
            seed = Math.Abs(fileName.GetHashCode());
        }

        return (string.IsNullOrWhiteSpace(prompt) ? null : prompt, seed);
    }

    public bool HasChangedSince(DateTime utcTime)
    {
        try
        {
            var dirInfo = new DirectoryInfo(_rootPath);
            return dirInfo.LastWriteTimeUtc > utcTime;
        }
        catch
        {
            return false;
        }
    }

    public string GetFullPath(FileRef file)
    {
        return Path.Combine(_rootPath, file.RelativePath);
    }

    public bool DeleteJob(JobRow job, bool deleteFiles)
    {
        if (!deleteFiles) return true; // Nothing to do

        try
        {
            foreach (var file in job.Files)
            {
                var fullPath = GetFullPath(file);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IndexLoadResult WithWarningOrLastKnown(
        string message,
        IReadOnlyList<JobRow>? lastKnownGood)
    {
        if (lastKnownGood != null && lastKnownGood.Count > 0)
        {
            return new IndexLoadResult
            {
                State = new GalleryState.List { Items = lastKnownGood },
                Banner = BannerInfo.Warning(message),
                LastKnownGood = lastKnownGood
            };
        }

        return new IndexLoadResult
        {
            State = new GalleryState.Empty(),
            Banner = BannerInfo.Warning(message)
        };
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
