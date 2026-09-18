namespace FamilyBudget.Mobile.Services.Sync;

public interface INetworkMonitor
{
    bool IsInternetAvailable { get; }
    event EventHandler? InternetAvailable;
}
