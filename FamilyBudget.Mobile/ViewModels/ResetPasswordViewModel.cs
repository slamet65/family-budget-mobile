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
            await feedback.ShowErrorDialogAsync("Kata sandi minimal 8 karakter.");
            return;
        }

        var confirmed = await feedback.ShowConfirmationAsync(
            "Atur ulang kata sandi?", $"Kata sandi {UserName} saat ini akan langsung berhenti berfungsi.", "Atur Ulang", "Batal");
        if (!confirmed)
        {
            return;
        }

        await apiClient.ResetPasswordAsync(UserId, new ResetPasswordRequest(NewPassword));
        await feedback.ShowInfoDialogAsync("Kata sandi diatur ulang", $"{UserName} sekarang bisa masuk dengan kata sandi baru.");
        await Shell.Current.GoToAsync("..");
    });
}
