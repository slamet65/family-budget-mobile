using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Common;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;
using FamilyBudget.Mobile.Services.Local;
using FamilyBudget.Mobile.Services.Sync;

namespace FamilyBudget.Mobile.ViewModels;

// Create-only: debt movements are immutable on the API (DEBT_TRANSACTION_IMMUTABLE);
// corrections are recorded as a new opposite movement.
[QueryProperty(nameof(DebtIdRaw), "debtId")]
[QueryProperty(nameof(TransactionTypeRaw), "type")]
public partial class DebtTransactionFormViewModel(IDomainRepository domain, IOutboxSyncService outbox,
    IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    private long remainingDebt;
    public ObservableCollection<SavingDto> Savings { get; } = [];

    [ObservableProperty] private string? debtIdRaw;
    [ObservableProperty] private string? transactionTypeRaw;
    [ObservableProperty] private string selectedType = "increase";
    [ObservableProperty] private SavingDto? selectedSaving;
    [ObservableProperty] private string amountText = string.Empty;
    [ObservableProperty] private DateTime occurredAt = DateTime.Today;
    [ObservableProperty] private string? note;
    [ObservableProperty] private string remainingText = string.Empty;

    public bool IsSavingPayment => SelectedType == "saving_payment";
    public bool PaysDown => SelectedType != "increase";
    public string TypeLabel => SelectedType switch
    {
        "saving_payment" => "Bayar dari Tabungan",
        "reduction" => "Pengurangan Utang",
        _ => "Tambah Utang",
    };
    public string TypeHint => SelectedType switch
    {
        "saving_payment" => "Saldo tabungan dan sisa utang sama-sama berkurang. Cicilan dari dompet dicatat sebagai pengeluaran di kategori utang.",
        "reduction" => "Mengurangi utang tanpa uang keluar, mis. diskon pelunasan atau utang dihapuskan.",
        _ => "Menambah utang tanpa uang masuk ke dompet, mis. bunga, denda, atau belanja paylater.",
    };

    // Overpaying is allowed (the API never rejects it), so this only warns.
    public string? OverpayWarning =>
        PaysDown && NumberInput.TryParseLong(AmountText, out var amount) && amount > remainingDebt
            ? $"Melebihi sisa utang. Kelebihan Rp {amount - Math.Max(remainingDebt, 0):N0} akan tercatat sebagai lebih bayar."
            : null;
    public bool HasOverpayWarning => OverpayWarning is not null;

    // The API does reject paying more than the saving holds.
    public string? SavingWarning =>
        IsSavingPayment && SelectedSaving is { } saving && NumberInput.TryParseLong(AmountText, out var amount)
            && amount > saving.Balance
            ? $"Saldo {saving.Name} hanya Rp {saving.Balance:N0}."
            : null;
    public bool HasSavingWarning => SavingWarning is not null;
    public string SavingBalanceText => SelectedSaving is { } saving ? $"Saldo: Rp {saving.Balance:N0}" : string.Empty;

    partial void OnSelectedTypeChanged(string value) => NotifyTypeProperties();
    partial void OnAmountTextChanged(string value) => NotifyWarnings();
    partial void OnSelectedSavingChanged(SavingDto? value)
    {
        OnPropertyChanged(nameof(SavingBalanceText));
        NotifyWarnings();
    }

    [RelayCommand]
    private void SetType(string type) => SelectedType = type;

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        if (!string.IsNullOrWhiteSpace(TransactionTypeRaw))
            SelectedType = TransactionTypeRaw;

        var debt = int.TryParse(DebtIdRaw, out var debtId) ? await domain.GetDebtAsync(debtId) : null;
        remainingDebt = debt?.Balance ?? 0;
        RemainingText = debt is null ? string.Empty
            : debt.Balance < 0 ? $"Lebih bayar Rp {-debt.Balance:N0}"
            : $"Sisa utang Rp {debt.Balance:N0}";

        Savings.Clear();
        foreach (var saving in await domain.GetSavingsAsync()) Savings.Add(saving);
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
        if (IsSavingPayment && SelectedSaving is null)
        {
            await feedback.ShowErrorDialogAsync("Pilih tabungan yang dipakai untuk membayar.");
            return;
        }
        if (HasSavingWarning)
        {
            await feedback.ShowErrorDialogAsync(SavingWarning!);
            return;
        }
        if (!int.TryParse(DebtIdRaw, out var debtId))
        {
            await feedback.ShowErrorDialogAsync("Utang tidak valid.");
            return;
        }

        var date = new DateTimeOffset(OccurredAt.Year, OccurredAt.Month, OccurredAt.Day, 0, 0, 0, TimeSpan.Zero);
        var note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
        await outbox.EnqueueDebtAsync(new PendingDebtTransactionInput(
            debtId, SelectedType, amount,
            IsSavingPayment ? SelectedSaving?.Id : null,
            IsSavingPayment ? SelectedSaving?.Name : null,
            note, date));
        await Shell.Current.GoToAsync("..");
    });

    private void NotifyTypeProperties()
    {
        OnPropertyChanged(nameof(IsSavingPayment));
        OnPropertyChanged(nameof(PaysDown));
        OnPropertyChanged(nameof(TypeLabel));
        OnPropertyChanged(nameof(TypeHint));
        NotifyWarnings();
    }

    private void NotifyWarnings()
    {
        OnPropertyChanged(nameof(OverpayWarning));
        OnPropertyChanged(nameof(HasOverpayWarning));
        OnPropertyChanged(nameof(SavingWarning));
        OnPropertyChanged(nameof(HasSavingWarning));
    }
}
