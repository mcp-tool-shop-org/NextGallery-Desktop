namespace Gallery.Domain.Models;

/// <summary>
/// A group of media items for timeline display.
/// </summary>
public sealed record MediaGroup
{
    /// <summary>
    /// The grouping key (e.g., "2026-06-15" for Day, "2026-06" for Month).
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Human-readable title (e.g., "June 15, 2026" or "June 2026").
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Items in this group, ordered by the query's sort settings.
    /// </summary>
    public required IReadOnlyList<MediaItem> Items { get; init; }

    /// <summary>
    /// Number of items in this group.
    /// </summary>
    public int Count => Items.Count;
}
