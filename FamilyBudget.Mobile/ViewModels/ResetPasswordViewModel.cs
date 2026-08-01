using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

[QueryProperty(nameof(UserId), "userId")]
[QueryProperty(nameof(UserName), "userName")]
public partial class ResetPasswordViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    [ObservableProperty]
    private int userId;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string newPassword = string.Empty;

    [RelayCommand]
    private Task SaveAsync() => ExecuteSafelyAsync(async () =>
    {
        if (NewPassword.Length < 8)
        {
            await feedback.ShowErrorDialogAsync("Password must be at least 8 characters.");
            return;
        }

        var confirmed = await feedback.ShowConfirmationAsync(
            "Reset password?", $"{UserName}'s current password will stop working immediately.", "Reset", "Cancel");
        if (!confirmed)
        {
            return;
        }

        await apiClient.ResetPasswordAsync(UserId, new ResetPasswordRequest(NewPassword));
        await feedback.ShowInfoDialogAsync("Password reset", $"{UserName} can now log in with the new password.");
        await Shell.Current.GoToAsync("..");
    });
}
