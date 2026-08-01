using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

[QueryProperty(nameof(PeriodId), "periodId")]
[QueryProperty(nameof(CategoryIdRaw), "categoryId")]
[QueryProperty(nameof(CategoryDisplayName), "categoryName")]
[QueryProperty(nameof(InitialPlannedAmountRaw), "plannedAmount")]
public partial class BudgetEditViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    public ObservableCollection<CategoryPickerOption> AvailableCategories { get; } = [];

    [ObservableProperty]
    private int periodId;

    [ObservableProperty]
    private string? categoryIdRaw;

    [ObservableProperty]
    private string? categoryDisplayName;

    [ObservableProperty]
    private string? initialPlannedAmountRaw;

    [ObservableProperty]
    private CategoryPickerOption? selectedCategoryOption;

    [ObservableProperty]
    private string plannedAmountText = string.Empty;

    public bool IsEditMode => int.TryParse(CategoryIdRaw, out _);

    public string PageTitle => IsEditMode ? "Ubah Anggaran" : "Tambah Anggaran";

    partial void OnInitialPlannedAmountRawChanged(string? value)
    {
        if (long.TryParse(value, out var amount))
        {
            PlannedAmountText = amount.ToString();
        }
    }

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
        if (IsEditMode)
        {
            return;
        }

        var categories = await apiClient.GetCategoriesAsync();
        var existingBudgets = await apiClient.GetBudgetsAsync(PeriodId);
        var budgetedCategoryIds = existingBudgets.Select(b => b.CategoryId).ToHashSet();

        // The catch-all ("Lain-lain") category's budget is derived server-side and can't be
        // set via PUT (rejected with a 400) -- excluded here rather than letting the user pick
        // it and hit that rejection on save.
        AvailableCategories.Clear();
        foreach (var category in categories.Where(c => !budgetedCategoryIds.Contains(c.Id) && !c.IsCatchAll))
        {
            var label = category.ParentId is { } parentId
                ? $"{categories.FirstOrDefault(c => c.Id == parentId)?.Name} > {category.Name}"
                : category.Name;
            AvailableCategories.Add(new CategoryPickerOption(category.Id, label));
        }
    });

    [RelayCommand]
    private Task SaveAsync() => ExecuteSafelyAsync(async () =>
    {
        if (!long.TryParse(PlannedAmountText, out var amount) || amount < 0)
        {
            await feedback.ShowErrorDialogAsync("Masukkan jumlah yang valid.");
            return;
        }

        int categoryId;
        if (IsEditMode)
        {
            categoryId = int.Parse(CategoryIdRaw!);
        }
        else if (SelectedCategoryOption is not null)
        {
            categoryId = SelectedCategoryOption.Id;
        }
        else
        {
            await feedback.ShowErrorDialogAsync("Pilih kategori.");
            return;
        }

        await apiClient.UpsertBudgetAsync(PeriodId, categoryId, new UpsertBudgetRequest(amount));
        await Shell.Current.GoToAsync("..");
    });
}
