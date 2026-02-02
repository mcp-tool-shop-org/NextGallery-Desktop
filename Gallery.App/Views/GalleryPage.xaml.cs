using Gallery.App.ViewModels;

namespace Gallery.App.Views;

public partial class GalleryPage : ContentPage
{
    private GalleryViewModel? _viewModel;

    public GalleryPage()
    {
        InitializeComponent();
    }

    public GalleryPage(GalleryViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel != null)
        {
            await _viewModel.InitializeAsync();
        }

#if WINDOWS
        // Set up keyboard handler for diagnostics toggle
        var window = this.GetParentWindow();
        if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window winuiWindow)
        {
            winuiWindow.Content.KeyDown += OnKeyDown;
        }
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _viewModel?.StopPolling();

#if WINDOWS
        var window = this.GetParentWindow();
        if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window winuiWindow)
        {
            winuiWindow.Content.KeyDown -= OnKeyDown;
        }
#endif
    }

#if WINDOWS
    private void OnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        // Ctrl+Shift+D toggles diagnostics
        if (e.Key == Windows.System.VirtualKey.D)
        {
            var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
            var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);

            if (ctrl.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down) &&
                shift.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
            {
                _viewModel?.ToggleDiagnosticsCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
#endif
}
