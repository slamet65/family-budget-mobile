using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

[QueryProperty(nameof(SavingIdRaw), "savingId")]
public partial class SavingDetailViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    public ObservableCollection<SavingTransactionDto> Transactions { get; } = [];

    [ObservableProperty] private string? savingIdRaw;
    [ObservableProperty] private SavingDetailDto? saving;

    public int SavingId => int.TryParse(SavingIdRaw, out var id) ? id : 0;

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        if (SavingId == 0) return;
        Saving = await apiClient.GetSavingAsync(SavingId);
        var transactions = await apiClient.GetSavingTransactionsAsync(SavingId);
        Transactions.Clear();
        foreach (var transaction in transactions)
        {
            Transactions.Add(transaction);
        }
    }, background: true);
}
