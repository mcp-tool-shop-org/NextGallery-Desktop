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

        // Always use GalleryPage - it supports folder browsing
        var factory = new GallerySourceFactory();
        var workspacePath = config.WorkspacePath;

        if (!string.IsNullOrEmpty(workspacePath) && Directory.Exists(workspacePath))
        {
            // Valid workspace/folder provided - load it
            var source = factory.CreateSource(workspacePath);
            var viewModel = new GalleryViewModel(source);
            startPage = new GalleryPage(viewModel);
            title = $"NextGallery - {Path.GetFileName(workspacePath)} ({source.SourceName})";
        }
        else
        {
            // No workspace or invalid path - start with empty source, user can browse
            var defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            var source = factory.CreateSource(defaultPath);
            var viewModel = new GalleryViewModel(source);
            startPage = new GalleryPage(viewModel);
            title = "NextGallery - Select a folder";
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
