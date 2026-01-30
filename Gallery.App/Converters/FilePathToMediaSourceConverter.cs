using System.Globalization;
using CommunityToolkit.Maui.Views;

namespace Gallery.App.Converters;

/// <summary>
/// Converts a file path string to a MediaSource for MediaElement.
/// </summary>
public class FilePathToMediaSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path) && File.Exists(path))
        {
            return MediaSource.FromFile(path);
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
