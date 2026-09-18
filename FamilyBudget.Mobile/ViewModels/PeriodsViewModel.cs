using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.Services.Local;
using FamilyBudget.Mobile.Services.Sync;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class PeriodsViewModel(IReferenceDataRepository repository, ISyncCoordinator sync,
    IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    public ObservableCollection<PeriodDto> Periods { get; } = [];

    [ObservableProperty]
    private bool hasOpenPeriod;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var cached = await repository.GetCachedPeriodsAsync();
        if (cached.Count > 0)
        {
            ReplacePeriods(cached);
        }

        await ExecuteSafelyAsync(async () =>
        {
            await sync.SynchronizeAsync();
            var fresh = await repository.GetCachedPeriodsAsync();
            ReplacePeriods(fresh);
        }, background: cached.Count > 0);
    }

    private void ReplacePeriods(IReadOnlyList<PeriodDto> periods)
    {
        Periods.Clear();
        foreach (var period in periods)
        {
            Periods.Add(period);
        }
        HasOpenPeriod = periods.Any(p => p.IsOpen);
    }

    [RelayCommand]
    private async Task ClosePeriodAsync(PeriodDto period)
    {
        var name = period.Name ?? period.StartDate.ToString("d");
        await Shell.Current.GoToAsync($"periodClose?periodId={period.Id}&periodName={Uri.EscapeDataString(name)}");
    }
}
