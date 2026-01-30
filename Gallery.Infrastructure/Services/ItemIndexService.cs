using Gallery.Application.Interfaces;
using Gallery.Domain.Enums;
using Gallery.Domain.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace Gallery.Infrastructure.Services;

/// <summary>
/// Scans folders and indexes media files.
/// </summary>
public sealed class ItemIndexService : IItemIndexService
{
    private readonly ILibraryStore _libraryStore;
    private readonly IMediaItemStore _itemStore;
    private readonly IThumbJobStore _jobStore;
    private readonly IThumbGenerator _thumbGenerator;

    public ItemIndexService(
        ILibraryStore libraryStore,
        IMediaItemStore itemStore,
        IThumbJobStore jobStore,
        IThumbGenerator thumbGenerator)
    {
        _libraryStore = libraryStore;
        _itemStore = itemStore;
        _jobStore = jobStore;
        _thumbGenerator = thumbGenerator;
    }

    public async Task<int> ScanFolderAsync(LibraryFolder folder, IProgress<ScanProgress>? progress = null, CancellationToken ct = default)
    {
        if (!System.IO.Directory.Exists(folder.Path))
        {
            return 0;
        }

        var files = EnumerateMediaFiles(folder.Path).ToList();
        var total = files.Count;
        var indexed = 0;

        foreach (var filePath in files)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var item = await IndexFileAsync(filePath, ct);
                if (item is not null)
                {
                    indexed++;

                    // Enqueue thumbnail jobs
                    await _jobStore.EnqueueAsync(item.Id, ThumbSize.Small, priority: 10, ct);
                    await _jobStore.EnqueueAsync(item.Id, ThumbSize.Large, priority: 1, ct);
                }
            }
            catch (Exception ex)
            {
                // Log but continue
                System.Diagnostics.Debug.WriteLine($"Failed to index {filePath}: {ex.Message}");
            }

            progress?.Report(new ScanProgress(filePath, indexed, total));
        }

        await _libraryStore.UpdateLastScannedAsync(folder.Id, DateTimeOffset.UtcNow, ct);
        return indexed;
    }

    public async Task<int> ScanAllAsync(IProgress<ScanProgress>? progress = null, CancellationToken ct = default)
    {
        var folders = await _libraryStore.GetAllAsync(ct);
        var total = 0;

        foreach (var folder in folders.Where(f => f.IsEnabled))
        {
            total += await ScanFolderAsync(folder, progress, ct);
        }

        return total;
    }

    private async Task<MediaItem?> IndexFileAsync(string filePath, CancellationToken ct)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            return null;
        }

        var extension = fileInfo.Extension.ToLowerInvariant();
        var type = GetMediaType(extension);

        // Try to extract metadata
        int? width = null, height = null;
        DateTimeOffset? takenAt = null;
        TimeSpan? duration = null;

        if (type == MediaType.Image)
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);

                var exifIfd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                var exifSubIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

                // Try to get dimensions
                if (exifIfd0?.TryGetInt32(ExifDirectoryBase.TagImageWidth, out var w) == true)
                    width = w;
                if (exifIfd0?.TryGetInt32(ExifDirectoryBase.TagImageHeight, out var h) == true)
                    height = h;

                // Try to get date taken
                if (exifSubIfd?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dt) == true)
                    takenAt = new DateTimeOffset(dt, TimeSpan.Zero);
            }
            catch
            {
                // Metadata extraction failed, continue without
            }
        }
        else if (type == MediaType.Video)
        {
            // Video metadata - MetadataExtractor can read some video formats
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);

                // Try to extract video dimensions and duration from QuickTime/MP4 metadata
                foreach (var dir in directories)
                {
                    if (dir.Name.Contains("QuickTime", StringComparison.OrdinalIgnoreCase) ||
                        dir.Name.Contains("MP4", StringComparison.OrdinalIgnoreCase))
                    {
                        // Look for common video metadata tags
                        foreach (var tag in dir.Tags)
                        {
                            if (tag.Name.Contains("Width", StringComparison.OrdinalIgnoreCase) && width is null)
                            {
                                if (int.TryParse(tag.Description, out var w))
                                    width = w;
                            }
                            if (tag.Name.Contains("Height", StringComparison.OrdinalIgnoreCase) && height is null)
                            {
                                if (int.TryParse(tag.Description, out var h))
                                    height = h;
                            }
                            if (tag.Name.Contains("Duration", StringComparison.OrdinalIgnoreCase) && duration is null)
                            {
                                // Duration might be in various formats
                                if (TryParseDuration(tag.Description, out var d))
                                    duration = d;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Video metadata extraction failed, continue without
            }
        }

        var item = new MediaItem
        {
            Path = filePath,
            Extension = extension,
            Type = type,
            SizeBytes = fileInfo.Length,
            ModifiedAt = new DateTimeOffset(fileInfo.LastWriteTimeUtc, TimeSpan.Zero),
            TakenAt = takenAt,
            Width = width,
            Height = height,
            Duration = duration,
            LastIndexedAt = DateTimeOffset.UtcNow
        };

        var id = await _itemStore.UpsertAsync(item, ct);
        return item with { Id = id };
    }

    private static bool TryParseDuration(string? description, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(description))
            return false;

        // Try to parse common duration formats
        // Format: "0:00:30" or "00:30" or "30.5 sec" or "1234 ms"
        description = description.Trim();

        // Try TimeSpan.TryParse first
        if (TimeSpan.TryParse(description, out duration))
            return true;

        // Try seconds format (e.g., "30.5 sec" or "30.5s")
        if (description.EndsWith("sec", StringComparison.OrdinalIgnoreCase) ||
            description.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            var numPart = description.TrimEnd('s', 'S', 'e', 'E', 'c', 'C', ' ');
            if (double.TryParse(numPart, out var seconds))
            {
                duration = TimeSpan.FromSeconds(seconds);
                return true;
            }
        }

        // Try milliseconds format
        if (description.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
        {
            var numPart = description[..^2].Trim();
            if (double.TryParse(numPart, out var ms))
            {
                duration = TimeSpan.FromMilliseconds(ms);
                return true;
            }
        }

        // Try plain number (assume seconds)
        if (double.TryParse(description, out var plainSeconds))
        {
            duration = TimeSpan.FromSeconds(plainSeconds);
            return true;
        }

        return false;
    }

    private IEnumerable<string> EnumerateMediaFiles(string folderPath)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true
        };

        return System.IO.Directory.EnumerateFiles(folderPath, "*", options)
            .Where(f => _thumbGenerator.SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
    }

    private static MediaType GetMediaType(string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".tiff" or ".tif" or ".heic" => MediaType.Image,
            ".mp4" or ".mov" or ".avi" or ".mkv" or ".webm" => MediaType.Video,
            _ => MediaType.Unknown
        };
    }
}
