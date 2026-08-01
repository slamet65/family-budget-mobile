using FamilyBudget.Mobile.ViewModels;

namespace FamilyBudget.Mobile.Views;

public partial class BudgetsPage : ContentPage
{
    private readonly BudgetsViewModel viewModel;

    public BudgetsPage(BudgetsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.LoadCommand.Execute(null);
    }

    private async void OnBudgetTapped(object? sender, TappedEventArgs e)
    {
        // Catch-all ("Lain-lain") categories have a derived plannedAmount -- the API
        // rejects any attempt to PUT it directly, so there's nothing editable to navigate to.
        if (!viewModel.IsPeriodOpen || e.Parameter is not BudgetDisplayItem { IsCatchAll: false } item)
        {
            return;
        }

        var budget = item.Budget;
        await Shell.Current.GoToAsync(
            $"budgetEdit?periodId={budget.PeriodId}&categoryId={budget.CategoryId}" +
            $"&categoryName={Uri.EscapeDataString(budget.CategoryName)}&plannedAmount={budget.PlannedAmount}");
    }

    private async void OnAddBudgetTapped(object? sender, EventArgs e)
    {
        if (viewModel.SelectedPeriod is not { } period)
        {
            return;
        }

        await Shell.Current.GoToAsync($"budgetEdit?periodId={period.Id}");
    }
}
