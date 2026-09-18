using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Common;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;
using FamilyBudget.Mobile.Services.Local;
using FamilyBudget.Mobile.Services.Sync;

namespace FamilyBudget.Mobile.ViewModels;

[QueryProperty(nameof(SavingIdRaw), "savingId")]
[QueryProperty(nameof(TransactionIdRaw), "transactionId")]
[QueryProperty(nameof(TransactionTypeRaw), "type")]
public partial class SavingExpenseFormViewModel(ILedgerRepository ledger,
    IDomainRepository domain, IOutboxSyncService outbox, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    private SavingTransactionDto? loadedTransaction;
    public ObservableCollection<WalletDto> Wallets { get; } = [];
    public ObservableCollection<SavingDto> TargetSavings { get; } = [];

    [ObservableProperty] private string? savingIdRaw;
    [ObservableProperty] private string? transactionIdRaw;
    [ObservableProperty] private string? transactionTypeRaw;
    [ObservableProperty] private string selectedType = "expense";
    [ObservableProperty] private WalletDto? selectedWallet;
    [ObservableProperty] private SavingDto? selectedTargetSaving;
    [ObservableProperty] private string amountText = string.Empty;
    [ObservableProperty] private DateTime occurredAt = DateTime.Today;
    [ObservableProperty] private string? note;

    public bool IsEditMode => int.TryParse(TransactionIdRaw, out _);
    public bool IsDeposit => SelectedType == "deposit";
    public bool IsWithdrawal => SelectedType == "withdrawal";
    public bool IsExpense => SelectedType == "expense";
    public bool IsTransfer => SelectedType == "transfer";
    public bool NeedsWallet => IsDeposit || IsWithdrawal;
    public string WalletLabel => IsDeposit ? "Dari dompet" : "Ke dompet";
    public string TypeLabel => SelectedType switch
    {
        "deposit" => "Setor Tabungan",
        "withdrawal" => "Tarik Tabungan",
        "transfer" => "Transfer Tabungan",
        _ => "Pengeluaran Tabungan",
    };
    public string PageTitle => IsEditMode ? $"Ubah {TypeLabel}" : TypeLabel;

    partial void OnSelectedTypeChanged(string value) => NotifyTypeProperties();

    [RelayCommand]
    private void SetType(string type)
    {
        if (!IsEditMode) SelectedType = type;
    }

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        Wallets.Clear();
        foreach (var wallet in await ledger.GetWalletsAsync()) Wallets.Add(wallet);

        var savings = await domain.GetSavingsAsync();
        SavingTransactionDto? transaction = null;
        if (IsEditMode)
        {
            transaction = await domain.GetSavingTransactionAsync(int.Parse(TransactionIdRaw!));
            if (transaction is null)
                throw new InvalidOperationException("Transaksi tabungan tidak tersedia di cache lokal.");
            loadedTransaction = transaction;
            SelectedType = transaction.Type switch
            {
                "transfer_in" or "transfer_out" => "transfer",
                _ => transaction.Type,
            };
            SavingIdRaw = transaction.Type == "transfer_in"
                ? transaction.RelatedSavingId?.ToString()
                : transaction.SavingId.ToString();
        }
        else if (!string.IsNullOrWhiteSpace(TransactionTypeRaw))
        {
            SelectedType = TransactionTypeRaw;
        }

        var savingId = int.TryParse(SavingIdRaw, out var parsedSavingId) ? parsedSavingId : 0;
        TargetSavings.Clear();
        foreach (var saving in savings.Where(s => s.Id != savingId)) TargetSavings.Add(saving);

        if (transaction is not null)
        {
            SelectedWallet = transaction.Type == "deposit"
                ? Wallets.FirstOrDefault(w => w.Id == transaction.FromWalletId)
                : Wallets.FirstOrDefault(w => w.Id == transaction.ToWalletId);
            var targetId = transaction.Type == "transfer_in" ? transaction.SavingId : transaction.RelatedSavingId;
            SelectedTargetSaving = TargetSavings.FirstOrDefault(s => s.Id == targetId);
            AmountText = transaction.Amount.ToString();
            OccurredAt = transaction.OccurredAt.UtcDateTime.Date;
            Note = transaction.Note;
        }

        NotifyTypeProperties();
    });

    [RelayCommand]
    private Task SaveAsync() => ExecuteSafelyAsync(async () =>
    {
        if (!NumberInput.TryParseLong(AmountText, out var amount) || amount <= 0)
        {
            await feedback.ShowErrorDialogAsync("Masukkan jumlah yang valid.");
            return;
        }
        if ((NeedsWallet && SelectedWallet is null) || (IsTransfer && SelectedTargetSaving is null))
        {
            await feedback.ShowErrorDialogAsync("Lengkapi semua kolom yang wajib diisi.");
            return;
        }

        var date = new DateTimeOffset(OccurredAt.Year, OccurredAt.Month, OccurredAt.Day, 0, 0, 0, TimeSpan.Zero);
        var note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
        if (!int.TryParse(SavingIdRaw, out var savingId))
        {
            await feedback.ShowErrorDialogAsync("Tabungan asal tidak valid.");
            return;
        }
        var input = new PendingSavingTransactionInput(
            savingId,
            SelectedType == "transfer" ? "transfer_out" : SelectedType,
            amount,
            IsDeposit ? SelectedWallet?.Id : null,
            IsDeposit ? SelectedWallet?.Name : null,
            IsWithdrawal ? SelectedWallet?.Id : null,
            IsWithdrawal ? SelectedWallet?.Name : null,
            IsTransfer ? SelectedTargetSaving?.Id : null,
            IsTransfer ? SelectedTargetSaving?.Name : null,
            note,
            date);

        if (IsEditMode)
        {
            if (loadedTransaction is null) throw new InvalidOperationException("Transaksi tabungan tidak tersedia di cache lokal.");
            await outbox.EnqueueSavingUpdateAsync(loadedTransaction, input);
        }
        else
            await outbox.EnqueueSavingAsync(input);

        await Shell.Current.GoToAsync("..");
    });

    [RelayCommand]
    private Task DeleteAsync() => ExecuteSafelyAsync(async () =>
    {
        if (!IsEditMode) return;
        var confirmed = await feedback.ShowConfirmationAsync(
            "Hapus transaksi", "Transaksi tabungan dan mutasi dompet terkait akan dihapus.", "Hapus", "Batal");
        if (!confirmed) return;
        if (loadedTransaction is null) throw new InvalidOperationException("Transaksi tabungan tidak tersedia di cache lokal.");
        await outbox.EnqueueSavingDeleteAsync(loadedTransaction);
        await Shell.Current.GoToAsync("..");
    });

    private void NotifyTypeProperties()
    {
        OnPropertyChanged(nameof(IsDeposit));
        OnPropertyChanged(nameof(IsWithdrawal));
        OnPropertyChanged(nameof(IsExpense));
        OnPropertyChanged(nameof(IsTransfer));
        OnPropertyChanged(nameof(NeedsWallet));
        OnPropertyChanged(nameof(WalletLabel));
        OnPropertyChanged(nameof(TypeLabel));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(IsEditMode));
    }
}
