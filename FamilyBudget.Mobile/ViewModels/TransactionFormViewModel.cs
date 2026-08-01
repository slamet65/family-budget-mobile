using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

[QueryProperty(nameof(TransactionIdRaw), "transactionId")]
public partial class TransactionFormViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
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

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));

        Wallets.Clear();
        foreach (var wallet in await apiClient.GetWalletsAsync())
        {
            Wallets.Add(wallet);
        }

        var categories = await apiClient.GetCategoriesAsync();
        CategoryOptions.Clear();
        foreach (var category in categories)
        {
            var label = category.ParentId is { } parentId
                ? $"{categories.FirstOrDefault(c => c.Id == parentId)?.Name} > {category.Name}"
                : category.Name;
            CategoryOptions.Add(new CategoryPickerOption(category.Id, label));
        }

        CurrentPeriod = await apiClient.GetCurrentPeriodAsync();
        HasOpenPeriod = CurrentPeriod is not null;
        OnPropertyChanged(nameof(MinimumOccurredAt));

        if (IsEditMode)
        {
            var transaction = await apiClient.GetTransactionAsync(int.Parse(TransactionIdRaw!));
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
        if (!long.TryParse(AmountText, out var amount) || amount <= 0)
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
        var id = IsEditMode ? int.Parse(TransactionIdRaw!) : 0;

        switch (SelectedType)
        {
            case "income" when ToWallet is not null:
                var incomeRequest = new CreateIncomeRequest(ToWallet.Id, amount, occurredAtUtc, note);
                await (IsEditMode ? apiClient.UpdateIncomeAsync(id, incomeRequest) : apiClient.CreateIncomeAsync(incomeRequest));
                break;
            case "expense" when FromWallet is not null && SelectedCategoryOption is not null:
                var expenseRequest = new CreateExpenseRequest(FromWallet.Id, SelectedCategoryOption.Id, amount, occurredAtUtc, note);
                await (IsEditMode ? apiClient.UpdateExpenseAsync(id, expenseRequest) : apiClient.CreateExpenseAsync(expenseRequest));
                break;
            case "transfer" when FromWallet is not null && ToWallet is not null:
                if (FromWallet.Id == ToWallet.Id)
                {
                    await feedback.ShowErrorDialogAsync("Dompet asal dan tujuan harus berbeda.");
                    return;
                }
                var transferRequest = new CreateTransferRequest(FromWallet.Id, ToWallet.Id, amount, occurredAtUtc, note);
                await (IsEditMode ? apiClient.UpdateTransferAsync(id, transferRequest) : apiClient.CreateTransferAsync(transferRequest));
                break;
            default:
                await feedback.ShowErrorDialogAsync("Lengkapi semua kolom yang wajib diisi.");
                return;
        }

        await Shell.Current.GoToAsync("..");
    });
}
