using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

[QueryProperty(nameof(SavingIdRaw), "savingId")]
public partial class SavingFormViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    [ObservableProperty] private string? savingIdRaw;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string? note;
    [ObservableProperty] private string openingBalanceText = "0";
    [ObservableProperty] private DateTime openingBalanceDate = DateTime.Today;

    public bool IsEditMode => int.TryParse(SavingIdRaw, out _);
    public string PageTitle => IsEditMode ? "Ubah Tabungan" : "Tabungan Baru";

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
        if (!IsEditMode) return;

        var saving = await apiClient.GetSavingAsync(int.Parse(SavingIdRaw!));
        Name = saving.Name;
        Note = saving.Note;
        OpeningBalanceText = saving.OpeningBalance.ToString();
        OpeningBalanceDate = saving.OpeningBalanceDate?.UtcDateTime.Date ?? DateTime.Today;
    });

    [RelayCommand]
    private Task SaveAsync() => ExecuteSafelyAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await feedback.ShowErrorDialogAsync("Masukkan nama tabungan.");
            return;
        }
        if (!long.TryParse(OpeningBalanceText, out var openingBalance) || openingBalance < 0)
        {
            await feedback.ShowErrorDialogAsync("Masukkan saldo awal yang valid.");
            return;
        }

        var date = openingBalance > 0
            ? new DateTimeOffset(OpeningBalanceDate.Year, OpeningBalanceDate.Month, OpeningBalanceDate.Day, 0, 0, 0, TimeSpan.Zero)
            : (DateTimeOffset?)null;
        var cleanNote = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
        if (IsEditMode)
        {
            await apiClient.UpdateSavingAsync(int.Parse(SavingIdRaw!),
                new UpdateSavingRequest(Name.Trim(), cleanNote, openingBalance, date));
        }
        else
        {
            await apiClient.CreateSavingAsync(new CreateSavingRequest(Name.Trim(), cleanNote, openingBalance, date));
        }
        await Shell.Current.GoToAsync("..");
    });
}
