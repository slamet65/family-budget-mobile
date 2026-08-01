using System.Globalization;

namespace FamilyBudget.Mobile.Converters;

// Pass ConverterParameter="invert" to get "is zero" instead.
public class NonZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isNonZero = value switch
        {
            long l => l != 0,
            int i => i != 0,
            _ => false,
        };
        return string.Equals(parameter as string, "invert", StringComparison.OrdinalIgnoreCase) ? !isNonZero : isNonZero;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
