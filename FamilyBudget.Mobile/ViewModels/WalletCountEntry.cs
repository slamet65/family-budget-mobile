using CommunityToolkit.Mvvm.ComponentModel;

namespace FamilyBudget.Mobile.ViewModels;

/// <summary>One wallet's row in the tutup buku step-1 form: the system-computed balance
/// (read-only) alongside an editable field for what was physically counted.</summary>
public partial class WalletCountEntry : ObservableObject
{
    public required int WalletId { get; init; }

    public required string WalletName { get; init; }

    public required long SystemBalance { get; init; }

    [ObservableProperty]
    private string countedBalanceText = string.Empty;
}
