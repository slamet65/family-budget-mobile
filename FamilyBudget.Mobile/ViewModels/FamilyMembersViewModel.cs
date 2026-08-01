using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Auth;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class FamilyMembersViewModel(IApiClient apiClient, IAuthService authService, IUserFeedbackService feedback)
    : ViewModelBase(feedback)
{
    public ObservableCollection<UserDto> Members { get; } = [];

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        var users = await apiClient.GetUsersAsync();
        var ownUserId = authService.CurrentUser?.Id;

        Members.Clear();
        foreach (var user in users.Where(u => u.Id != ownUserId))
        {
            Members.Add(user);
        }
    });
}
