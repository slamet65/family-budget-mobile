using FamilyBudget.Mobile.ViewModels;
using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Views;

public partial class CategoriesPage : ContentPage
{
    private readonly CategoriesViewModel viewModel;

    public CategoriesPage(CategoriesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.LoadCommand.Execute(null);
    }

    private static async void OnAddCategoryTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("categoryCreate");

    private static async void OnCategoryTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is CategoryDto category)
        {
            await Shell.Current.GoToAsync($"categoryCreate?categoryId={category.Id}");
        }
    }
}
