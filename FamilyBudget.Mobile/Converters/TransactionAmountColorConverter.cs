using System.Globalization;
using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Converters;

public class TransactionAmountColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var resources = Application.Current!.Resources;
        return (value as TransactionDto)?.Type switch
        {
            "income" => resources["Success"],
            "expense" => resources["Error"],
            _ => resources["OnSurface"],
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
