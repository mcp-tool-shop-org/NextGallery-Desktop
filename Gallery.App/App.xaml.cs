using Gallery.App.Services;
using Gallery.App.Views;
using Gallery.App.ViewModels;
using Gallery.Domain.Sources;

namespace Gallery.App;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var config = _services.GetRequiredService<WorkspaceConfiguration>();
        Page startPage;
        string title;

        if (!string.IsNullOrEmpty(config.WorkspacePath))
        {
            // Workspace/folder mode - auto-detect source type
            var factory = new GallerySourceFactory();
            var sourceType = factory.DetectSourceType(config.WorkspacePath);
            var source = factory.CreateSource(config.WorkspacePath);
            var viewModel = new GalleryViewModel(source);

            startPage = new GalleryPage(viewModel);
            title = $"NextGallery - {Path.GetFileName(config.WorkspacePath)} ({source.SourceName})";
        }
        else
        {
            // No workspace - show folder picker / main page
            startPage = _services.GetRequiredService<MainPage>();
            title = "NextGallery";
        }

        return new Window(startPage)
        {
            Title = title,
            Width = 1400,
            Height = 900,
            MinimumWidth = 1000,
            MinimumHeight = 600
        };
    }
}
