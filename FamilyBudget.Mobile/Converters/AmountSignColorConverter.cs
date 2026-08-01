using System.Globalization;

namespace FamilyBudget.Mobile.Converters;

// Used for "remaining" style figures where negative means over-budget.
public class AmountSignColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var resources = Application.Current!.Resources;
        var amount = value switch
        {
            long l => l,
            int i => i,
            _ => 0L,
        };
        return amount < 0 ? resources["Error"] : resources["Success"];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
