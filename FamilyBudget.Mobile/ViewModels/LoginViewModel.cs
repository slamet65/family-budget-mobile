using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Auth;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class LoginViewModel(IApiClient apiClient, IAuthService authService, IUserFeedbackService feedback)
    : ViewModelBase(feedback)
{
    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [RelayCommand]
    private Task LoginAsync() => ExecuteSafelyAsync(async () =>
    {
        var response = await apiClient.LoginAsync(new LoginRequest(Email.Trim(), Password));
        await authService.SaveSessionAsync(response.Token, response.User);
        Password = string.Empty;
        await Shell.Current.GoToAsync("//main");
    });
}
