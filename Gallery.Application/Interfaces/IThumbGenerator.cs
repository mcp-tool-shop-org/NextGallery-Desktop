namespace Gallery.Application.Interfaces;

/// <summary>
/// Generates thumbnail images from source files.
/// </summary>
public interface IThumbGenerator
{
    /// <summary>
    /// Generate a JPEG thumbnail from a source image.
    /// </summary>
    /// <param name="sourcePath">Path to source image</param>
    /// <param name="maxPixels">Maximum dimension (width or height)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>JPEG bytes</returns>
    Task<byte[]> GenerateImageThumbAsync(string sourcePath, int maxPixels, CancellationToken ct = default);

    /// <summary>
    /// Supported image extensions (lowercase, with dot).
    /// </summary>
    IReadOnlySet<string> SupportedImageExtensions { get; }

    /// <summary>
    /// Supported video extensions (lowercase, with dot).
    /// Videos are indexed but may not have thumbnails until FFmpeg plugin is available.
    /// </summary>
    IReadOnlySet<string> SupportedVideoExtensions { get; }

    /// <summary>
    /// All supported media extensions (images + videos).
    /// </summary>
    IReadOnlySet<string> SupportedExtensions { get; }
}
