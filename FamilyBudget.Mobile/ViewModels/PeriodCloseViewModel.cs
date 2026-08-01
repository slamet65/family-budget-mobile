using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

[QueryProperty(nameof(PeriodId), "periodId")]
[QueryProperty(nameof(PeriodDisplayName), "periodName")]
public partial class PeriodCloseViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    public ObservableCollection<WalletCountEntry> WalletEntries { get; } = [];

    public ObservableCollection<ReconciliationDisplayItem> Reconciliations { get; } = [];

    [ObservableProperty]
    private int periodId;

    [ObservableProperty]
    private string? periodDisplayName;

    [ObservableProperty]
    private DateTime newPeriodStartDate = DateTime.Today;

    [ObservableProperty]
    private string? newPeriodName;

    [ObservableProperty]
    private bool isReviewStep;

    [ObservableProperty]
    private string? newPeriodDisplayName;

    [ObservableProperty]
    private int copiedBudgetsCount;

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        var wallets = await apiClient.GetWalletsAsync();
        WalletEntries.Clear();
        foreach (var wallet in wallets)
        {
            WalletEntries.Add(new WalletCountEntry
            {
                WalletId = wallet.Id,
                WalletName = wallet.Name,
                SystemBalance = wallet.Balance,
            });
        }
    });

    [RelayCommand]
    private async Task ConfirmCloseAsync()
    {
        var walletBalances = new List<WalletBalanceEntry>();
        foreach (var entry in WalletEntries)
        {
            if (!long.TryParse(entry.CountedBalanceText, out var counted))
            {
                await feedback.ShowErrorDialogAsync($"Enter the counted balance for {entry.WalletName}.");
                return;
            }
            walletBalances.Add(new WalletBalanceEntry(entry.WalletId, counted));
        }

        var confirmed = await feedback.ShowConfirmationAsync(
            "Close this period?",
            "This locks the period and records any wallet balance adjustments. It cannot be undone.",
            "Close period",
            "Cancel");
        if (!confirmed)
        {
            return;
        }

        await ExecuteSafelyAsync(async () =>
        {
            var startDateUtc = new DateTimeOffset(NewPeriodStartDate.Year, NewPeriodStartDate.Month, NewPeriodStartDate.Day, 0, 0, 0, TimeSpan.Zero);
            var name = string.IsNullOrWhiteSpace(NewPeriodName) ? null : NewPeriodName;

            var response = await apiClient.ClosePeriodAsync(PeriodId, new ClosePeriodRequest(startDateUtc, name, walletBalances));

            Reconciliations.Clear();
            foreach (var reconciliation in response.Reconciliations)
            {
                var walletName = WalletEntries.FirstOrDefault(w => w.WalletId == reconciliation.WalletId)?.WalletName ?? $"Wallet #{reconciliation.WalletId}";
                Reconciliations.Add(new ReconciliationDisplayItem(walletName, reconciliation.SystemBalance, reconciliation.CountedBalance, reconciliation.Difference));
            }

            NewPeriodDisplayName = response.NewPeriod.Name ?? response.NewPeriod.StartDate.ToString("d");
            CopiedBudgetsCount = response.CopiedBudgets.Count;
            IsReviewStep = true;
        });
    }

    [RelayCommand]
    private async Task DoneAsync() => await Shell.Current.GoToAsync("//main/wallets");
}
