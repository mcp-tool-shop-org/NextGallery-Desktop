using System.Globalization;
using Gallery.Domain.Enums;

namespace Gallery.App.Converters;

/// <summary>
/// Returns true if the MediaType is Image (not Video).
/// </summary>
public class IsImageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is MediaType type && type == MediaType.Image;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
