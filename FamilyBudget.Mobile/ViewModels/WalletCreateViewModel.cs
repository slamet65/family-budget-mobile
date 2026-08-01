using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class WalletCreateViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    [ObservableProperty]
    private string name = string.Empty;

    [RelayCommand]
    private Task SaveAsync() => ExecuteSafelyAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await feedback.ShowErrorDialogAsync("Enter a wallet name.");
            return;
        }

        await apiClient.CreateWalletAsync(new CreateWalletRequest(Name.Trim()));
        await Shell.Current.GoToAsync("..");
    });
}
