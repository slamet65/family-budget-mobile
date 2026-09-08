using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.ViewModels;

namespace FamilyBudget.Mobile.Views;

public partial class SavingDetailPage : ContentPage
{
    private readonly SavingDetailViewModel viewModel;
    public SavingDetailPage(SavingDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.LoadCommand.Execute(null);
    }
    private async void OnEditSavingTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync($"savingForm?savingId={viewModel.SavingId}");
    private async void OnAddExpenseTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync($"savingExpenseForm?savingId={viewModel.SavingId}");
    private static async void OnTransactionTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is SavingTransactionDto { IsExpense: true } transaction)
            await Shell.Current.GoToAsync($"savingExpenseForm?transactionId={transaction.Id}");
    }
}
