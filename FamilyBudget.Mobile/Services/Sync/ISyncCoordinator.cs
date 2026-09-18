namespace FamilyBudget.Mobile.Services.Sync;

public interface ISyncCoordinator
{
    Task SynchronizeAsync(CancellationToken ct = default);
    Task ForceFullResyncAsync(CancellationToken ct = default);
    Task<SyncStatusSnapshot> GetStatusAsync();
}
