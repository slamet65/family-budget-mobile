using FamilyBudget.Mobile.ViewModels;

namespace FamilyBudget.Mobile.Views;

public partial class TransactionsPage : ContentPage
{
    private readonly TransactionsViewModel viewModel;

    public TransactionsPage(TransactionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.LoadCommand.Execute(null);
    }

    private static async void OnAddTransactionTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("transactionForm");
}
