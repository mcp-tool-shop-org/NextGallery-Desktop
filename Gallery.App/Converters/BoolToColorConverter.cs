using System.Globalization;

namespace Gallery.App.Converters;

/// <summary>
/// Converts bool to a color for filter button highlighting.
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            return Colors.DarkOrange; // Active filter color
        }
        return Color.FromArgb("#374151"); // Gray700
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
