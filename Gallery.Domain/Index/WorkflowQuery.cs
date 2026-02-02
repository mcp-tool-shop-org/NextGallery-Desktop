namespace Gallery.Domain.Index;

/// <summary>
/// Filter criteria for searching jobs by workflow parameters.
/// 2026 feature - industry standard for AI gallery tools.
/// </summary>
public sealed class WorkflowQuery
{
    /// <summary>Search text within prompt.</summary>
    public string? PromptContains { get; set; }

    /// <summary>Search text within negative prompt.</summary>
    public string? NegativePromptContains { get; set; }

    /// <summary>Filter by preset/model ID.</summary>
    public string? PresetId { get; set; }

    /// <summary>Exact seed match.</summary>
    public long? Seed { get; set; }

    /// <summary>Filter by job kind (image/video).</summary>
    public JobKind? Kind { get; set; }

    /// <summary>Only show favorited jobs.</summary>
    public bool? IsFavorite { get; set; }

    /// <summary>Minimum file count.</summary>
    public int? MinFileCount { get; set; }

    /// <summary>Created after this date.</summary>
    public DateTimeOffset? CreatedAfter { get; set; }

    /// <summary>Created before this date.</summary>
    public DateTimeOffset? CreatedBefore { get; set; }

    /// <summary>
    /// Check if a job matches this query.
    /// </summary>
    public bool Matches(JobRow job)
    {
        // Prompt contains
        if (!string.IsNullOrWhiteSpace(PromptContains))
        {
            if (string.IsNullOrWhiteSpace(job.Prompt) ||
                !job.Prompt.Contains(PromptContains, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Negative prompt contains
        if (!string.IsNullOrWhiteSpace(NegativePromptContains))
        {
            if (string.IsNullOrWhiteSpace(job.NegativePrompt) ||
                !job.NegativePrompt.Contains(NegativePromptContains, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Preset filter
        if (!string.IsNullOrWhiteSpace(PresetId))
        {
            if (!string.Equals(job.PresetId, PresetId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Exact seed match
        if (Seed.HasValue && job.Seed != Seed.Value)
        {
            return false;
        }

        // Job kind
        if (Kind.HasValue && job.Kind != Kind.Value)
        {
            return false;
        }

        // Favorite filter
        if (IsFavorite.HasValue && job.Favorite != IsFavorite.Value)
        {
            return false;
        }

        // Min file count
        if (MinFileCount.HasValue && job.Files.Count < MinFileCount.Value)
        {
            return false;
        }

        // Date range
        if (CreatedAfter.HasValue && job.CreatedAt < CreatedAfter.Value)
        {
            return false;
        }

        if (CreatedBefore.HasValue && job.CreatedAt > CreatedBefore.Value)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Check if any filter is active.
    /// </summary>
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(PromptContains) ||
        !string.IsNullOrWhiteSpace(NegativePromptContains) ||
        !string.IsNullOrWhiteSpace(PresetId) ||
        Seed.HasValue ||
        Kind.HasValue ||
        IsFavorite.HasValue ||
        MinFileCount.HasValue ||
        CreatedAfter.HasValue ||
        CreatedBefore.HasValue;

    /// <summary>
    /// Clear all filters.
    /// </summary>
    public void Clear()
    {
        PromptContains = null;
        NegativePromptContains = null;
        PresetId = null;
        Seed = null;
        Kind = null;
        IsFavorite = null;
        MinFileCount = null;
        CreatedAfter = null;
        CreatedBefore = null;
    }

    /// <summary>
    /// Get a human-readable summary of active filters.
    /// </summary>
    public string GetFilterSummary()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(PromptContains))
            parts.Add($"prompt:\"{PromptContains}\"");

        if (!string.IsNullOrWhiteSpace(NegativePromptContains))
            parts.Add($"neg:\"{NegativePromptContains}\"");

        if (!string.IsNullOrWhiteSpace(PresetId))
            parts.Add($"preset:{PresetId}");

        if (Seed.HasValue)
            parts.Add($"seed:{Seed}");

        if (Kind.HasValue)
            parts.Add($"kind:{Kind}");

        if (IsFavorite == true)
            parts.Add("★ favorites");

        if (parts.Count == 0)
            return "No filters active";

        return string.Join(" + ", parts);
    }
}
