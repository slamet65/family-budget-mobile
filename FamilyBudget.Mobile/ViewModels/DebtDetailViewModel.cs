using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;
using FamilyBudget.Mobile.Services.Local;
using FamilyBudget.Mobile.Services.Sync;
using Microsoft.Maui.Networking;

namespace FamilyBudget.Mobile.ViewModels;

[QueryProperty(nameof(DebtIdRaw), "debtId")]
public partial class DebtDetailViewModel(IDomainRepository repository, ISyncCoordinator sync, IOutboxSyncService outbox,
    IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    public ObservableCollection<DebtTransactionDto> Transactions { get; } = [];

    [ObservableProperty] private string? debtIdRaw;
    [ObservableProperty] private DebtDetailDto? debt;

    public int DebtId => int.TryParse(DebtIdRaw, out var id) ? id : 0;

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        if (DebtId == 0) return;
        Debt = await repository.GetDebtAsync(DebtId);
        var transactions = await repository.GetDebtTransactionsAsync(DebtId);
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            await sync.SynchronizeAsync();
            Debt = await repository.GetDebtAsync(DebtId);
            transactions = await repository.GetDebtTransactionsAsync(DebtId);
        }
        Transactions.Clear();
        foreach (var transaction in transactions) Transactions.Add(transaction);
    }, background: true);

    public async Task HandleTransactionAsync(DebtTransactionDto transaction)
    {
        if (transaction.SyncStatus == "failed" && transaction.LocalId is { } failedId)
        {
            var retry = await feedback.ShowConfirmationAsync("Sinkronisasi gagal",
                transaction.SyncError ?? "Mutasi utang ditolak server.", "Coba lagi", "Batalkan lokal");
            if (retry) await outbox.RetryAsync(failedId);
            else await outbox.CancelAsync(failedId);
            await LoadAsync();
            return;
        }
        if (!transaction.IsSynced)
        {
            var cancel = await feedback.ShowConfirmationAsync("Belum tersinkron",
                "Mutasi utang tersimpan di perangkat dan akan dikirim saat koneksi tersedia.",
                "Batalkan lokal", "Tunggu");
            if (cancel && transaction.LocalId is { } localId) await outbox.CancelAsync(localId);
            outbox.TriggerSync();
            await LoadAsync();
            return;
        }
        // The API rejects every edit/delete of a debt movement (DEBT_TRANSACTION_IMMUTABLE),
        // so explain how to correct it instead of opening an edit form.
        var message = transaction.Type switch
        {
            "opening_balance" => "Sisa utang awal dapat dikoreksi lewat tombol Ubah di bagian atas halaman ini.",
            "payment" => "Pembayaran ini mengikuti transaksi pengeluaran asalnya. Ubah atau hapus transaksi tersebut di menu Transaksi.",
            "saving_payment" => "Mutasi utang tidak dapat diubah atau dihapus. Untuk mengoreksi, catat Tambah utang " +
                                "sebesar jumlah ini, lalu setor kembali uangnya ke tabungan.",
            _ => "Mutasi utang tidak dapat diubah atau dihapus. Untuk mengoreksi, catat mutasi kebalikannya: " +
                 "Pengurangan untuk membatalkan Tambah utang, atau sebaliknya.",
        };
        await feedback.ShowInfoDialogAsync("Mutasi utang", message);
    }
}
