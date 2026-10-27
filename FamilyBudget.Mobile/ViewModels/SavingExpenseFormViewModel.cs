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

// Create-only: saving movements are immutable on the API (SAVING_TRANSACTION_IMMUTABLE);
// corrections are recorded as a new opposite movement.
[QueryProperty(nameof(SavingIdRaw), "savingId")]
[QueryProperty(nameof(TransactionTypeRaw), "type")]
public partial class SavingExpenseFormViewModel(ILedgerRepository ledger, IDomainRepository domain,
    IReferenceDataRepository references, IOutboxSyncService outbox, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    private IReadOnlyList<FundingOption> fundableCategories = [];
    private IReadOnlyList<BudgetDto> openPeriodBudgets = [];
    public ObservableCollection<WalletDto> Wallets { get; } = [];
    public ObservableCollection<SavingDto> TargetSavings { get; } = [];
    public ObservableCollection<FundingOption> FundingOptions { get; } = [];

    [ObservableProperty] private string? savingIdRaw;
    [ObservableProperty] private string? transactionTypeRaw;
    [ObservableProperty] private string selectedType = "expense";
    [ObservableProperty] private WalletDto? selectedWallet;
    [ObservableProperty] private SavingDto? selectedTargetSaving;
    [ObservableProperty] private FundingOption? selectedFunding;
    [ObservableProperty] private string amountText = string.Empty;
    [ObservableProperty] private DateTime occurredAt = DateTime.Today;
    [ObservableProperty] private string? note;

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
    public string FundingLabel => IsDeposit ? "Kembalikan sisa anggaran" : "Dipakai untuk anggaran";
    public string FundingHint => (IsDeposit, SelectedFunding?.CategoryName) switch
    {
        (false, null) => "Dianggap pinjaman: anggaran tidak berubah. Kembalikan nanti lewat Setor.",
        (false, var name) => $"Anggaran {name} periode ini bertambah sebesar jumlah ini.",
        (true, null) => "Setoran biasa atau bayar pinjaman: anggaran tidak berubah.",
        (true, var name) => $"Dana dari tabungan untuk {name} periode ini berkurang sebesar jumlah ini.",
    };

    partial void OnSelectedTypeChanged(string value)
    {
        NotifyTypeProperties();
        RebuildFundingOptions(SelectedFunding?.CategoryId);
    }

    partial void OnSelectedFundingChanged(FundingOption? value) => OnPropertyChanged(nameof(FundingHint));

    [RelayCommand]
    private void SetType(string type) => SelectedType = type;

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        Wallets.Clear();
        foreach (var wallet in await ledger.GetWalletsAsync()) Wallets.Add(wallet);

        // Saving-mapped categories can't be funded from a saving (the API rejects it).
        var categories = await references.GetCachedCategoriesAsync();
        fundableCategories = categories
            .Where(category => category.SavingId is null)
            .Select(category => new FundingOption(category.Id, category.Name, category.ParentId is { } parentId
                ? $"{categories.FirstOrDefault(c => c.Id == parentId)?.Name} > {category.Name}"
                : category.Name))
            .ToList();
        var openPeriod = (await ledger.GetPeriodsAsync()).FirstOrDefault(period => period.IsOpen);
        openPeriodBudgets = openPeriod is null ? [] : await domain.GetBudgetsAsync(openPeriod.Id);

        var savings = await domain.GetSavingsAsync();
        if (!string.IsNullOrWhiteSpace(TransactionTypeRaw))
            SelectedType = TransactionTypeRaw;

        var savingId = int.TryParse(SavingIdRaw, out var parsedSavingId) ? parsedSavingId : 0;
        TargetSavings.Clear();
        foreach (var saving in savings.Where(s => s.Id != savingId)) TargetSavings.Add(saving);

        NotifyTypeProperties();
        RebuildFundingOptions(null);
    });

    // Withdrawal: any fundable category. Deposit: only categories that currently hold
    // savings funding this period, since returning more than that is rejected by the API.
    private void RebuildFundingOptions(int? keepCategoryId)
    {
        FundingOptions.Clear();
        FundingOptions.Add(new FundingOption(null, null,
            IsDeposit ? "Setor biasa / bayar pinjaman" : "Pinjam, dikembalikan nanti"));
        var options = IsDeposit
            ? openPeriodBudgets
                .Where(budget => budget.FundedAmount > 0)
                .Select(budget => new FundingOption(budget.CategoryId, budget.CategoryName,
                    $"{budget.CategoryName} · dana tabungan Rp {budget.FundedAmount:N0}"))
            : fundableCategories;
        foreach (var option in options) FundingOptions.Add(option);

        SelectedFunding = FundingOptions.FirstOrDefault(option => option.CategoryId == keepCategoryId) ?? FundingOptions[0];
    }

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
            date,
            NeedsWallet ? SelectedFunding?.CategoryId : null,
            NeedsWallet ? SelectedFunding?.CategoryName : null);

        await outbox.EnqueueSavingAsync(input);
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
        OnPropertyChanged(nameof(FundingLabel));
        OnPropertyChanged(nameof(FundingHint));
    }
}
