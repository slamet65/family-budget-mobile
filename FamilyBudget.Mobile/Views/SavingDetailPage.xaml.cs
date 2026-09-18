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
    private async void OnAddTransactionTapped(object? sender, EventArgs e)
    {
        var action = await DisplayActionSheetAsync("Tambah transaksi", "Batal", null,
            "Setor dari dompet", "Tarik ke dompet", "Pengeluaran langsung", "Transfer antar-tabungan");
        var type = action switch
        {
            "Setor dari dompet" => "deposit",
            "Tarik ke dompet" => "withdrawal",
            "Pengeluaran langsung" => "expense",
            "Transfer antar-tabungan" => "transfer",
            _ => null,
        };
        if (type is not null)
            await Shell.Current.GoToAsync($"savingExpenseForm?savingId={viewModel.SavingId}&type={type}");
    }
    private async void OnTransactionTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is SavingTransactionDto transaction)
            await viewModel.HandleTransactionAsync(transaction);
    }
}
