using Gallery.Domain.Index;
using NUnit.Framework;

namespace Gallery.Tests;

/// <summary>
/// Tests for 2026 gallery features: Compare Mode and Workflow Search.
/// </summary>
[TestFixture]
public class CodeComfy2026FeatureTests
{
    #region CompareSession Tests

    [Test]
    public void CompareSession_GetDiffs_IdenticalJobs_NoDifferences()
    {
        var job1 = CreateTestJob("job1", seed: 12345, prompt: "a cat");
        var job2 = CreateTestJob("job2", seed: 12345, prompt: "a cat");

        var session = new CompareSession { Left = job1, Right = job2 };
        var diffs = session.GetDiffs();

        // Even identical values are listed, but IsDifferent should be false for matching ones
        Assert.That(diffs.Any(d => d.Parameter == "Seed" && !d.IsDifferent), Is.True);
        Assert.That(diffs.Any(d => d.Parameter == "Prompt" && !d.IsDifferent), Is.True);
    }

    [Test]
    public void CompareSession_GetDiffs_DifferentSeeds_ShowsDifference()
    {
        var job1 = CreateTestJob("job1", seed: 12345, prompt: "a cat");
        var job2 = CreateTestJob("job2", seed: 67890, prompt: "a cat");

        var session = new CompareSession { Left = job1, Right = job2 };
        var diffs = session.GetDiffs();

        var seedDiff = diffs.FirstOrDefault(d => d.Parameter == "Seed");
        Assert.That(seedDiff, Is.Not.Null);
        Assert.That(seedDiff!.IsDifferent, Is.True);
        Assert.That(seedDiff.LeftValue, Is.EqualTo("12345"));
        Assert.That(seedDiff.RightValue, Is.EqualTo("67890"));
    }

    [Test]
    public void CompareSession_GetDiffs_DifferentPrompts_ShowsDifference()
    {
        var job1 = CreateTestJob("job1", seed: 12345, prompt: "a cat");
        var job2 = CreateTestJob("job2", seed: 12345, prompt: "a dog");

        var session = new CompareSession { Left = job1, Right = job2 };
        var diffs = session.GetDiffs();

        var promptDiff = diffs.FirstOrDefault(d => d.Parameter == "Prompt");
        Assert.That(promptDiff, Is.Not.Null);
        Assert.That(promptDiff!.IsDifferent, Is.True);
        Assert.That(promptDiff.LeftValue, Is.EqualTo("a cat"));
        Assert.That(promptDiff.RightValue, Is.EqualTo("a dog"));
    }

    [Test]
    public void CompareSession_GetChangeSummary_NoDifferences()
    {
        var job1 = CreateTestJob("job1", seed: 12345, prompt: "a cat");
        var job2 = CreateTestJob("job2", seed: 12345, prompt: "a cat");

        var session = new CompareSession { Left = job1, Right = job2 };
        var summary = session.GetChangeSummary();

        Assert.That(summary, Is.EqualTo("No differences found"));
    }

    [Test]
    public void CompareSession_GetChangeSummary_SingleDifference()
    {
        var job1 = CreateTestJob("job1", seed: 12345, prompt: "a cat");
        var job2 = CreateTestJob("job2", seed: 67890, prompt: "a cat");

        var session = new CompareSession { Left = job1, Right = job2 };
        var summary = session.GetChangeSummary();

        Assert.That(summary, Does.Contain("Seed"));
    }

    [Test]
    public void CompareSession_GetChangeSummary_MultipleDifferences()
    {
        var job1 = CreateTestJob("job1", seed: 12345, prompt: "a cat");
        var job2 = CreateTestJob("job2", seed: 67890, prompt: "a dog");

        var session = new CompareSession { Left = job1, Right = job2 };
        var summary = session.GetChangeSummary();

        Assert.That(summary, Does.Contain("Seed"));
        Assert.That(summary, Does.Contain("Prompt"));
    }

    [Test]
    public void CompareSession_ViewModes_AllSupported()
    {
        var session = new CompareSession
        {
            Left = CreateTestJob("job1"),
            Right = CreateTestJob("job2")
        };

        session.ViewMode = CompareViewMode.SideBySide;
        Assert.That(session.ViewMode, Is.EqualTo(CompareViewMode.SideBySide));

        session.ViewMode = CompareViewMode.Overlay;
        Assert.That(session.ViewMode, Is.EqualTo(CompareViewMode.Overlay));

        session.ViewMode = CompareViewMode.DiffOnly;
        Assert.That(session.ViewMode, Is.EqualTo(CompareViewMode.DiffOnly));
    }

    #endregion

    #region WorkflowQuery Tests

