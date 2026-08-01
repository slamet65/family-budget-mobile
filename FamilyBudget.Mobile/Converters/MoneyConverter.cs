using System.Globalization;

namespace FamilyBudget.Mobile.Converters;

// API amounts are whole Rupiah integers (no cents) -- see REQUIREMENTS.md.
public class MoneyConverter : IValueConverter
{
    private static readonly CultureInfo IdCulture = CultureInfo.GetCultureInfo("id-ID");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        long l => Format(l),
        int i => Format(i),
        _ => string.Empty,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static string Format(long amount) => $"Rp {amount.ToString("N0", IdCulture)}";
}
