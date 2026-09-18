using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.Services.Local;
using Microsoft.Maui.Networking;
using FamilyBudget.Mobile.Services.Sync;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class TransactionsViewModel(ILedgerRepository repository, IOutboxSyncService outbox,
    ISyncCoordinator sync,
    IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    private bool isLoadingFilters;
    private bool filtersInitialized;
    private int queryVersion;

    public ObservableCollection<TransactionDto> Transactions { get; } = [];

    public ObservableCollection<TransactionFilterOption> PeriodOptions { get; } = [];

    public ObservableCollection<TransactionFilterOption> WalletOptions { get; } = [];

    public ObservableCollection<TransactionTypeOption> TypeOptions { get; } =
    [
        new(null, "Semua jenis"),
        new("income", "Pemasukan"),
        new("expense", "Pengeluaran"),
        new("transfer", "Transfer"),
        new("adjustment", "Penyesuaian"),
        new("saving_deposit", "Setor tabungan"),
        new("saving_withdrawal", "Tarik tabungan"),
    ];

    [ObservableProperty]
    private TransactionFilterOption? selectedPeriodOption;

    [ObservableProperty]
    private TransactionFilterOption? selectedWalletOption;

    [ObservableProperty]
    private TransactionTypeOption? selectedTypeOption;

    [ObservableProperty]
    private string cacheStatusText = "Belum ada data lokal";

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        await LoadCachedAsync();
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            CacheStatusText = "Offline — " + CacheStatusText;
            return;
        }
        try
        {
            await sync.SynchronizeAsync();
            await LoadCachedAsync();
        }
        catch (ApiException)
        {
            CacheStatusText = "Refresh gagal — " + CacheStatusText;
            throw;
        }
    }, background: true);

    private async Task LoadCachedAsync()
    {
        var periods = await repository.GetPeriodsAsync();
        var wallets = await repository.GetWalletsAsync();
        var refreshedAt = await repository.GetRefreshedAtAsync();
        isLoadingFilters = true;
        try
        {
            var previousPeriodId = SelectedPeriodOption?.Value;
            var previousWalletId = SelectedWalletOption?.Value;
            PeriodOptions.Clear();
            PeriodOptions.Add(new TransactionFilterOption(null, "Semua periode"));
            foreach (var period in periods.OrderByDescending(period => period.StartDate))
            {
                PeriodOptions.Add(new TransactionFilterOption(period.Id, period.Name ?? period.StartDate.ToString("d")));
            }

            WalletOptions.Clear();
            WalletOptions.Add(new TransactionFilterOption(null, "Semua dompet"));
            foreach (var wallet in wallets)
            {
                WalletOptions.Add(new TransactionFilterOption(wallet.Id, wallet.Name));
            }

            var openPeriod = periods.FirstOrDefault(p => p.IsOpen);
            var desiredPeriodId = filtersInitialized ? previousPeriodId : openPeriod?.Id;
            SelectedPeriodOption = PeriodOptions.FirstOrDefault(option => option.Value == desiredPeriodId) ?? PeriodOptions[0];
            SelectedWalletOption = WalletOptions.FirstOrDefault(option => option.Value == previousWalletId) ?? WalletOptions[0];
            SelectedTypeOption ??= TypeOptions[0];
            // An empty, never-loaded cache must not lock in the initial default filter.
            if (refreshedAt is not null) filtersInitialized = true;
        }
        finally
        {
            isLoadingFilters = false;
        }
        CacheStatusText = refreshedAt is { } time
            ? $"Data terakhir: {time.ToLocalTime():d MMM HH:mm}"
            : "Belum ada data lokal — hubungkan internet untuk memuat";
        await ReloadTransactionsAsync();
    }

    public async Task HandleTransactionAsync(TransactionDto transaction)
    {
        if (transaction.SyncStatus == "failed" && transaction.LocalId is { } failedId)
        {
            if (transaction.SyncError?.Contains("perangkat lain", StringComparison.OrdinalIgnoreCase) == true)
            {
                var discard = await feedback.ShowConfirmationAsync("Konflik perubahan",
                    "Transaksi sudah berubah di perangkat lain. Perubahan lokal Anda tetap disimpan. " +
                    "Batalkan perubahan lokal, muat ulang, lalu terapkan kembali edit pada versi terbaru.",
                    "Batalkan lokal", "Nanti");
                if (discard) await outbox.CancelAsync(failedId);
                await LoadCachedAsync();
                return;
            }
            var retry = await feedback.ShowConfirmationAsync("Sinkronisasi gagal",
                transaction.SyncError ?? "Transaksi ditolak server.", "Coba lagi", "Tutup");
            if (retry) await outbox.RetryAsync(failedId);
            await LoadCachedAsync();
            return;
        }
        if (!transaction.IsSynced)
        {
            await feedback.ShowInfoDialogAsync("Belum tersinkron",
                "Transaksi tersimpan di perangkat dan akan dikirim ketika koneksi tersedia.");
            outbox.TriggerSync();
            return;
        }
        if (transaction.Type is not ("adjustment" or "saving_deposit" or "saving_withdrawal"))
        {
            await Shell.Current.GoToAsync($"transactionForm?transactionId={transaction.Id}");
        }
    }

    public async Task CancelPendingAsync(TransactionDto transaction)
    {
        if (transaction.IsSynced || transaction.LocalId is not { } localId) return;
        var confirmed = await feedback.ShowConfirmationAsync("Batalkan transaksi lokal",
            "Transaksi yang belum tersinkron akan dihapus dari perangkat.", "Batalkan transaksi", "Kembali");
        if (!confirmed) return;
        await outbox.CancelAsync(localId);
        await LoadCachedAsync();
    }

    partial void OnSelectedPeriodOptionChanged(TransactionFilterOption? value) => ReloadIfReady();

    partial void OnSelectedWalletOptionChanged(TransactionFilterOption? value) => ReloadIfReady();

    partial void OnSelectedTypeOptionChanged(TransactionTypeOption? value) => ReloadIfReady();

    private void ReloadIfReady()
    {
        if (!isLoadingFilters)
        {
            // Filter queries remain local and may run while the network refresh is busy.
            _ = ReloadTransactionsAsync();
        }
    }

    private async Task ReloadTransactionsAsync()
    {
        var version = ++queryVersion;
        var query = new TransactionListQuery(
            SelectedPeriodOption?.Value,
            SelectedWalletOption?.Value,
            SelectedTypeOption?.Value);

        var transactions = await repository.GetTransactionsAsync(query);
        if (version != queryVersion) return;
        Transactions.Clear();
        foreach (var transaction in transactions)
        {
            Transactions.Add(transaction);
        }
    }
}
