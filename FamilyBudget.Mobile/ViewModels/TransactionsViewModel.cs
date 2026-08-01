using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class TransactionsViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    private bool isLoadingFilters;

    public ObservableCollection<TransactionDto> Transactions { get; } = [];

    public ObservableCollection<TransactionFilterOption> PeriodOptions { get; } = [];

    public ObservableCollection<TransactionFilterOption> WalletOptions { get; } = [];

    public ObservableCollection<TransactionTypeOption> TypeOptions { get; } =
    [
        new(null, "All types"),
        new("income", "Income"),
        new("expense", "Expense"),
        new("transfer", "Transfer"),
        new("adjustment", "Adjustment"),
    ];

    [ObservableProperty]
    private TransactionFilterOption? selectedPeriodOption;

    [ObservableProperty]
    private TransactionFilterOption? selectedWalletOption;

    [ObservableProperty]
    private TransactionTypeOption? selectedTypeOption;

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        isLoadingFilters = true;

        var periods = await apiClient.GetPeriodsAsync();
        PeriodOptions.Clear();
        PeriodOptions.Add(new TransactionFilterOption(null, "All periods"));
        foreach (var period in periods)
        {
            PeriodOptions.Add(new TransactionFilterOption(period.Id, period.Name ?? period.StartDate.ToString("d")));
        }

        var wallets = await apiClient.GetWalletsAsync();
        WalletOptions.Clear();
        WalletOptions.Add(new TransactionFilterOption(null, "All wallets"));
        foreach (var wallet in wallets)
        {
            WalletOptions.Add(new TransactionFilterOption(wallet.Id, wallet.Name));
        }

        var openPeriod = periods.FirstOrDefault(p => p.IsOpen);
        SelectedPeriodOption = openPeriod is not null
            ? PeriodOptions.First(o => o.Value == openPeriod.Id)
            : PeriodOptions[0];
        SelectedWalletOption ??= WalletOptions[0];
        SelectedTypeOption ??= TypeOptions[0];

        isLoadingFilters = false;

        await ReloadTransactionsAsync();
    });

    partial void OnSelectedPeriodOptionChanged(TransactionFilterOption? value) => ReloadIfReady();

    partial void OnSelectedWalletOptionChanged(TransactionFilterOption? value) => ReloadIfReady();

    partial void OnSelectedTypeOptionChanged(TransactionTypeOption? value) => ReloadIfReady();

    private void ReloadIfReady()
    {
        if (!isLoadingFilters)
        {
            _ = ExecuteSafelyAsync(ReloadTransactionsAsync);
        }
    }

    private async Task ReloadTransactionsAsync()
    {
        var query = new TransactionListQuery(
            SelectedPeriodOption?.Value,
            SelectedWalletOption?.Value,
            SelectedTypeOption?.Value);

        var transactions = await apiClient.GetTransactionsAsync(query);
        Transactions.Clear();
        foreach (var transaction in transactions)
        {
            Transactions.Add(transaction);
        }
    }
}
