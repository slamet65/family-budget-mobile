using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class PeriodCreateViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    [ObservableProperty]
    private DateTime startDate = DateTime.Today;

    [ObservableProperty]
    private string? name;

    [RelayCommand]
    private Task SaveAsync() => ExecuteSafelyAsync(async () =>
    {
        // UTC-midnight construction, not a naive DateTime->DateTimeOffset conversion -- see
        // TransactionFormViewModel.SaveAsync for why (local-offset conversion can shift the
        // date across midnight UTC).
        var startDateUtc = new DateTimeOffset(StartDate.Year, StartDate.Month, StartDate.Day, 0, 0, 0, TimeSpan.Zero);
        var trimmedName = string.IsNullOrWhiteSpace(Name) ? null : Name.Trim();

        await apiClient.CreatePeriodAsync(new CreatePeriodRequest(startDateUtc, trimmedName));
        await Shell.Current.GoToAsync("..");
    });
}
