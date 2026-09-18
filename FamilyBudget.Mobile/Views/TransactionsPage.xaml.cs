using FamilyBudget.Mobile.Services.Api.Dtos;
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

    private async void OnTransactionTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is TransactionDto transaction) await viewModel.HandleTransactionAsync(transaction);
    }

    private async void OnCancelPendingInvoked(object? sender, EventArgs e)
    {
        if (sender is SwipeItem { CommandParameter: TransactionDto transaction })
            await viewModel.CancelPendingAsync(transaction);
    }
}
