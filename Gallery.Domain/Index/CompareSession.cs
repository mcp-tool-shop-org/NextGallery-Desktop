namespace Gallery.Domain.Index;

/// <summary>
/// Represents a comparison session between two jobs.
/// 2026 feature - industry standard for AI gallery tools.
/// </summary>
public sealed class CompareSession
{
    public required JobRow Left { get; init; }
    public required JobRow Right { get; init; }
    public CompareViewMode ViewMode { get; set; } = CompareViewMode.SideBySide;

    /// <summary>
    /// Get all parameter differences between the two jobs.
    /// </summary>
    public IReadOnlyList<ParameterDiff> GetDiffs()
    {
        var diffs = new List<ParameterDiff>();

        // Compare seed
        AddDiff(diffs, "Seed", Left.Seed.ToString(), Right.Seed.ToString());

        // Compare prompt
        AddDiff(diffs, "Prompt", Left.Prompt, Right.Prompt);

        // Compare negative prompt
        AddDiff(diffs, "Negative Prompt", Left.NegativePrompt ?? "", Right.NegativePrompt ?? "");

        // Compare preset
        AddDiff(diffs, "Preset", Left.PresetId, Right.PresetId);

        // Compare elapsed time
        AddDiff(diffs, "Time (s)",
            Left.ElapsedSeconds?.ToString("F1") ?? "N/A",
            Right.ElapsedSeconds?.ToString("F1") ?? "N/A");

        // Compare file count
        AddDiff(diffs, "Files", Left.Files.Count.ToString(), Right.Files.Count.ToString());

        // Compare dimensions (first file)
        var leftFile = Left.Files.FirstOrDefault();
        var rightFile = Right.Files.FirstOrDefault();
        if (leftFile != null || rightFile != null)
        {
            var leftDim = leftFile != null && leftFile.Width.HasValue && leftFile.Height.HasValue
                ? $"{leftFile.Width}x{leftFile.Height}"
                : "N/A";
            var rightDim = rightFile != null && rightFile.Width.HasValue && rightFile.Height.HasValue
                ? $"{rightFile.Width}x{rightFile.Height}"
                : "N/A";
            AddDiff(diffs, "Dimensions", leftDim, rightDim);
        }

        return diffs;
    }

    /// <summary>
    /// Get a summary of what changed between the jobs.
    /// </summary>
    public string GetChangeSummary()
    {
        var diffs = GetDiffs();
        var changedParams = diffs.Where(d => d.IsDifferent).Select(d => d.Parameter).ToList();

        if (changedParams.Count == 0)
            return "No differences found";

        if (changedParams.Count == 1)
            return $"Changed: {changedParams[0]}";

        if (changedParams.Count <= 3)
            return $"Changed: {string.Join(", ", changedParams)}";

        return $"Changed: {changedParams.Count} parameters";
    }

    private static void AddDiff(List<ParameterDiff> diffs, string name, string? left, string? right)
    {
        diffs.Add(new ParameterDiff(name, left, right));
    }
}

/// <summary>
/// View modes for comparison.
/// </summary>
public enum CompareViewMode
{
    /// <summary>Left and right side by side.</summary>
    SideBySide,

    /// <summary>Overlay with blend slider.</summary>
    Overlay,

    /// <summary>Show only the diff table.</summary>
    DiffOnly
}

/// <summary>
/// Single parameter difference.
/// </summary>
public sealed class ParameterDiff
{
    public string Parameter { get; }
    public string? LeftValue { get; }
    public string? RightValue { get; }
    public bool IsDifferent { get; }

    public ParameterDiff(string parameter, string? leftValue, string? rightValue)
    {
        Parameter = parameter;
        LeftValue = leftValue;
        RightValue = rightValue;
        IsDifferent = !string.Equals(leftValue, rightValue, StringComparison.Ordinal);
    }
}
