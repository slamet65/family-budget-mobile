using System.Globalization;

namespace FamilyBudget.Mobile.Converters;

public class PeriodStatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value as string == "open" ? "Terbuka" : "Ditutup";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
