using FamilyBudget.Mobile.Common;
using FamilyBudget.Mobile.ViewModels;

namespace FamilyBudget.Mobile.Views;

public partial class PeriodsPage : ContentPage
{
    private readonly PeriodsViewModel viewModel;

    public PeriodsPage(PeriodsViewModel viewModel)
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

    private static async void OnAddPeriodTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("periodCreate");
}
