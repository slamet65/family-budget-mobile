using System.Globalization;
using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Converters;

public class TransactionDescriptionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            TransactionDto { Type: "income" } t => t.ToWalletName ?? string.Empty,
            TransactionDto { Type: "expense" } t => $"{t.CategoryName} • {t.FromWalletName}",
            TransactionDto { Type: "transfer" } t => $"{t.FromWalletName} → {t.ToWalletName}",
            TransactionDto { Type: "adjustment" } t => $"{t.FromWalletName ?? t.ToWalletName} (penyesuaian)",
            _ => string.Empty,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
