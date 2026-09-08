using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class SavingsViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    public ObservableCollection<SavingDto> Savings { get; } = [];

    [ObservableProperty]
    private long totalBalance;

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        var savings = await apiClient.GetSavingsAsync();
        Savings.Clear();
        foreach (var saving in savings)
        {
            Savings.Add(saving);
        }
        TotalBalance = savings.Sum(s => s.Balance);
    }, background: true);
}
