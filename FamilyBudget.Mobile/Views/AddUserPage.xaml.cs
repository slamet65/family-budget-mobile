using FamilyBudget.Mobile.ViewModels;

namespace FamilyBudget.Mobile.Views;

public partial class AddUserPage : ContentPage
{
    public AddUserPage(AddUserViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
