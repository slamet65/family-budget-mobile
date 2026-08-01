using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class WalletsViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    public ObservableCollection<WalletDto> Wallets { get; } = [];

    [ObservableProperty]
    private PeriodDto? currentPeriod;

    [ObservableProperty]
    private bool hasOpenPeriod;

    [ObservableProperty]
    private long totalBalance;

    [ObservableProperty]
    private string periodStatusText = "No open period";

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        var wallets = await apiClient.GetWalletsAsync();
        Wallets.Clear();
        foreach (var wallet in wallets)
        {
            Wallets.Add(wallet);
        }
        TotalBalance = wallets.Sum(w => w.Balance);

        CurrentPeriod = await apiClient.GetCurrentPeriodAsync();
        HasOpenPeriod = CurrentPeriod is not null;
        PeriodStatusText = HasOpenPeriod ? "Current period is active" : "No open period";
    });
}
