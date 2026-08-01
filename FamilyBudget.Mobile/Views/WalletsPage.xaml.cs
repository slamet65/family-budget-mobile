using FamilyBudget.Mobile.ViewModels;

namespace FamilyBudget.Mobile.Views;

public partial class WalletsPage : ContentPage
{
    private readonly WalletsViewModel viewModel;

    public WalletsPage(WalletsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.LoadCommand.Execute(null);
    }

    private static async void OnAddWalletTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("walletCreate");
}
