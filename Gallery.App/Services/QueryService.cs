using Gallery.Domain.Models;

namespace Gallery.App.Services;

/// <summary>
/// Single source of truth for library query state.
/// Grid, count display, and selection all listen to this.
/// </summary>
public sealed class QueryService
{
    public event Action<LibraryQuery>? QueryChanged;

    public LibraryQuery Current { get; private set; } = LibraryQuery.Default;

    /// <summary>
    /// Replace the entire query.
    /// </summary>
    public void Set(LibraryQuery query)
    {
        if (query == Current) return;
        Current = query;
        QueryChanged?.Invoke(query);
    }

    /// <summary>
    /// Modify the current query.
    /// </summary>
    public void Patch(Func<LibraryQuery, LibraryQuery> mutator)
    {
        Set(mutator(Current));
    }

    /// <summary>
    /// Reset to default query.
    /// </summary>
    public void Reset()
    {
        Set(LibraryQuery.Default);
    }

    /// <summary>
    /// Update just the search text.
    /// </summary>
    public void SetSearchText(string text)
    {
        Patch(q => q with { Text = text });
    }

    /// <summary>
    /// Toggle favorites filter.
    /// </summary>
    public void ToggleFavorites()
    {
        Patch(q => q with { FavoritesOnly = !q.FavoritesOnly });
    }

    /// <summary>
    /// Set media type filter.
    /// </summary>
    public void SetMediaType(MediaTypeFilter mediaType)
    {
        Patch(q => q with { MediaType = mediaType });
    }

    /// <summary>
    /// Set sort field and direction.
    /// </summary>
    public void SetSort(SortField field, SortDir direction)
    {
        Patch(q => q with { SortBy = field, SortDir = direction });
    }

    /// <summary>
    /// Set grouping mode.
    /// </summary>
    public void SetGroupBy(GroupBy groupBy)
    {
        Patch(q => q with { GroupBy = groupBy });
    }

    /// <summary>
    /// Cycle through grouping modes: None -> Day -> Month -> None
    /// </summary>
    public void CycleGroupBy()
    {
        var next = Current.GroupBy switch
        {
            GroupBy.None => GroupBy.Day,
            GroupBy.Day => GroupBy.Month,
            GroupBy.Month => GroupBy.None,
            _ => GroupBy.None
        };
        SetGroupBy(next);
    }
}
