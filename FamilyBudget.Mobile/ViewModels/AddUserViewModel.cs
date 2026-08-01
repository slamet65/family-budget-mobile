using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class AddUserViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [RelayCommand]
    private Task SaveAsync() => ExecuteSafelyAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email))
        {
            await feedback.ShowErrorDialogAsync("Enter a name and email.");
            return;
        }
        if (Password.Length < 8)
        {
            await feedback.ShowErrorDialogAsync("Password must be at least 8 characters.");
            return;
        }

        var created = await apiClient.CreateUserAsync(new CreateUserRequest(Name.Trim(), Email.Trim(), Password));
        await feedback.ShowInfoDialogAsync("User created", $"{created.Name} can now log in with this email and password.");
        await Shell.Current.GoToAsync("..");
    });
}
