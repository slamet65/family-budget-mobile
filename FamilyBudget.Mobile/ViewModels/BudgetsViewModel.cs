using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class BudgetsViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    private bool isLoadingPeriods;
    private int? catchAllCategoryId;

    public ObservableCollection<PeriodDto> Periods { get; } = [];

    public ObservableCollection<BudgetDisplayItem> Budgets { get; } = [];

    [ObservableProperty]
    private PeriodDto? selectedPeriod;

    [ObservableProperty]
    private long totalPlanned;

    [ObservableProperty]
    private long totalActual;

    [ObservableProperty]
    private long totalRemaining;

    public bool IsPeriodOpen => SelectedPeriod?.IsOpen ?? false;

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        isLoadingPeriods = true;

        var categories = await apiClient.GetCategoriesAsync();
        catchAllCategoryId = categories.FirstOrDefault(c => c.IsCatchAll)?.Id;

        var periods = await apiClient.GetPeriodsAsync();
        Periods.Clear();
        foreach (var period in periods)
        {
            Periods.Add(period);
        }

        SelectedPeriod = periods.FirstOrDefault(p => p.IsOpen) ?? periods.FirstOrDefault();

        isLoadingPeriods = false;

        await ReloadBudgetsAsync();
    });

    partial void OnSelectedPeriodChanged(PeriodDto? value)
    {
        OnPropertyChanged(nameof(IsPeriodOpen));
        if (!isLoadingPeriods)
        {
            _ = ExecuteSafelyAsync(ReloadBudgetsAsync);
        }
    }

    private async Task ReloadBudgetsAsync()
    {
        Budgets.Clear();
        if (SelectedPeriod is null)
        {
            TotalPlanned = TotalActual = TotalRemaining = 0;
            return;
        }

        var budgets = await apiClient.GetBudgetsAsync(SelectedPeriod.Id);
        foreach (var budget in budgets)
        {
            Budgets.Add(new BudgetDisplayItem(budget, budget.CategoryId == catchAllCategoryId));
        }

        // Exclude the catch-all category from the summary totals: its plannedAmount is
        // derived from total wallet balance minus every other category's plan, so including
        // it would make "Total Planned" trivially equal the wallet balance every time --
        // not a useful "sum of what's been planned" figure.
        var plannedBudgets = budgets.Where(b => b.CategoryId != catchAllCategoryId);
        TotalPlanned = plannedBudgets.Sum(b => b.PlannedAmount);
        TotalActual = plannedBudgets.Sum(b => b.ActualAmount);
        TotalRemaining = TotalPlanned - TotalActual;
    }
}
