using System.Globalization;
using Gallery.Domain.Enums;

namespace Gallery.App.Converters;

/// <summary>
/// Converts MediaType to an appropriate icon character.
/// </summary>
public class MediaTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            MediaType.Video => "🎬",
            MediaType.Image => "🖼",
            _ => "📄"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
