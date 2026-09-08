using FamilyBudget.Mobile.ViewModels;

namespace FamilyBudget.Mobile.Views;

public partial class SavingExpenseFormPage : ContentPage
{
    private readonly SavingExpenseFormViewModel viewModel;
    public SavingExpenseFormPage(SavingExpenseFormViewModel viewModel)
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
