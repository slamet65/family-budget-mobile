using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

[QueryProperty(nameof(SavingIdRaw), "savingId")]
[QueryProperty(nameof(TransactionIdRaw), "transactionId")]
public partial class SavingExpenseFormViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    [ObservableProperty] private string? savingIdRaw;
    [ObservableProperty] private string? transactionIdRaw;
    [ObservableProperty] private string amountText = string.Empty;
    [ObservableProperty] private DateTime occurredAt = DateTime.Today;
    [ObservableProperty] private string? note;

    public bool IsEditMode => int.TryParse(TransactionIdRaw, out _);
    public string PageTitle => IsEditMode ? "Ubah Pengeluaran" : "Pengeluaran Tabungan";

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
        if (!IsEditMode) return;
        var transaction = await apiClient.GetSavingTransactionAsync(int.Parse(TransactionIdRaw!));
        SavingIdRaw = transaction.SavingId.ToString();
        AmountText = transaction.Amount.ToString();
        OccurredAt = transaction.OccurredAt.UtcDateTime.Date;
        Note = transaction.Note;
    });

    [RelayCommand]
    private Task SaveAsync() => ExecuteSafelyAsync(async () =>
    {
        if (!long.TryParse(AmountText, out var amount) || amount <= 0)
        {
            await feedback.ShowErrorDialogAsync("Masukkan jumlah yang valid.");
            return;
        }
        var date = new DateTimeOffset(OccurredAt.Year, OccurredAt.Month, OccurredAt.Day, 0, 0, 0, TimeSpan.Zero);
        var request = new CreateSavingExpenseRequest(amount, date, string.IsNullOrWhiteSpace(Note) ? null : Note.Trim());
        if (IsEditMode)
        {
            await apiClient.UpdateSavingExpenseAsync(int.Parse(TransactionIdRaw!), request);
        }
        else if (int.TryParse(SavingIdRaw, out var savingId))
        {
            await apiClient.CreateSavingExpenseAsync(savingId, request);
        }
        await Shell.Current.GoToAsync("..");
    });

    [RelayCommand]
    private Task DeleteAsync() => ExecuteSafelyAsync(async () =>
    {
        if (!IsEditMode) return;
        var confirmed = await feedback.ShowConfirmationAsync(
            "Hapus pengeluaran", "Pengeluaran tabungan ini akan dihapus.", "Hapus", "Batal");
        if (!confirmed) return;
        await apiClient.DeleteSavingExpenseAsync(int.Parse(TransactionIdRaw!));
        await Shell.Current.GoToAsync("..");
    });
}
