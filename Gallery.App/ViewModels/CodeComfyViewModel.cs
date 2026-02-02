using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallery.Domain.Index;
using Gallery.Domain.Routing;

namespace Gallery.App.ViewModels;

/// <summary>
/// ViewModel for CodeComfy workspace mode.
/// Pure projection of GalleryState from IndexLoader.
/// </summary>
public partial class CodeComfyViewModel : ObservableObject, IDisposable
{
    private readonly IndexLoader _indexLoader;
    private readonly IFileReader _fileReader;
    private readonly string _workspaceRoot;
    private readonly string _workspaceKey;

    private CancellationTokenSource? _pollCts;
    private int _consecutiveFailures;
    private const int MaxFailuresBeforeBackoff = 3;
    private DateTime _lastPollTime;
    private bool _disposed;

    // Cached state for transient error recovery
    private IReadOnlyList<JobRow>? _lastKnownGood;

    [ObservableProperty]
    private CodeComfyViewState _currentCodeComfyViewState = CodeComfyViewState.Loading;

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private FatalReason? _fatalReason;

    [ObservableProperty]
    private ObservableCollection<JobRow> _jobs = new();

    [ObservableProperty]
    private JobRow? _selectedJob;

    [ObservableProperty]
    private BannerInfo _banner = BannerInfo.None;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isPollingEnabled = true;

    [ObservableProperty]
    private string _workspacePath;

    [ObservableProperty]
    private bool _isDiagnosticsVisible;

    [ObservableProperty]
    private string _diagnosticsText = "";

    // Compare Mode (2026 feature)
    [ObservableProperty]
    private bool _isCompareMode;

    [ObservableProperty]
    private JobRow? _compareLeftJob;

    [ObservableProperty]
    private JobRow? _compareRightJob;

    [ObservableProperty]
    private CompareViewMode _compareViewMode = CompareViewMode.SideBySide;

    [ObservableProperty]
    private double _overlayBlendRatio = 0.5; // 0 = full left, 1 = full right

    [ObservableProperty]
    private ObservableCollection<ParameterDiff> _parameterDiffs = new();

    [ObservableProperty]
    private string _changeSummary = "";

    // Workflow Search & Filter (2026 feature)
    [ObservableProperty]
    private bool _isFilterPanelVisible;

    [ObservableProperty]
    private string _promptSearchText = "";

    [ObservableProperty]
    private string _seedSearchText = "";

    [ObservableProperty]
    private string? _selectedPresetFilter;

    [ObservableProperty]
    private bool _showOnlyFavorites;

    [ObservableProperty]
    private ObservableCollection<string> _availablePresets = new();

    [ObservableProperty]
    private string _filterSummary = "";

    [ObservableProperty]
    private int _filteredJobCount;

    [ObservableProperty]
    private ObservableCollection<JobRow> _filteredJobs = new();

    public string WorkspaceKey => _workspaceKey;

    public CodeComfyViewModel(string workspaceRoot)
    {
        _workspaceRoot = workspaceRoot;
        _workspacePath = workspaceRoot;
        _workspaceKey = Domain.WorkspaceKey.ComputeKey(workspaceRoot);
        _fileReader = RealFileReader.Instance;
        _indexLoader = new IndexLoader(_fileReader);
    }

    /// <summary>
    /// Initialize and perform first load.
    /// </summary>
    public async Task InitializeAsync()
    {
        await RefreshAsync();
        StartPolling();
    }

    /// <summary>
    /// Refresh from index file.
    /// </summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (_disposed) return;

        IsRefreshing = true;

