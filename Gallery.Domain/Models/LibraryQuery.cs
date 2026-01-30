namespace Gallery.Domain.Models;

public enum MediaTypeFilter { All = 0, Images = 1, Videos = 2 }
public enum SortField { TakenAt = 0, ModifiedAt = 1, Size = 2, Name = 3 }
public enum SortDir { Desc = 0, Asc = 1 }

/// <summary>
/// How to group items in the grid.
/// </summary>
public enum GroupBy
{
    /// <summary>No grouping - flat list.</summary>
    None = 0,
    /// <summary>Group by day (YYYY-MM-DD).</summary>
    Day = 1,
    /// <summary>Group by month (YYYY-MM).</summary>
    Month = 2
}

/// <summary>
/// Represents a query against the media library.
/// Single source of truth for what the grid displays.
/// </summary>
public sealed record LibraryQuery(
    string Text,
    MediaTypeFilter MediaType,
    bool FavoritesOnly,
    SortField SortBy,
    SortDir SortDir,
    GroupBy GroupBy = GroupBy.None
)
{
    public static LibraryQuery Default => new(
        Text: "",
        MediaType: MediaTypeFilter.All,
        FavoritesOnly: false,
        SortBy: SortField.ModifiedAt,
        SortDir: SortDir.Desc,
        GroupBy: GroupBy.None
    );

    /// <summary>
    /// Returns true if this query has any active filters.
    /// </summary>
    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Text) ||
        MediaType != MediaTypeFilter.All ||
        FavoritesOnly;

    /// <summary>
    /// Returns true if grouping is enabled.
    /// </summary>
    public bool IsGrouped => GroupBy != GroupBy.None;
}
