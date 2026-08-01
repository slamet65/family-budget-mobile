using FamilyBudget.Mobile.ViewModels;

namespace FamilyBudget.Mobile.Views;

public partial class PeriodCreatePage : ContentPage
{
    public PeriodCreatePage(PeriodCreateViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
