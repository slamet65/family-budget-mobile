using System.Globalization;
using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Converters;

public class TransactionIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as TransactionDto)?.Type switch
        {
            "income" => "↓",
            "expense" => "↑",
            "transfer" => "↔",
            "adjustment" => "⚖",
            _ => "•",
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
