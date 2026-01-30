using System.Globalization;
using Gallery.Domain.Enums;

namespace Gallery.App.Converters;

/// <summary>
/// Returns true if the MediaType is Video.
/// </summary>
public class IsVideoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is MediaType type && type == MediaType.Video;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
