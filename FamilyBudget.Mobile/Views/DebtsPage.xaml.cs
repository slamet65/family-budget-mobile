using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.ViewModels;

namespace FamilyBudget.Mobile.Views;

public partial class DebtsPage : ContentPage
{
    private readonly DebtsViewModel viewModel;
    public DebtsPage(DebtsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.LoadCommand.Execute(null);
    }

    private static async void OnAddDebtTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("debtForm");

    private static async void OnDebtTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is DebtDto debt)
            await Shell.Current.GoToAsync($"debtDetail?debtId={debt.Id}");
    }
}
