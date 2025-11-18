using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AvaloniaHello.Converters;

public class BoolToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isDragging && parameter is string colors)
        {
            var colorStrings = colors.Split(';');
            if (colorStrings.Length == 2)
            {
                var colorString = isDragging ? colorStrings[1] : colorStrings[0];
                if (Color.TryParse(colorString, out var color))
                {
                    return new SolidColorBrush(color);
                }
            }
        }

        // Default to blue if parsing fails
        return new SolidColorBrush(Color.Parse("#1d4ed8"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}