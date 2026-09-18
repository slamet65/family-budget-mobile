using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.Services.Local;
using Microsoft.Maui.Networking;
using FamilyBudget.Mobile.Services.Sync;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class WalletsViewModel(ILedgerRepository repository, ISyncCoordinator sync,
    IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    public ObservableCollection<WalletDto> Wallets { get; } = [];

    [ObservableProperty]
    private PeriodDto? currentPeriod;

    [ObservableProperty]
    private bool hasOpenPeriod;

    [ObservableProperty]
    private long totalBalance;

    [ObservableProperty]
    private string periodStatusText = "Tidak ada periode aktif";

    [ObservableProperty]
    private string cacheStatusText = "Belum ada data lokal";

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        await LoadCachedAsync();
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            CacheStatusText = "Offline — " + CacheStatusText;
            return;
        }
        try
        {
            await sync.SynchronizeAsync();
            await LoadCachedAsync();
        }
        catch (ApiException)
        {
            CacheStatusText = "Refresh gagal — " + CacheStatusText;
            throw;
        }
    }, background: true);

    [RelayCommand]
    private Task ForceFullResyncAsync() => ExecuteSafelyAsync(async () =>
    {
        await sync.ForceFullResyncAsync();
        await LoadCachedAsync();
    });

    private async Task LoadCachedAsync()
    {
        var wallets = await repository.GetWalletsAsync();
        Wallets.Clear();
        foreach (var wallet in wallets)
        {
            Wallets.Add(wallet);
        }
        TotalBalance = wallets.Sum(w => w.Balance);

        var periods = await repository.GetPeriodsAsync();
        CurrentPeriod = periods.FirstOrDefault(period => period.IsOpen);
        HasOpenPeriod = CurrentPeriod is not null;
        PeriodStatusText = HasOpenPeriod ? "Periode saat ini aktif" : "Tidak ada periode aktif";
        var refreshedAt = await repository.GetRefreshedAtAsync();
        var status = await sync.GetStatusAsync();
        var baseStatus = refreshedAt is { } time
            ? $"Saldo server terakhir: {time.ToLocalTime():d MMM HH:mm}"
            : "Belum ada data lokal — hubungkan internet untuk memuat";
        var queueStatus = status.QueueDepth > 0 ? $" • {status.QueueDepth} antrean" : string.Empty;
        var errorStatus = status.LastErrorCode is not null ? " • sync bermasalah" : string.Empty;
        CacheStatusText = baseStatus + queueStatus + errorStatus;
    }
}
