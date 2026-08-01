using System.Globalization;
using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Converters;

public class TransactionAmountTextConverter : IValueConverter
{
    private static readonly CultureInfo IdCulture = CultureInfo.GetCultureInfo("id-ID");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TransactionDto transaction)
        {
            return string.Empty;
        }

        var sign = transaction.Type switch
        {
            "income" => "+",
            "expense" => "-",
            _ => string.Empty,
        };

        return $"{sign}Rp {transaction.Amount.ToString("N0", IdCulture)}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
