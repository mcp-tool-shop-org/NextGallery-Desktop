using System.Globalization;
using Gallery.Domain.Index;
using Gallery.Domain.Sources;

namespace Gallery.App.Converters;

/// <summary>
/// Converts a JobRow to a thumbnail ImageSource.
/// Uses the ConverterParameter to pass the IGallerySource for path resolution.
/// </summary>
public class JobRowToThumbnailConverter : IValueConverter
{
    /// <summary>
    /// Static source reference set by the page for path resolution.
    /// </summary>
    public static IGallerySource? CurrentSource { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            if (value is not JobRow job)
                return null;

            if (CurrentSource == null)
                return null;

            if (job.Files == null || job.Files.Count == 0)
                return null;

            var firstFile = job.Files.FirstOrDefault();
            if (firstFile == null || string.IsNullOrEmpty(firstFile.RelativePath))
                return null;

            var fullPath = CurrentSource.GetFullPath(firstFile);
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                return null;

            // Only load images, not videos
            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            if (ext == ".mp4" || ext == ".webm" || ext == ".mov" || ext == ".avi" || ext == ".mkv")
                return null; // Don't try to load video as image

            return ImageSource.FromFile(fullPath);
        }
        catch
        {
            // Ignore all errors, return null for fallback display
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
