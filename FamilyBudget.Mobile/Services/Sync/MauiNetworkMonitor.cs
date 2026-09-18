using Microsoft.Maui.Networking;

namespace FamilyBudget.Mobile.Services.Sync;

public sealed class MauiNetworkMonitor : INetworkMonitor
{
    public bool IsInternetAvailable => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    public event EventHandler? InternetAvailable;

    public MauiNetworkMonitor()
    {
        Connectivity.Current.ConnectivityChanged += (_, args) =>
        {
            if (args.NetworkAccess == NetworkAccess.Internet) InternetAvailable?.Invoke(this, EventArgs.Empty);
        };
    }
}
