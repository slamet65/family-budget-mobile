using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;
using FamilyBudget.Mobile.Services.Local;
using FamilyBudget.Mobile.Services.Sync;
using Microsoft.Maui.Networking;

namespace FamilyBudget.Mobile.ViewModels;

public partial class DebtsViewModel(IDomainRepository repository, ISyncCoordinator sync,
    IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    public ObservableCollection<DebtDto> Debts { get; } = [];

    // Overpaid debts don't offset what is still owed elsewhere.
    [ObservableProperty]
    private long totalRemaining;

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        var debts = await repository.GetDebtsAsync();
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            await sync.SynchronizeAsync();
            debts = await repository.GetDebtsAsync();
        }
        Debts.Clear();
        // Outstanding debts first, then paid-off and overpaid ones.
        foreach (var debt in debts.OrderBy(d => d.Balance <= 0).ThenBy(d => d.Name))
        {
            Debts.Add(debt);
        }
        TotalRemaining = debts.Where(d => d.Balance > 0).Sum(d => d.Balance);
    }, background: true);
}
