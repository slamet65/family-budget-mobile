using System.Globalization;
using FamilyBudget.Mobile.Common;
using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Converters;

// Picks a glyph from IconGlyphs.WalletPalette deterministically by wallet Id, so a given
// wallet always shows the same icon on every device without an `icon` field on the API.
public class WalletIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            WalletDto wallet => IconGlyphs.WalletPalette[wallet.Id % IconGlyphs.WalletPalette.Length],
            _ => IconGlyphs.AccountBalanceWallet,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