        try
        {
            await Task.Run(() =>
            {
                if (_disposed) return;
                var result = _indexLoader.Load(_workspaceRoot, _lastKnownGood);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_disposed) return;
                    ApplyResult(result);
                });
            });

            _consecutiveFailures = 0;
        }
        catch (Exception ex)
        {
            _consecutiveFailures++;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_disposed) return;
                Banner = BannerInfo.Warning($"Refresh failed: {ex.Message}");
            });
        }
        finally
        {
            if (!_disposed)
            {
                IsRefreshing = false;
                _lastPollTime = DateTime.UtcNow;
            }
        }
    }

    private void ApplyResult(IndexLoadResult result)
    {
        Banner = result.Banner;

        switch (result.State)
        {
            case GalleryState.Loading:
                CurrentCodeComfyViewState = CodeComfyViewState.Loading;
                break;

            case GalleryState.Empty empty:
                CurrentCodeComfyViewState = CodeComfyViewState.Empty;
                Jobs.Clear();
                break;

            case GalleryState.List list:
                CurrentCodeComfyViewState = CodeComfyViewState.List;
                Jobs = new ObservableCollection<JobRow>(list.Items);
                _lastKnownGood = list.Items;

                // Auto-select first if no selection
                if (SelectedJob == null && Jobs.Count > 0)
                {
                    SelectedJob = Jobs[0];
                }
                break;

            case GalleryState.Fatal fatal:
                CurrentCodeComfyViewState = CodeComfyViewState.Fatal;
                ErrorMessage = fatal.Message;
                FatalReason = fatal.Reason;
                break;
        }
    }

    #region Polling

    private void StartPolling()
    {
        if (_pollCts != null) return;

        _pollCts = new CancellationTokenSource();
        _ = PollLoopAsync(_pollCts.Token);
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), ct);

                if (!IsPollingEnabled) continue;
                if (_consecutiveFailures >= MaxFailuresBeforeBackoff) continue;

                // Check if file changed before reloading
                var indexPath = Path.Combine(_workspaceRoot, ".codecomfy", "outputs", "index.json");
                if (_fileReader.FileExists(indexPath))
                {
                    var lastWrite = _fileReader.GetLastWriteTimeUtc(indexPath);
                    if (lastWrite > _lastPollTime)
                    {
                        await RefreshAsync();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                _consecutiveFailures++;
            }
        }
    }

    public void StopPolling()
    {
        if (_pollCts == null) return;

        try
        {
            _pollCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed, safe to ignore
        }

        try
        {
            _pollCts.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed, safe to ignore
        }

        _pollCts = null;
    }

    #endregion

    #region Focus Events

    /// <summary>
    /// Called when window gains focus.
    /// </summary>
    public async Task OnWindowActivatedAsync()
    {
        // Reset failure count on focus
        _consecutiveFailures = 0;
        await RefreshAsync();
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task OpenOutputsFolderAsync()
    {
        var outputsPath = Path.Combine(_workspaceRoot, ".codecomfy", "outputs");
        if (Directory.Exists(outputsPath))
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(outputsPath)
            });
        }
        else
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(_workspaceRoot)
            });
        }
    }

    [RelayCommand]
    private void SelectJob(JobRow? job)
    {
        SelectedJob = job;
    }

    #region Job Actions (2026 Agency Features)

    /// <summary>
    /// Delete job from index and optionally delete files.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteJobAction))]
    private async Task DeleteJobAsync(bool deleteFiles = false)
    {
        if (SelectedJob == null) return;

        var job = SelectedJob;
        var jobId = job.JobId;

        try
        {
            if (deleteFiles)
            {
                // Delete actual files
                foreach (var file in job.Files)
                {
                    var fullPath = Path.Combine(_workspaceRoot, ".codecomfy", "outputs", file.RelativePath);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
            }

            // Remove from local collection
            var jobToRemove = Jobs.FirstOrDefault(j => j.JobId == jobId);
            if (jobToRemove != null)
            {
                Jobs.Remove(jobToRemove);
            }

            // Clear selection
            SelectedJob = Jobs.FirstOrDefault();

            Banner = BannerInfo.Warning($"Job {jobId[..8]}... deleted" + (deleteFiles ? " with files" : ""));
        }
        catch (Exception ex)
        {
            Banner = BannerInfo.Warning($"Failed to delete job: {ex.Message}");
        }
    }

    /// <summary>
    /// Open the folder containing job output files.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteJobAction))]
    private async Task OpenJobFilesAsync()
    {
        if (SelectedJob == null) return;

        var firstFile = SelectedJob.Files.FirstOrDefault();
        if (firstFile == null)
        {
            Banner = BannerInfo.Warning("Job has no files");
            return;
        }

        var fullPath = Path.Combine(_workspaceRoot, ".codecomfy", "outputs", firstFile.RelativePath);
        var directory = Path.GetDirectoryName(fullPath);

        if (directory != null && Directory.Exists(directory))
        {
            // Open folder and select the file
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
            }
            catch
            {
                // Fallback: just open the directory
                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(directory)
                });
            }
        }
        else
        {
            Banner = BannerInfo.Warning("Output folder not found");
        }
    }

    /// <summary>
    /// Copy the prompt text to clipboard.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteJobAction))]
    private async Task CopyPromptAsync()
    {
        if (SelectedJob == null) return;

        var prompt = SelectedJob.Prompt;
        if (string.IsNullOrWhiteSpace(prompt) || prompt == "(no prompt)")
        {
            Banner = BannerInfo.Warning("No prompt available");
            return;
        }

        await Clipboard.SetTextAsync(prompt);
        Banner = BannerInfo.Warning("Prompt copied to clipboard");
    }

    /// <summary>
    /// Copy full job metadata as JSON to clipboard.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteJobAction))]
    private async Task CopyFullMetadataAsync()
    {
        if (SelectedJob == null) return;

        var job = SelectedJob;
        var metadata = new
        {
            job.JobId,
            CreatedAt = job.CreatedAt.ToString("o"),
            job.Kind,
            job.Seed,
            job.Prompt,
            job.NegativePrompt,
            job.PresetId,
            job.ElapsedSeconds,
            job.Tags,
            job.Favorite,
            job.Notes,
            FileCount = job.Files.Count,
            Files = job.Files.Select(f => new
            {
                f.RelativePath,
                f.Width,
                f.Height,
                f.SizeBytes,
                f.ContentType
            })
        };

        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await Clipboard.SetTextAsync(json);
        Banner = BannerInfo.Warning("Full metadata copied to clipboard");
    }

    /// <summary>
    /// Copy generation parameters in a format suitable for re-use.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteJobAction))]
    private async Task CopyGenerationParamsAsync()
    {
        if (SelectedJob == null) return;

        var job = SelectedJob;
        var sb = new StringBuilder();

        sb.AppendLine("=== Generation Parameters ===");
        sb.AppendLine($"Prompt: {job.Prompt}");

        if (!string.IsNullOrWhiteSpace(job.NegativePrompt))
        {
            sb.AppendLine($"Negative: {job.NegativePrompt}");
        }

        sb.AppendLine($"Seed: {job.Seed}");
        sb.AppendLine($"Preset: {job.PresetId}");

        if (job.ElapsedSeconds.HasValue)
        {
            sb.AppendLine($"Time: {job.ElapsedSeconds:F1}s");
        }

        await Clipboard.SetTextAsync(sb.ToString());
        Banner = BannerInfo.Warning("Generation params copied to clipboard");
    }

    private bool CanExecuteJobAction() => SelectedJob != null;

    #endregion

    #region Compare Mode (2026 Industry Feature)

    /// <summary>
    /// Enter compare mode with the currently selected job as the left side.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteJobAction))]
    private void StartCompare()
    {
        if (SelectedJob == null) return;

        CompareLeftJob = SelectedJob;
        CompareRightJob = null;
        IsCompareMode = true;
        ParameterDiffs.Clear();
        ChangeSummary = "Select a second job to compare";
        Banner = BannerInfo.Warning("Compare mode: select second job");
    }

    /// <summary>
    /// Set the right job for comparison.
    /// </summary>
    [RelayCommand]
    private void SetCompareRight(JobRow? job)
    {
        if (!IsCompareMode || job == null) return;

        CompareRightJob = job;
        UpdateComparison();
    }

    /// <summary>
    /// Exit compare mode.
    /// </summary>
    [RelayCommand]
    private void ExitCompare()
    {
        IsCompareMode = false;
        CompareLeftJob = null;
        CompareRightJob = null;
        ParameterDiffs.Clear();
        ChangeSummary = "";
        Banner = BannerInfo.None;
    }

    /// <summary>
    /// Swap left and right jobs in comparison.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSwapCompare))]
    private void SwapCompare()
    {
        if (CompareLeftJob == null || CompareRightJob == null) return;

        (CompareLeftJob, CompareRightJob) = (CompareRightJob, CompareLeftJob);
        UpdateComparison();
    }

    private bool CanSwapCompare() => IsCompareMode && CompareLeftJob != null && CompareRightJob != null;

    /// <summary>
    /// Cycle through compare view modes.
    /// </summary>
    [RelayCommand]
    private void CycleCompareViewMode()
    {
        CompareViewMode = CompareViewMode switch
        {
            CompareViewMode.SideBySide => CompareViewMode.Overlay,
            CompareViewMode.Overlay => CompareViewMode.DiffOnly,
            CompareViewMode.DiffOnly => CompareViewMode.SideBySide,
            _ => CompareViewMode.SideBySide
        };
    }

    private void UpdateComparison()
    {
        if (CompareLeftJob == null || CompareRightJob == null)
        {
            ParameterDiffs.Clear();
            ChangeSummary = "";
            return;
        }

        var session = new CompareSession
        {
            Left = CompareLeftJob,
            Right = CompareRightJob,
            ViewMode = CompareViewMode
        };

        var diffs = session.GetDiffs();
        ParameterDiffs = new ObservableCollection<ParameterDiff>(diffs);
        ChangeSummary = session.GetChangeSummary();
        Banner = BannerInfo.None;
    }

    #endregion

    #region Workflow Search & Filter (2026 Industry Feature)

    /// <summary>
    /// Toggle the filter panel visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleFilterPanel()
    {
        IsFilterPanelVisible = !IsFilterPanelVisible;
        if (IsFilterPanelVisible)
        {
            UpdateAvailablePresets();
        }
    }

    /// <summary>
    /// Apply current filter settings.
    /// </summary>
    [RelayCommand]
    private void ApplyFilter()
    {
        var query = BuildCurrentQuery();

        if (!query.HasActiveFilters)
        {
            // No filters - show all
            FilteredJobs = new ObservableCollection<JobRow>(Jobs);
            FilteredJobCount = Jobs.Count;
            FilterSummary = "";
            return;
        }

        var filtered = Jobs.Where(query.Matches).ToList();
        FilteredJobs = new ObservableCollection<JobRow>(filtered);
        FilteredJobCount = filtered.Count;
        FilterSummary = query.GetFilterSummary();

        if (filtered.Count == 0)
        {
            Banner = BannerInfo.Warning("No jobs match filter");
        }
        else
        {
            Banner = BannerInfo.Warning($"Showing {filtered.Count} of {Jobs.Count} jobs");
        }
    }

    /// <summary>
    /// Clear all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        PromptSearchText = "";
        SeedSearchText = "";
        SelectedPresetFilter = null;
        ShowOnlyFavorites = false;
        FilterSummary = "";
        FilteredJobs = new ObservableCollection<JobRow>(Jobs);
        FilteredJobCount = Jobs.Count;
        Banner = BannerInfo.None;
    }

    /// <summary>
    /// Quick search by seed - useful for finding variations.
    /// </summary>
    [RelayCommand]
    private void SearchBySeed(long seed)
    {
        SeedSearchText = seed.ToString();
        ApplyFilter();
    }

    /// <summary>
    /// Quick filter to show only jobs with same preset as selected.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteJobAction))]
    private void FilterBySelectedPreset()
    {
        if (SelectedJob == null) return;

        SelectedPresetFilter = SelectedJob.PresetId;
        IsFilterPanelVisible = true;
        ApplyFilter();
    }

    private WorkflowQuery BuildCurrentQuery()
    {
        var query = new WorkflowQuery
        {
            PromptContains = string.IsNullOrWhiteSpace(PromptSearchText) ? null : PromptSearchText,
            PresetId = SelectedPresetFilter,
            IsFavorite = ShowOnlyFavorites ? true : null
        };

        // Parse seed if provided
        if (!string.IsNullOrWhiteSpace(SeedSearchText) &&
            long.TryParse(SeedSearchText, out var seed))
        {
            query.Seed = seed;
        }

        return query;
    }

    private void UpdateAvailablePresets()
    {
        var presets = Jobs
            .Select(j => j.PresetId)
            .Where(p => !string.IsNullOrWhiteSpace(p) && p != "unknown")
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        AvailablePresets = new ObservableCollection<string>(presets);
    }

    #endregion

    [RelayCommand]
    private void ToggleDiagnostics()
    {
        IsDiagnosticsVisible = !IsDiagnosticsVisible;
        if (IsDiagnosticsVisible)
        {
            UpdateDiagnosticsText();
        }
    }

    private void UpdateDiagnosticsText()
    {
        var indexPath = Path.Combine(_workspaceRoot, ".codecomfy", "outputs", "index.json");
        var indexExists = _fileReader.FileExists(indexPath);
        var lastWriteTime = indexExists ? _fileReader.GetLastWriteTimeUtc(indexPath) : (DateTime?)null;
        var canonPath = Domain.WorkspaceKey.NormalizePath(_workspaceRoot);

        var pollingStatus = _consecutiveFailures >= MaxFailuresBeforeBackoff
            ? "manual-backoff"
            : IsPollingEnabled ? "active" : "paused";

        var skippedCount = Banner.SkippedCount;

        DiagnosticsText = $"""
            === CodeComfy Diagnostics ===

            Workspace Path: {_workspaceRoot}
            Canon Path:     {canonPath}
            Workspace Key:  {_workspaceKey}
            Pipe Name:      \\.\pipe\codecomfy.nextgallery.{_workspaceKey}

            Index Path:     {indexPath}
            Index Exists:   {indexExists}
            Last Write:     {lastWriteTime?.ToString("o") ?? "N/A"}
            Last Poll:      {(_lastPollTime == default ? "never" : _lastPollTime.ToString("o"))}

            Polling Status: {pollingStatus}
            Failures:       {_consecutiveFailures}/{MaxFailuresBeforeBackoff}
            Jobs Loaded:    {Jobs.Count}
            Skipped:        {skippedCount}
            View State:     {CurrentCodeComfyViewState}
            """;
    }

    #endregion

    #region Activation Handler Integration

    /// <summary>
    /// Handle activation from another instance.
    /// Returns response for IPC.
    /// </summary>
    public ActivationResponsePayload HandleActivation(
        ActivationRequestPayload request,
        IWindowManager windowManager)
    {
        var result = ActivationHandler.HandleSecondInstanceActivation(
            request,
            _workspaceKey,
            windowManager,
            new ViewModelIndexLoader(this));

        return ActivationHandler.ToResponsePayload(result);
    }

    private sealed class ViewModelIndexLoader : IIndexLoader
    {
        private readonly CodeComfyViewModel _vm;
        public ViewModelIndexLoader(CodeComfyViewModel vm) => _vm = vm;
        public void Refresh() => MainThread.BeginInvokeOnMainThread(async () => await _vm.RefreshAsync());
    }

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            StopPolling();
            _lastKnownGood = null;
        }

        _disposed = true;
    }
}

/// <summary>
/// View states for UI binding.
/// </summary>
public enum CodeComfyViewState
{
    Loading,
    Empty,
    List,
    Fatal
}

/// <summary>
/// Static converters for CodeComfyViewState to bool visibility.
/// Used via x:Static in XAML.
/// </summary>
public static class CodeComfyViewStateConverters
{
    public static IValueConverter IsLoading { get; } = new CodeComfyViewStateConverter(CodeComfyViewState.Loading);
    public static IValueConverter IsEmpty { get; } = new CodeComfyViewStateConverter(CodeComfyViewState.Empty);
    public static IValueConverter IsList { get; } = new CodeComfyViewStateConverter(CodeComfyViewState.List);
    public static IValueConverter IsFatal { get; } = new CodeComfyViewStateConverter(CodeComfyViewState.Fatal);

    private sealed class CodeComfyViewStateConverter : IValueConverter
    {
        private readonly CodeComfyViewState _targetState;
        public CodeComfyViewStateConverter(CodeComfyViewState targetState) => _targetState = targetState;

        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            return value is CodeComfyViewState state && state == _targetState;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
