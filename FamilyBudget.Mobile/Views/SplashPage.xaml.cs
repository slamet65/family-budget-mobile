using FamilyBudget.Mobile.ViewModels;

namespace FamilyBudget.Mobile.Views;

public partial class SplashPage : ContentPage
{
    private readonly SplashViewModel viewModel;

    public SplashPage(SplashViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = viewModel.StartAsync();
    }
}
