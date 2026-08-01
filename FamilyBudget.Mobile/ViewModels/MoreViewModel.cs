using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Auth;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class MoreViewModel(IApiClient apiClient, IAuthService authService, IUserFeedbackService feedback)
    : ViewModelBase(feedback)
{
    [ObservableProperty]
    private string userDisplay = authService.CurrentUser is { } user
        ? $"{user.Name} ({user.Email})"
        : string.Empty;

    [RelayCommand]
    private Task LogoutAsync() => ExecuteSafelyAsync(async () =>
    {
        try
        {
            await apiClient.LogoutAsync();
        }
        catch (ApiException)
        {
            // Best-effort: the local session is cleared regardless, so a dead/expired
            // token or unreachable server doesn't strand the user unable to log out.
        }

        await authService.ClearSessionAsync();
        await Shell.Current.GoToAsync("//login");
    });

    [RelayCommand]
    private Task GoToCategoriesAsync() => Shell.Current.GoToAsync("categories");

    [RelayCommand]
    private Task GoToPeriodsAsync() => Shell.Current.GoToAsync("periods");

    [RelayCommand]
    private Task GoToFamilyMembersAsync() => Shell.Current.GoToAsync("familyMembers");
}
