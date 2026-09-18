using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;
using FamilyBudget.Mobile.Services.Local;
using FamilyBudget.Mobile.Services.Sync;
using Microsoft.Maui.Networking;

namespace FamilyBudget.Mobile.ViewModels;

public partial class SavingsViewModel(IDomainRepository repository, ISyncCoordinator sync,
    IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    public ObservableCollection<SavingDto> Savings { get; } = [];

    [ObservableProperty]
    private long totalBalance;

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        var savings = await repository.GetSavingsAsync();
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            await sync.SynchronizeAsync();
            savings = await repository.GetSavingsAsync();
        }
        Savings.Clear();
        foreach (var saving in savings)
        {
            Savings.Add(saving);
        }
        TotalBalance = savings.Sum(s => s.Balance);
    }, background: true);
}
