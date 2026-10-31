using FamilyBudget.Mobile.Common;
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
        DesktopToolbar.AddRefresh(this, viewModel.LoadCommand);
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
        // SwipeItem (touch) and MenuFlyoutItem (right-click on desktop) are both MenuItems.
        if (sender is MenuItem { CommandParameter: TransactionDto transaction })
            await viewModel.CancelPendingAsync(transaction);
    }
}
