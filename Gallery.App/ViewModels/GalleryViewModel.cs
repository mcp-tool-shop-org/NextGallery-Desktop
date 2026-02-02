using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallery.Domain.Index;
using Gallery.Domain.Sources;

namespace Gallery.App.ViewModels;

/// <summary>
/// Generic ViewModel for any gallery source.
/// Source-agnostic - works with folders, CodeComfy, ComfyUI, A1111, Fooocus, etc.
/// </summary>
public partial class GalleryViewModel : ObservableObject, IDisposable
{
    private IGallerySource _source;
    private CancellationTokenSource? _pollCts;
    private int _consecutiveFailures;
    private const int MaxFailuresBeforeBackoff = 3;
    private DateTime _lastPollTime;
    private bool _disposed;

    // Cached state for transient error recovery
    private IReadOnlyList<JobRow>? _lastKnownGood;

    [ObservableProperty]
    private GalleryViewState _currentViewState = GalleryViewState.Loading;

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
    private string _sourcePath = "";

    [ObservableProperty]
    private string _sourceName = "";

    [ObservableProperty]
    private bool _isDiagnosticsVisible;

    [ObservableProperty]
    private string _diagnosticsText = "";

    // Compare Mode
    [ObservableProperty]
    private bool _isCompareMode;

    [ObservableProperty]
    private JobRow? _compareLeftJob;

    [ObservableProperty]
    private JobRow? _compareRightJob;

    [ObservableProperty]
    private CompareViewMode _compareViewMode = CompareViewMode.SideBySide;

    [ObservableProperty]
    private double _overlayBlendRatio = 0.5;

    [ObservableProperty]
    private ObservableCollection<ParameterDiff> _parameterDiffs = new();

    [ObservableProperty]
    private string _changeSummary = "";

    // Search & Filter
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

    public IGallerySource Source => _source;

    public GalleryViewModel(IGallerySource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _sourcePath = source.RootPath;
        _sourceName = source.SourceName;
    }

    /// <summary>
    /// Create a GalleryViewModel for a path, auto-detecting source type.
    /// </summary>
    public static GalleryViewModel CreateForPath(string path)
    {
        var factory = new GallerySourceFactory();
        var source = factory.CreateSource(path);
        return new GalleryViewModel(source);
    }

