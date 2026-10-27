using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.Common;
using FamilyBudget.Mobile.ViewModels.Base;
using FamilyBudget.Mobile.Services.Local;

namespace FamilyBudget.Mobile.ViewModels;

// Online-only, like the saving form: debt master data isn't queued offline.
[QueryProperty(nameof(DebtIdRaw), "debtId")]
public partial class DebtFormViewModel(IApiClient apiClient, IDomainRepository repository,
    IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    [ObservableProperty] private string? debtIdRaw;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string? note;
    [ObservableProperty] private string openingBalanceText = "0";
    [ObservableProperty] private DateTime openingBalanceDate = DateTime.Today;

    public bool IsEditMode => int.TryParse(DebtIdRaw, out _);
    public string PageTitle => IsEditMode ? "Ubah Utang" : "Utang Baru";

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
        if (!IsEditMode) return;

        var debt = await repository.GetDebtAsync(int.Parse(DebtIdRaw!));
        if (debt is null)
        {
            await feedback.ShowErrorDialogAsync("Data utang belum tersedia di perangkat.");
            return;
        }
        Name = debt.Name;
        Note = debt.Note;
        OpeningBalanceText = debt.OpeningBalance.ToString();
        OpeningBalanceDate = debt.OpeningBalanceDate?.UtcDateTime.Date ?? DateTime.Today;
    });

    [RelayCommand]
    private Task SaveAsync() => ExecuteSafelyAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await feedback.ShowErrorDialogAsync("Masukkan nama utang.");
            return;
        }
        if (!NumberInput.TryParseLong(OpeningBalanceText, out var openingBalance) || openingBalance < 0)
        {
            await feedback.ShowErrorDialogAsync("Masukkan sisa utang awal yang valid.");
            return;
        }

        var date = openingBalance > 0
            ? new DateTimeOffset(OpeningBalanceDate.Year, OpeningBalanceDate.Month, OpeningBalanceDate.Day, 0, 0, 0, TimeSpan.Zero)
            : (DateTimeOffset?)null;
        var cleanNote = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
        if (IsEditMode)
        {
            await apiClient.UpdateDebtAsync(int.Parse(DebtIdRaw!),
                new UpdateDebtRequest(Name.Trim(), cleanNote, openingBalance, date));
        }
        else
        {
            await apiClient.CreateDebtAsync(new CreateDebtRequest(Name.Trim(), cleanNote, openingBalance, date));
        }
        await Shell.Current.GoToAsync("..");
    });
}
