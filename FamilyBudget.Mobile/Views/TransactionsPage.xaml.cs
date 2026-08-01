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

    // Adjustment rows are system-generated at tutup buku and have no matching create-form
    // fields to edit; the API also rejects editing them outright, so don't even navigate.
    private static async void OnTransactionTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not TransactionDto { Type: not "adjustment" } transaction)
        {
            return;
        }

        await Shell.Current.GoToAsync($"transactionForm?transactionId={transaction.Id}");
    }
}
