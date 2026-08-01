using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class PeriodsViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    public ObservableCollection<PeriodDto> Periods { get; } = [];

    [ObservableProperty]
    private bool hasOpenPeriod;

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        var periods = await apiClient.GetPeriodsAsync();
        Periods.Clear();
        foreach (var period in periods)
        {
            Periods.Add(period);
        }
        HasOpenPeriod = periods.Any(p => p.IsOpen);
    });

    [RelayCommand]
    private async Task ClosePeriodAsync(PeriodDto period)
    {
        var name = period.Name ?? period.StartDate.ToString("d");
        await Shell.Current.GoToAsync($"periodClose?periodId={period.Id}&periodName={Uri.EscapeDataString(name)}");
    }
}
