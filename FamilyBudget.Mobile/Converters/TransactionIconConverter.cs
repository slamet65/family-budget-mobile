using System.Globalization;
using FamilyBudget.Mobile.Common;
using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Converters;

public class TransactionIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as TransactionDto)?.Type switch
        {
            "income" => IconGlyphs.CallReceived,
            "expense" => IconGlyphs.CallMade,
            "transfer" => IconGlyphs.SwapHoriz,
            "adjustment" => IconGlyphs.Balance,
            "saving_deposit" => IconGlyphs.Savings,
            "saving_withdrawal" => IconGlyphs.Savings,
            _ => IconGlyphs.RadioButtonUnchecked,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
