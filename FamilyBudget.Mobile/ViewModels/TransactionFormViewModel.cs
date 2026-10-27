using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.Common;
using FamilyBudget.Mobile.Services.Local;
using FamilyBudget.Mobile.Services.Sync;
using Microsoft.Maui.Networking;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

[QueryProperty(nameof(TransactionIdRaw), "transactionId")]
public partial class TransactionFormViewModel(ILedgerRepository ledgerRepository,
    IReferenceDataRepository referenceRepository, IDomainRepository domainRepository, IOutboxSyncService outbox,
    IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    private TransactionDto? loadedTransaction;
    // Debt-mapped categories: an expense there pays the debt off.
    private Dictionary<int, DebtDto> debtsByCategoryId = [];
    public ObservableCollection<WalletDto> Wallets { get; } = [];

    public ObservableCollection<CategoryPickerOption> CategoryOptions { get; } = [];

    [ObservableProperty]
    private string? transactionIdRaw;

    [ObservableProperty]
    private PeriodDto? currentPeriod;

    [ObservableProperty]
    private bool hasOpenPeriod;

    [ObservableProperty]
    private string selectedType = "expense";

    [ObservableProperty]
    private WalletDto? fromWallet;

    [ObservableProperty]
    private WalletDto? toWallet;

    [ObservableProperty]
    private CategoryPickerOption? selectedCategoryOption;

    [ObservableProperty]
    private string amountText = string.Empty;

    [ObservableProperty]
    private DateTime occurredAt = DateTime.Today;

    [ObservableProperty]
    private string? note;

    public DateTime MinimumOccurredAt => CurrentPeriod?.StartDate.Date ?? DateTime.Today;

    public bool IsEditMode => int.TryParse(TransactionIdRaw, out _);

    public string PageTitle => IsEditMode ? "Ubah Transaksi" : "Tambah Transaksi";

    private DebtDto? SelectedDebt => SelectedType == "expense" && SelectedCategoryOption is { } option
        && debtsByCategoryId.TryGetValue(option.Id, out var debt) ? debt : null;

    // Remaining debt before this transaction; when editing, the payment it already made is added back.
    private long RemainingBeforeThis => SelectedDebt is not { } debt ? 0
        : debt.Balance + (loadedTransaction is { Type: "expense", CategoryId: { } loadedCategoryId }
            && debtsByCategoryId.TryGetValue(loadedCategoryId, out var loadedDebt) && loadedDebt.Id == debt.Id
            ? loadedTransaction.Amount : 0);

    public string? DebtHint => SelectedDebt is { } debt
        ? RemainingBeforeThis < 0
            ? $"Membayar utang {debt.Name} · sudah lebih bayar Rp {-RemainingBeforeThis:N0}"
            : $"Membayar utang {debt.Name} · sisa Rp {RemainingBeforeThis:N0}"
        : null;
    public bool HasDebtHint => DebtHint is not null;

    // Overpaying is allowed by the API, so this is only a warning.
    public string? DebtOverpayWarning => SelectedDebt is not null
        && NumberInput.TryParseLong(AmountText, out var amount) && amount > RemainingBeforeThis
            ? $"Melebihi sisa utang. Kelebihan Rp {amount - Math.Max(RemainingBeforeThis, 0):N0} akan tercatat sebagai lebih bayar."
            : null;
    public bool HasDebtOverpayWarning => DebtOverpayWarning is not null;

    partial void OnSelectedCategoryOptionChanged(CategoryPickerOption? value) => NotifyDebtHint();
    partial void OnAmountTextChanged(string value) => NotifyDebtHint();
    partial void OnSelectedTypeChanged(string value) => NotifyDebtHint();

    private void NotifyDebtHint()
    {
        OnPropertyChanged(nameof(DebtHint));
        OnPropertyChanged(nameof(HasDebtHint));
        OnPropertyChanged(nameof(DebtOverpayWarning));
        OnPropertyChanged(nameof(HasDebtOverpayWarning));
    }

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));

        var wallets = await ledgerRepository.GetWalletsAsync();
        var categories = await referenceRepository.GetCachedCategoriesAsync();
        var periods = await ledgerRepository.GetPeriodsAsync();
        if ((wallets.Count == 0 || categories.Count == 0 || periods.Count == 0)
            && Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            await ledgerRepository.RefreshAsync();
            await referenceRepository.RefreshCategoriesAsync();
            wallets = await ledgerRepository.GetWalletsAsync();
            categories = await referenceRepository.GetCachedCategoriesAsync();
            periods = await ledgerRepository.GetPeriodsAsync();
        }

        Wallets.Clear();
        foreach (var wallet in wallets)
        {
            Wallets.Add(wallet);
        }

        var debts = (await domainRepository.GetDebtsAsync()).ToDictionary(debt => debt.Id);
        debtsByCategoryId = categories
            .Where(category => category.DebtId is { } debtId && debts.ContainsKey(debtId))
            .ToDictionary(category => category.Id, category => debts[category.DebtId!.Value]);

        CategoryOptions.Clear();
        foreach (var category in categories)
        {
            var label = category.ParentId is { } parentId
                ? $"{categories.FirstOrDefault(c => c.Id == parentId)?.Name} > {category.Name}"
                : category.Name;
            if (category.SavingName is { Length: > 0 } savingName)
            {
                label += $" · Tabungan {savingName}";
            }
            if (category.DebtName is { Length: > 0 } debtName)
            {
                label += $" · Utang {debtName}";
            }
            CategoryOptions.Add(new CategoryPickerOption(category.Id, label));
        }

        CurrentPeriod = periods.FirstOrDefault(period => period.IsOpen);
        HasOpenPeriod = CurrentPeriod is not null;
        OnPropertyChanged(nameof(MinimumOccurredAt));

        if (IsEditMode)
        {
            var transaction = await ledgerRepository.GetTransactionAsync(int.Parse(TransactionIdRaw!));
            if (transaction is null)
                throw new InvalidOperationException("Transaksi tidak tersedia di cache lokal. Muat ulang data lalu coba lagi.");
            loadedTransaction = transaction;
            SelectedType = transaction.Type;
            FromWallet = transaction.FromWalletId is { } fromWalletId ? Wallets.FirstOrDefault(w => w.Id == fromWalletId) : null;
            ToWallet = transaction.ToWalletId is { } toWalletId ? Wallets.FirstOrDefault(w => w.Id == toWalletId) : null;
            SelectedCategoryOption = transaction.CategoryId is { } categoryId ? CategoryOptions.FirstOrDefault(c => c.Id == categoryId) : null;
            AmountText = transaction.Amount.ToString();
            // .UtcDateTime, not .Date -- occurredAt is always built/stored as UTC midnight (see
            // the comment in SaveAsync), so this recovers the same calendar date that was saved.
            OccurredAt = transaction.OccurredAt.UtcDateTime.Date;
            Note = transaction.Note;
        }
        else
        {
            OccurredAt = DateTime.Today < MinimumOccurredAt ? MinimumOccurredAt : DateTime.Today;
        }
        NotifyDebtHint();
    });

    [RelayCommand]
    private void SetType(string type)
    {
        // The API rejects changing a transaction's type on PUT -- delete and recreate instead.
        if (!IsEditMode)
        {
            SelectedType = type;
        }
    }

    [RelayCommand]
    private Task SaveAsync() => ExecuteSafelyAsync(async () =>
    {
        if (!NumberInput.TryParseLong(AmountText, out var amount) || amount <= 0)
        {
            await feedback.ShowErrorDialogAsync("Masukkan jumlah yang valid.");
            return;
        }

        // Built as UTC midnight of the selected calendar date, not a local-time conversion --
        // the API compares occurredAt to the period's UTC startDate at day granularity, and a
        // naive DateTime->DateTimeOffset conversion applies the device's local offset, which can
        // shift the date across midnight UTC and wrongly land before the period start.
        var occurredAtUtc = new DateTimeOffset(OccurredAt.Year, OccurredAt.Month, OccurredAt.Day, 0, 0, 0, TimeSpan.Zero);
        var note = string.IsNullOrWhiteSpace(Note) ? null : Note;
        var periodId = IsEditMode ? loadedTransaction?.PeriodId : CurrentPeriod?.Id;
        if (periodId is null)
        {
            await feedback.ShowErrorDialogAsync("Tidak ada periode aktif di cache. Hubungkan internet dan muat ulang data.");
            return;
        }
        var input = SelectedType switch
        {
            "income" when ToWallet is not null => new PendingTransactionInput(periodId.Value, "income",
                null, null, ToWallet.Id, ToWallet.Name, null, null, amount, note, occurredAtUtc),
            "expense" when FromWallet is not null && SelectedCategoryOption is not null =>
                new PendingTransactionInput(periodId.Value, "expense", FromWallet.Id, FromWallet.Name,
                    null, null, SelectedCategoryOption.Id, SelectedCategoryOption.Label, amount, note, occurredAtUtc),
            "transfer" when FromWallet is not null && ToWallet is not null && FromWallet.Id != ToWallet.Id =>
                new PendingTransactionInput(periodId.Value, "transfer", FromWallet.Id, FromWallet.Name,
                    ToWallet.Id, ToWallet.Name, null, null, amount, note, occurredAtUtc),
            _ => null,
        };
        if (SelectedType == "transfer" && FromWallet?.Id == ToWallet?.Id)
        {
            await feedback.ShowErrorDialogAsync("Dompet asal dan tujuan harus berbeda.");
            return;
        }
        if (input is null)
        {
            await feedback.ShowErrorDialogAsync("Lengkapi semua kolom yang wajib diisi.");
            return;
        }

        if (!IsEditMode)
        {
            await outbox.EnqueueAsync(input);
            await Shell.Current.GoToAsync("..");
            return;
        }

        if (loadedTransaction is null)
        {
            await feedback.ShowErrorDialogAsync("Transaksi tidak tersedia di cache lokal. Muat ulang data lalu coba lagi.");
            return;
        }
        await outbox.EnqueueUpdateAsync(loadedTransaction, input);
        await Shell.Current.GoToAsync("..");
    });

    [RelayCommand]
    private Task DeleteAsync() => ExecuteSafelyAsync(async () =>
    {
        if (!IsEditMode) return;
        var confirmed = await feedback.ShowConfirmationAsync(
            "Hapus transaksi", "Transaksi ini akan dihapus. Setoran tabungan atau pembayaran utang terkait juga akan disesuaikan.", "Hapus", "Batal");
        if (!confirmed) return;
        if (loadedTransaction is null)
        {
            await feedback.ShowErrorDialogAsync("Transaksi tidak tersedia di cache lokal. Muat ulang data lalu coba lagi.");
            return;
        }
        await outbox.EnqueueDeleteAsync(loadedTransaction);
        await Shell.Current.GoToAsync("..");
    });
}