    /// <summary>
    /// Initialize and perform first load.
    /// </summary>
    public Task InitializeAsync()
    {
        Refresh();
        if (_source.SupportsPolling)
        {
            StartPolling();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Refresh from source.
    /// </summary>
    [RelayCommand]
    public void Refresh()
    {
        if (_disposed) return;

        IsRefreshing = true;

        try
        {
            var result = _source.Load(_lastKnownGood);
            ApplyResult(result);
            _consecutiveFailures = 0;
        }
        catch (Exception ex)
        {
            _consecutiveFailures++;
            Banner = BannerInfo.Warning($"Refresh failed: {ex.Message}");
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

    /// <summary>
    /// Async refresh for background use.
    /// </summary>
    public async Task RefreshAsync()
    {
        await Task.Run(() => MainThread.BeginInvokeOnMainThread(Refresh));
    }

    private void ApplyResult(IndexLoadResult result)
    {
        Banner = result.Banner;

        switch (result.State)
        {
            case GalleryState.Loading:
                CurrentViewState = GalleryViewState.Loading;
                break;

            case GalleryState.Empty:
                CurrentViewState = GalleryViewState.Empty;
                Jobs.Clear();
                break;

            case GalleryState.List list:
                CurrentViewState = GalleryViewState.List;
                Jobs = new ObservableCollection<JobRow>(list.Items);
                _lastKnownGood = list.Items;

                // Auto-select first if no selection
                if (SelectedJob == null && Jobs.Count > 0)
                {
                    SelectedJob = Jobs[0];
                }

                // Update filtered view
                ApplyFilter();
                break;

            case GalleryState.Fatal fatal:
                CurrentViewState = GalleryViewState.Fatal;
                ErrorMessage = fatal.Message;
                FatalReason = fatal.Reason;
                break;
        }
    }

    #region Polling

    private void StartPolling()
    {
        if (_pollCts != null || !_source.SupportsPolling) return;

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

                if (_source.HasChangedSince(_lastPollTime))
                {
                    await RefreshAsync();
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
            _pollCts.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed
        }

        _pollCts = null;
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void OpenOutputsFolder()
    {
        try
        {
            var outputsPath = _source.OutputsPath;
            if (Directory.Exists(outputsPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = outputsPath,
                    UseShellExecute = true
                });
            }
            else if (Directory.Exists(_source.RootPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = _source.RootPath,
                    UseShellExecute = true
                });
            }
            else
            {
                Banner = BannerInfo.Warning("Folder not found");
            }
        }
        catch (Exception ex)
        {
            Banner = BannerInfo.Warning($"Cannot open folder: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task BrowseFolderAsync()
    {
        try
        {
            var result = await CommunityToolkit.Maui.Storage.FolderPicker.Default.PickAsync(CancellationToken.None);

            if (result.IsSuccessful && result.Folder != null)
            {
                var newPath = result.Folder.Path;
                await SwitchSourceAsync(newPath);
            }
        }
        catch (Exception ex)
        {
            Banner = BannerInfo.Warning($"Folder picker failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Switch to a new source folder.
    /// </summary>
    public async Task SwitchSourceAsync(string newPath)
    {
        // Stop polling on old source
        StopPolling();

        // Dispose old source
        _source.Dispose();

        // Create new source
        var factory = new GallerySourceFactory();
        _source = factory.CreateSource(newPath);

        // Update display properties
        SourcePath = _source.RootPath;
        SourceName = _source.SourceName;

        // Clear old state
        _lastKnownGood = null;
        Jobs.Clear();
        FilteredJobs.Clear();
        SelectedJob = null;
        _consecutiveFailures = 0;

        // Load from new source
        await Task.Run(() => MainThread.BeginInvokeOnMainThread(() =>
        {
            Refresh();
            if (_source.SupportsPolling)
            {
                StartPolling();
            }
        }));
    }

    [RelayCommand]
    private void SelectJob(JobRow? job)
    {
        SelectedJob = job;
    }

    #region Job Actions

    [RelayCommand(CanExecute = nameof(CanExecuteJobAction))]
    private async Task DeleteJobAsync(bool deleteFiles = false)
    {
        if (SelectedJob == null) return;

        var job = SelectedJob;
        var jobId = job.JobId;

        try
        {
            if (_source.SupportsDeletion)
            {
                _source.DeleteJob(job, deleteFiles);
            }

            // Remove from local collection
            var jobToRemove = Jobs.FirstOrDefault(j => j.JobId == jobId);
            if (jobToRemove != null)
            {
                Jobs.Remove(jobToRemove);
            }

            SelectedJob = Jobs.FirstOrDefault();
            Banner = BannerInfo.Warning($"Item deleted" + (deleteFiles ? " with files" : ""));
        }
        catch (Exception ex)
        {
            Banner = BannerInfo.Warning($"Failed to delete: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteJobAction))]
    private void OpenJobFilesAsync()
    {
        if (SelectedJob == null) return;

        var firstFile = SelectedJob.Files.FirstOrDefault();
        if (firstFile == null)
        {
            Banner = BannerInfo.Warning("No files in this item");
            return;
        }

        try
        {
            var fullPath = _source.GetFullPath(firstFile);
            var directory = Path.GetDirectoryName(fullPath);

            if (File.Exists(fullPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
            }
            else if (directory != null && Directory.Exists(directory))
            {
                System.Diagnostics.Process.Start("explorer.exe", directory);
            }
            else
            {
                Banner = BannerInfo.Warning("File not found");
            }
        }
        catch (Exception ex)
        {
            Banner = BannerInfo.Warning($"Cannot open: {ex.Message}");
        }
    }

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
            Source = _source.SourceName,
            FileCount = job.Files.Count,
            Files = job.Files.Select(f => new
            {
                f.RelativePath,
                FullPath = _source.GetFullPath(f),
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
        Banner = BannerInfo.Warning("Metadata copied to clipboard");
    }

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
        Banner = BannerInfo.Warning("Parameters copied to clipboard");
    }

    private bool CanExecuteJobAction() => SelectedJob != null;

    #endregion

    #region Compare Mode

    [RelayCommand(CanExecute = nameof(CanExecuteJobAction))]
    private void StartCompare()
    {
        if (SelectedJob == null) return;

        CompareLeftJob = SelectedJob;
        CompareRightJob = null;
        IsCompareMode = true;
        ParameterDiffs.Clear();
        ChangeSummary = "Select a second item to compare";
        Banner = BannerInfo.Warning("Compare mode: select second item");
    }

    [RelayCommand]
    private void SetCompareRight(JobRow? job)
    {
        if (!IsCompareMode || job == null) return;

        CompareRightJob = job;
        UpdateComparison();
    }

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

    [RelayCommand(CanExecute = nameof(CanSwapCompare))]
    private void SwapCompare()
    {
        if (CompareLeftJob == null || CompareRightJob == null) return;

        (CompareLeftJob, CompareRightJob) = (CompareRightJob, CompareLeftJob);
        UpdateComparison();
    }

    private bool CanSwapCompare() => IsCompareMode && CompareLeftJob != null && CompareRightJob != null;

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

    #region Search & Filter

    [RelayCommand]
    private void ToggleFilterPanel()
    {
        IsFilterPanelVisible = !IsFilterPanelVisible;
        if (IsFilterPanelVisible)
        {
            UpdateAvailablePresets();
        }
    }

    [RelayCommand]
    private void ApplyFilter()
    {
        var query = BuildCurrentQuery();

        if (!query.HasActiveFilters)
        {
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
            Banner = BannerInfo.Warning("No items match filter");
        }
        else
        {
            Banner = BannerInfo.Warning($"Showing {filtered.Count} of {Jobs.Count} items");
        }
    }

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

    [RelayCommand]
    private void SearchBySeed(long seed)
    {
        SeedSearchText = seed.ToString();
        ApplyFilter();
    }

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
        var pollingStatus = _consecutiveFailures >= MaxFailuresBeforeBackoff
            ? "manual-backoff"
            : IsPollingEnabled ? "active" : "paused";

        DiagnosticsText = $"""
            === Gallery Diagnostics ===

            Source Type:    {_source.SourceName}
            Root Path:      {_source.RootPath}
            Outputs Path:   {_source.OutputsPath}

            Polling:        {(_source.SupportsPolling ? pollingStatus : "not supported")}
            Failures:       {_consecutiveFailures}/{MaxFailuresBeforeBackoff}
            Last Poll:      {(_lastPollTime == default ? "never" : _lastPollTime.ToString("o"))}

            Items Loaded:   {Jobs.Count}
            Filtered:       {FilteredJobCount}
            View State:     {CurrentViewState}
            """;
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
            _source.Dispose();
            _lastKnownGood = null;
        }

        _disposed = true;
    }
}

/// <summary>
/// View states for UI binding.
/// </summary>
public enum GalleryViewState
{
    Loading,
    Empty,
    List,
    Fatal
}

/// <summary>
/// Static converters for GalleryViewState to bool visibility.
/// </summary>
public static class GalleryViewStateConverters
{
    public static IValueConverter IsLoading { get; } = new GalleryViewStateConverter(GalleryViewState.Loading);
    public static IValueConverter IsEmpty { get; } = new GalleryViewStateConverter(GalleryViewState.Empty);
    public static IValueConverter IsList { get; } = new GalleryViewStateConverter(GalleryViewState.List);
    public static IValueConverter IsFatal { get; } = new GalleryViewStateConverter(GalleryViewState.Fatal);

    private sealed class GalleryViewStateConverter : IValueConverter
    {
        private readonly GalleryViewState _targetState;
        public GalleryViewStateConverter(GalleryViewState targetState) => _targetState = targetState;

        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            return value is GalleryViewState state && state == _targetState;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
