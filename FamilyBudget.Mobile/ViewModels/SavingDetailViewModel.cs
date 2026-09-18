using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;
using FamilyBudget.Mobile.Services.Local;
using FamilyBudget.Mobile.Services.Sync;
using Microsoft.Maui.Networking;

namespace FamilyBudget.Mobile.ViewModels;

[QueryProperty(nameof(SavingIdRaw), "savingId")]
public partial class SavingDetailViewModel(IDomainRepository repository, ISyncCoordinator sync, IOutboxSyncService outbox,
    IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    private List<SavingTransactionDto> allTransactions = [];
    public ObservableCollection<SavingTransactionDto> Transactions { get; } = [];

    [ObservableProperty] private string? savingIdRaw;
    [ObservableProperty] private SavingDetailDto? saving;
    [ObservableProperty] private string selectedFlow = "all";

    public int SavingId => int.TryParse(SavingIdRaw, out var id) ? id : 0;

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        if (SavingId == 0) return;
        Saving = await repository.GetSavingAsync(SavingId);
        var transactions = await repository.GetSavingTransactionsAsync(SavingId);
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            await sync.SynchronizeAsync();
            Saving = await repository.GetSavingAsync(SavingId);
            transactions = await repository.GetSavingTransactionsAsync(SavingId);
        }
        allTransactions = transactions.ToList();
        ApplyFilter();
    }, background: true);

    [RelayCommand]
    private void SetFlow(string flow) => SelectedFlow = flow;

    public async Task HandleTransactionAsync(SavingTransactionDto transaction)
    {
        if (transaction.SyncStatus == "failed" && transaction.LocalId is { } failedId)
        {
            if (transaction.SyncError?.Contains("perangkat lain", StringComparison.OrdinalIgnoreCase) == true)
            {
                var discard = await feedback.ShowConfirmationAsync("Konflik perubahan",
                    "Mutasi tabungan sudah berubah di perangkat lain. Perubahan lokal tetap tersimpan sampai Anda membatalkannya.",
                    "Batalkan lokal", "Nanti");
                if (discard) await outbox.CancelAsync(failedId);
                await LoadAsync();
                return;
            }
            var retry = await feedback.ShowConfirmationAsync("Sinkronisasi gagal",
                transaction.SyncError ?? "Mutasi tabungan ditolak server.", "Coba lagi", "Tutup");
            if (retry) await outbox.RetryAsync(failedId);
            await LoadAsync();
            return;
        }
        if (!transaction.IsSynced)
        {
            var cancel = await feedback.ShowConfirmationAsync("Belum tersinkron",
                "Mutasi tabungan tersimpan di perangkat dan akan dikirim saat koneksi tersedia.",
                "Batalkan lokal", "Tunggu");
            if (cancel && transaction.LocalId is { } localId) await outbox.CancelAsync(localId);
            outbox.TriggerSync();
            await LoadAsync();
            return;
        }
        if (!transaction.IsReadOnly)
            await Shell.Current.GoToAsync($"savingExpenseForm?transactionId={transaction.Id}");
    }

    partial void OnSelectedFlowChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Transactions.Clear();
        foreach (var transaction in allTransactions.Where(transaction => SelectedFlow switch
        {
            "in" => !transaction.IsOutgoing,
            "out" => transaction.IsOutgoing,
            _ => true,
        }))
        {
            Transactions.Add(transaction);
        }
    }
}
