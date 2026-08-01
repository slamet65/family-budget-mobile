using FamilyBudget.Mobile.ViewModels;

namespace FamilyBudget.Mobile.Views;

public partial class WalletCreatePage : ContentPage
{
    public WalletCreatePage(WalletCreateViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