    [Test]
    public void WorkflowQuery_PromptContains_MatchesSubstring()
    {
        var query = new WorkflowQuery { PromptContains = "cat" };
        var job = CreateTestJob("job1", prompt: "a fluffy cat sitting");

        Assert.That(query.Matches(job), Is.True);
    }

    [Test]
    public void WorkflowQuery_PromptContains_CaseInsensitive()
    {
        var query = new WorkflowQuery { PromptContains = "CAT" };
        var job = CreateTestJob("job1", prompt: "a fluffy cat sitting");

        Assert.That(query.Matches(job), Is.True);
    }

    [Test]
    public void WorkflowQuery_PromptContains_NoMatch()
    {
        var query = new WorkflowQuery { PromptContains = "dog" };
        var job = CreateTestJob("job1", prompt: "a fluffy cat sitting");

        Assert.That(query.Matches(job), Is.False);
    }

    [Test]
    public void WorkflowQuery_SeedMatch_ExactMatch()
    {
        var query = new WorkflowQuery { Seed = 12345 };
        var job = CreateTestJob("job1", seed: 12345);

        Assert.That(query.Matches(job), Is.True);
    }

    [Test]
    public void WorkflowQuery_SeedMatch_NoMatch()
    {
        var query = new WorkflowQuery { Seed = 12345 };
        var job = CreateTestJob("job1", seed: 67890);

        Assert.That(query.Matches(job), Is.False);
    }

    [Test]
    public void WorkflowQuery_PresetFilter_Matches()
    {
        var query = new WorkflowQuery { PresetId = "sdxl-turbo" };
        var job = CreateTestJob("job1", preset: "sdxl-turbo");

        Assert.That(query.Matches(job), Is.True);
    }

    [Test]
    public void WorkflowQuery_PresetFilter_CaseInsensitive()
    {
        var query = new WorkflowQuery { PresetId = "SDXL-TURBO" };
        var job = CreateTestJob("job1", preset: "sdxl-turbo");

        Assert.That(query.Matches(job), Is.True);
    }

    [Test]
    public void WorkflowQuery_FavoriteFilter_Matches()
    {
        var query = new WorkflowQuery { IsFavorite = true };
        var job = CreateTestJob("job1", favorite: true);

        Assert.That(query.Matches(job), Is.True);
    }

    [Test]
    public void WorkflowQuery_FavoriteFilter_NoMatch()
    {
        var query = new WorkflowQuery { IsFavorite = true };
        var job = CreateTestJob("job1", favorite: false);

        Assert.That(query.Matches(job), Is.False);
    }

    [Test]
    public void WorkflowQuery_CombinedFilters_AllMustMatch()
    {
        var query = new WorkflowQuery
        {
            PromptContains = "cat",
            Seed = 12345,
            PresetId = "sdxl"
        };

        var matchingJob = CreateTestJob("job1", seed: 12345, prompt: "a cat", preset: "sdxl");
        var wrongSeed = CreateTestJob("job2", seed: 99999, prompt: "a cat", preset: "sdxl");
        var wrongPrompt = CreateTestJob("job3", seed: 12345, prompt: "a dog", preset: "sdxl");

        Assert.That(query.Matches(matchingJob), Is.True);
        Assert.That(query.Matches(wrongSeed), Is.False);
        Assert.That(query.Matches(wrongPrompt), Is.False);
    }

    [Test]
    public void WorkflowQuery_HasActiveFilters_FalseWhenEmpty()
    {
        var query = new WorkflowQuery();

        Assert.That(query.HasActiveFilters, Is.False);
    }

    [Test]
    public void WorkflowQuery_HasActiveFilters_TrueWithPrompt()
    {
        var query = new WorkflowQuery { PromptContains = "cat" };

        Assert.That(query.HasActiveFilters, Is.True);
    }

    [Test]
    public void WorkflowQuery_HasActiveFilters_TrueWithSeed()
    {
        var query = new WorkflowQuery { Seed = 12345 };

        Assert.That(query.HasActiveFilters, Is.True);
    }

    [Test]
    public void WorkflowQuery_Clear_RemovesAllFilters()
    {
        var query = new WorkflowQuery
        {
            PromptContains = "cat",
            Seed = 12345,
            PresetId = "sdxl",
            IsFavorite = true
        };

        query.Clear();

        Assert.That(query.HasActiveFilters, Is.False);
        Assert.That(query.PromptContains, Is.Null);
        Assert.That(query.Seed, Is.Null);
        Assert.That(query.PresetId, Is.Null);
        Assert.That(query.IsFavorite, Is.Null);
    }

