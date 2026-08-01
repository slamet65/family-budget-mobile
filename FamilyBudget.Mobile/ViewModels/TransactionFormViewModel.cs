using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class TransactionFormViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    public ObservableCollection<WalletDto> Wallets { get; } = [];

    public ObservableCollection<CategoryPickerOption> CategoryOptions { get; } = [];

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

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
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
        OccurredAt = DateTime.Today < MinimumOccurredAt ? MinimumOccurredAt : DateTime.Today;
        OnPropertyChanged(nameof(MinimumOccurredAt));
    });

    [RelayCommand]
    private void SetType(string type) => SelectedType = type;

    [RelayCommand]
    private Task SaveAsync() => ExecuteSafelyAsync(async () =>
    {
        if (!long.TryParse(AmountText, out var amount) || amount <= 0)
        {
            await feedback.ShowErrorDialogAsync("Enter a valid amount.");
            return;
        }

        // Built as UTC midnight of the selected calendar date, not a local-time conversion --
        // the API compares occurredAt to the period's UTC startDate at day granularity, and a
        // naive DateTime->DateTimeOffset conversion applies the device's local offset, which can
        // shift the date across midnight UTC and wrongly land before the period start.
        var occurredAtUtc = new DateTimeOffset(OccurredAt.Year, OccurredAt.Month, OccurredAt.Day, 0, 0, 0, TimeSpan.Zero);
        var note = string.IsNullOrWhiteSpace(Note) ? null : Note;

        switch (SelectedType)
        {
            case "income" when ToWallet is not null:
                await apiClient.CreateIncomeAsync(new CreateIncomeRequest(ToWallet.Id, amount, occurredAtUtc, note));
                break;
            case "expense" when FromWallet is not null && SelectedCategoryOption is not null:
                await apiClient.CreateExpenseAsync(new CreateExpenseRequest(FromWallet.Id, SelectedCategoryOption.Id, amount, occurredAtUtc, note));
                break;
            case "transfer" when FromWallet is not null && ToWallet is not null:
                if (FromWallet.Id == ToWallet.Id)
                {
                    await feedback.ShowErrorDialogAsync("Source and destination wallets must differ.");
                    return;
                }
                await apiClient.CreateTransferAsync(new CreateTransferRequest(FromWallet.Id, ToWallet.Id, amount, occurredAtUtc, note));
                break;
            default:
                await feedback.ShowErrorDialogAsync("Fill in all required fields.");
                return;
        }

        await Shell.Current.GoToAsync("..");
    });
}
