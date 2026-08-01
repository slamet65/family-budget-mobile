using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.ViewModels;

namespace FamilyBudget.Mobile.Views;

public partial class FamilyMembersPage : ContentPage
{
    private readonly FamilyMembersViewModel viewModel;

    public FamilyMembersPage(FamilyMembersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.LoadCommand.Execute(null);
    }

    private static async void OnMemberTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not UserDto member)
        {
            return;
        }

        await Shell.Current.GoToAsync($"resetPassword?userId={member.Id}&userName={Uri.EscapeDataString(member.Name)}");
    }

    private static async void OnAddFamilyMemberTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("addUser");
}
