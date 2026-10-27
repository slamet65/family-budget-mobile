using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.ViewModels;

namespace FamilyBudget.Mobile.Views;

public partial class DebtDetailPage : ContentPage
{
    private readonly DebtDetailViewModel viewModel;
    public DebtDetailPage(DebtDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.LoadCommand.Execute(null);
    }
    private async void OnEditDebtTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync($"debtForm?debtId={viewModel.DebtId}");
    private async void OnAddTransactionTapped(object? sender, EventArgs e)
    {
        // Regular installments aren't here: they're expenses in the debt's category.
        var action = await DisplayActionSheetAsync("Catat mutasi utang", "Batal", null,
            "Tambah utang", "Bayar dari tabungan", "Pengurangan");
        var type = action switch
        {
            "Tambah utang" => "increase",
            "Bayar dari tabungan" => "saving_payment",
            "Pengurangan" => "reduction",
            _ => null,
        };
        if (type is not null)
            await Shell.Current.GoToAsync($"debtTransactionForm?debtId={viewModel.DebtId}&type={type}");
    }
    private async void OnTransactionTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is DebtTransactionDto transaction)
            await viewModel.HandleTransactionAsync(transaction);
    }
}