    [Test]
    public void WorkflowQuery_GetFilterSummary_NoFilters()
    {
        var query = new WorkflowQuery();
        var summary = query.GetFilterSummary();

        Assert.That(summary, Is.EqualTo("No filters active"));
    }

    [Test]
    public void WorkflowQuery_GetFilterSummary_WithFilters()
    {
        var query = new WorkflowQuery
        {
            PromptContains = "cat",
            Seed = 12345
        };
        var summary = query.GetFilterSummary();

        Assert.That(summary, Does.Contain("prompt:"));
        Assert.That(summary, Does.Contain("cat"));
        Assert.That(summary, Does.Contain("seed:"));
        Assert.That(summary, Does.Contain("12345"));
    }

    [Test]
    public void WorkflowQuery_DateFilter_CreatedAfter()
    {
        var query = new WorkflowQuery { CreatedAfter = DateTimeOffset.UtcNow.AddDays(-1) };

        var recentJob = CreateTestJob("job1", createdAt: DateTimeOffset.UtcNow);
        var oldJob = CreateTestJob("job2", createdAt: DateTimeOffset.UtcNow.AddDays(-5));

        Assert.That(query.Matches(recentJob), Is.True);
        Assert.That(query.Matches(oldJob), Is.False);
    }

    [Test]
    public void WorkflowQuery_DateFilter_CreatedBefore()
    {
        var query = new WorkflowQuery { CreatedBefore = DateTimeOffset.UtcNow.AddDays(-1) };

        var recentJob = CreateTestJob("job1", createdAt: DateTimeOffset.UtcNow);
        var oldJob = CreateTestJob("job2", createdAt: DateTimeOffset.UtcNow.AddDays(-5));

        Assert.That(query.Matches(recentJob), Is.False);
        Assert.That(query.Matches(oldJob), Is.True);
    }

    [Test]
    public void WorkflowQuery_JobKindFilter_Image()
    {
        var query = new WorkflowQuery { Kind = JobKind.Image };

        var imageJob = CreateTestJob("job1", kind: JobKind.Image);
        var videoJob = CreateTestJob("job2", kind: JobKind.Video);

        Assert.That(query.Matches(imageJob), Is.True);
        Assert.That(query.Matches(videoJob), Is.False);
    }

    [Test]
    public void WorkflowQuery_MinFileCount_Matches()
    {
        var query = new WorkflowQuery { MinFileCount = 3 };

        var manyFiles = CreateTestJob("job1", fileCount: 5);
        var fewFiles = CreateTestJob("job2", fileCount: 2);

        Assert.That(query.Matches(manyFiles), Is.True);
        Assert.That(query.Matches(fewFiles), Is.False);
    }

    #endregion

    #region ParameterDiff Tests

    [Test]
    public void ParameterDiff_SameValue_NotDifferent()
    {
        var diff = new ParameterDiff("Test", "value", "value");

        Assert.That(diff.IsDifferent, Is.False);
    }

    [Test]
    public void ParameterDiff_DifferentValue_IsDifferent()
    {
        var diff = new ParameterDiff("Test", "left", "right");

        Assert.That(diff.IsDifferent, Is.True);
        Assert.That(diff.LeftValue, Is.EqualTo("left"));
        Assert.That(diff.RightValue, Is.EqualTo("right"));
    }

    [Test]
    public void ParameterDiff_NullValues_HandledCorrectly()
    {
        var bothNull = new ParameterDiff("Test", null, null);
        var leftNull = new ParameterDiff("Test", null, "value");
        var rightNull = new ParameterDiff("Test", "value", null);

        Assert.That(bothNull.IsDifferent, Is.False);
        Assert.That(leftNull.IsDifferent, Is.True);
        Assert.That(rightNull.IsDifferent, Is.True);
    }

    #endregion

    #region Helper Methods

    private static JobRow CreateTestJob(
        string jobId,
        long seed = 12345,
        string prompt = "test prompt",
        string? negativePrompt = null,
        string preset = "default",
        bool favorite = false,
        JobKind kind = JobKind.Image,
        int fileCount = 1,
        DateTimeOffset? createdAt = null)
    {
        var files = Enumerable.Range(1, fileCount)
            .Select(i => new FileRef
            {
                RelativePath = $"images/{jobId}_{i}.png",
                Sha256 = $"hash{i}",
                Width = 512,
                Height = 512
            })
            .ToList();

        return new JobRow
        {
            JobId = jobId,
            Seed = seed,
            Prompt = prompt,
            NegativePrompt = negativePrompt,
            PresetId = preset,
            Favorite = favorite,
            Kind = kind,
            Files = files,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow
        };
    }

    #endregion
}
