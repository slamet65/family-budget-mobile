using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.ViewModels;

namespace FamilyBudget.Mobile.Views;

public partial class SavingsPage : ContentPage
{
    private readonly SavingsViewModel viewModel;
    public SavingsPage(SavingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.LoadCommand.Execute(null);
    }

    private static async void OnAddSavingTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("savingForm");

    private static async void OnSavingTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is SavingDto saving)
            await Shell.Current.GoToAsync($"savingDetail?savingId={saving.Id}");
    }
}
