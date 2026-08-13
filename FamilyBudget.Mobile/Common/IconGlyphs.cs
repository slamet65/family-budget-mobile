namespace FamilyBudget.Mobile.Common;

// Codepoints from Material Symbols Outlined (fill=0, wght=400, grad=0, opsz=24) --
// https://github.com/google/material-design-icons, subsetted into Resources/Fonts/MaterialSymbolsOutlined.ttf.
// Rendered via FontFamily="MaterialSymbolsOutlined" so every device draws the identical glyph
// (unlike Unicode emoji, which render through the OS's own emoji font and vary by device/vendor).
public static class IconGlyphs
{
    public const string FontFamily = "MaterialSymbolsOutlined";

    public const string AccountBalance = "";
    public const string AccountBalanceWallet = "";
    public const string Balance = "";
    public const string CallMade = "";
    public const string CallReceived = "";
    public const string CreditCard = "";
    public const string CurrencyExchange = "";
    public const string Payments = "";
    public const string RadioButtonUnchecked = "";
    public const string Savings = "";
    public const string SwapHoriz = "";

    // Cycled deterministically by wallet Id (Id % length) so the same wallet always
    // shows the same icon on every device without an `icon` field on the API.
    public static readonly string[] WalletPalette =
    [
        Savings,
        Payments,
        AccountBalance,
        CreditCard,
        CurrencyExchange,
        AccountBalanceWallet,
    ];
}
