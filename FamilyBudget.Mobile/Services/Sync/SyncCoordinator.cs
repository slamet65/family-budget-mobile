using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Auth;
using FamilyBudget.Mobile.Services.Local;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FamilyBudget.Mobile.Services.Sync;

public sealed class SyncCoordinator(IApiClient api, ILocalDatabase database, IAuthService auth,
    IReferenceDataRepository references, ILedgerRepository ledger, IDomainRepository domain,
    IOutboxSyncService outbox, INetworkMonitor network, ILogger<SyncCoordinator>? providedLogger = null) : ISyncCoordinator
{
    private const int MaxChangePagesPerRun = 100;
    private readonly SemaphoreSlim syncLock = new(1, 1);
    private readonly ILogger<SyncCoordinator> logger = providedLogger ?? NullLogger<SyncCoordinator>.Instance;
    private int fullResyncRequested;

    public async Task SynchronizeAsync(CancellationToken ct = default)
    {
        if (!network.IsInternetAvailable || auth.CurrentUser is null) return;
        var requestedUserId = auth.CurrentUser.Id;
        await syncLock.WaitAsync(ct);
        var userId = requestedUserId;
        try
        {
            if (auth.CurrentUser?.Id != requestedUserId) return;
            await database.MarkSyncStartedAsync(userId);
            logger.LogInformation("Sync started");
            await outbox.ProcessPendingAsync(ct);
            if (Interlocked.Exchange(ref fullResyncRequested, 0) == 1)
            {
                await database.ClearSyncCursorAsync(userId);
                logger.LogInformation("Full resync started; local pending mutations are preserved");
            }
            var cursor = await database.GetSyncCursorAsync(userId);
            if (cursor is null)
            {
                var baseline = await api.GetSyncBootstrapAsync(ct);
                await RefreshAllAsync(ct);
                await database.SetSyncCursorAsync(userId, baseline.Cursor);
                cursor = baseline.Cursor;
            }

            var pageCount = 0;
            while (true)
            {
                if (++pageCount > MaxChangePagesPerRun)
                    throw new InvalidOperationException("Sync change-page safety limit exceeded.");
                var page = await api.GetSyncChangesAsync(cursor.Value, ct);
                logger.LogInformation("Sync change page {PageNumber} contains {ChangeCount} entries; hasMore={HasMore}",
                    pageCount, page.Changes.Count, page.HasMore);
                if (page.Changes.Count > 0)
                {
                    // The change feed is incremental; the current first implementation
                    // refreshes affected projections as collection snapshots. Cursor is
                    // committed only after all local writes succeed.
                    await RefreshAffectedAsync(userId, page.Changes, ct);
                    await database.SetSyncCursorAsync(userId, page.NextCursor);
                    cursor = page.NextCursor;
                }
                if (!page.HasMore || page.Changes.Count == 0) break;
            }
            await database.MarkSyncSucceededAsync(userId, cursor.Value);
            logger.LogInformation("Sync completed after {PageCount} change pages", pageCount);
        }
        catch (Exception exception)
        {
            await database.MarkSyncFailedAsync(userId, exception.GetType().Name);
            logger.LogWarning("Sync failed with {ErrorCode}", exception.GetType().Name);
            throw;
        }
        finally { syncLock.Release(); }
    }

    public async Task ForceFullResyncAsync(CancellationToken ct = default)
    {
        if (auth.CurrentUser is null) return;
        // The flag is consumed while holding the same lock as normal sync. This avoids
        // racing an in-flight sync that could otherwise write its old cursor back.
        Interlocked.Exchange(ref fullResyncRequested, 1);
        logger.LogInformation("Full resync requested");
        await SynchronizeAsync(ct);
    }

    public async Task<SyncStatusSnapshot> GetStatusAsync()
    {
        if (auth.CurrentUser is null) return new("idle", null, null, 0, null);
        var state = await database.GetSyncDiagnosticsAsync(auth.CurrentUser.Id);
        return new(state.Status, state.LastSuccessAt, state.LastSuccessCursor, state.QueueDepth, state.LastErrorCode);
    }

    private async Task RefreshAllAsync(CancellationToken ct)
    {
        await references.RefreshCategoriesAsync(ct);
        await references.RefreshPeriodsAsync(ct);
        await ledger.RefreshAsync(ct);
        await domain.RefreshAsync(ct);
    }

    private async Task RefreshAffectedAsync(int userId, IReadOnlyCollection<SyncChangeDto> changes,
        CancellationToken ct)
    {
        var types = changes.Select(change => change.EntityType).ToHashSet();
        var affectedPeriodIds = new HashSet<int>();
        foreach (var change in changes)
        {
            if (change.EntityType == "transactions")
            {
                var cached = await database.GetTransactionAsync(userId, change.EntityId);
                if (cached?.PeriodId is { } periodId) affectedPeriodIds.Add(periodId);
            }
            else if (change.EntityType == "budgets")
            {
                var periodId = await database.GetBudgetPeriodIdAsync(userId, change.EntityId);
                if (periodId is not null) affectedPeriodIds.Add(periodId.Value);
            }
        }

        if (types.Overlaps(["categories", "savings"])) await references.RefreshCategoriesAsync(ct);
        if (types.Overlaps(["periods"])) await references.RefreshPeriodsAsync(ct);
        if (types.Overlaps(["wallets", "periods", "wallet_reconciliations"]))
            await ledger.RefreshAsync(ct);
        else if (types.Contains("transactions"))
            await ledger.RefreshTransactionChangesAsync(
                changes.Where(change => change.EntityType == "transactions").ToList(), ct);

        // New transaction rows are only discoverable after the ledger refresh; deleted
        // rows were captured from the old cache above.
        foreach (var change in changes.Where(change => change.EntityType == "transactions"))
        {
            var refreshed = await database.GetTransactionAsync(userId, change.EntityId);
            if (refreshed?.PeriodId is { } periodId) affectedPeriodIds.Add(periodId);
        }

        if (types.Overlaps(["periods", "categories"]) ||
            (types.Contains("budgets") && affectedPeriodIds.Count == 0))
        {
            foreach (var period in await ledger.GetPeriodsAsync())
                if (period.IsOpen) affectedPeriodIds.Add(period.Id);
        }

        if (types.Overlaps(["savings", "saving_transactions"]))
            await domain.RefreshSavingChangesAsync(changes, ct);
        if (affectedPeriodIds.Count > 0) await domain.RefreshBudgetsAsync(affectedPeriodIds, ct);
        if (types.Contains("users")) await domain.RefreshUsersAsync(ct);
    }
}
