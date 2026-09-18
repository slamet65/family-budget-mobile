using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Auth;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;
using FamilyBudget.Mobile.Services.Local;
using FamilyBudget.Mobile.Services.Sync;
using Microsoft.Maui.Networking;

namespace FamilyBudget.Mobile.ViewModels;

public partial class FamilyMembersViewModel(IDomainRepository repository, ISyncCoordinator sync,
    IAuthService authService, IUserFeedbackService feedback)
    : ViewModelBase(feedback)
{
    public ObservableCollection<UserDto> Members { get; } = [];

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        var users = await repository.GetUsersAsync();
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            await sync.SynchronizeAsync();
            users = await repository.GetUsersAsync();
        }
        var ownUserId = authService.CurrentUser?.Id;

        Members.Clear();
        foreach (var user in users.Where(u => u.Id != ownUserId))
        {
            Members.Add(user);
        }
    });
}
