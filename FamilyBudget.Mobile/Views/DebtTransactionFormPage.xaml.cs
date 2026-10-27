using FamilyBudget.Mobile.ViewModels;

namespace FamilyBudget.Mobile.Views;

public partial class DebtTransactionFormPage : ContentPage
{
    private readonly DebtTransactionFormViewModel viewModel;
    public DebtTransactionFormPage(DebtTransactionFormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.LoadCommand.Execute(null);
    }
}
