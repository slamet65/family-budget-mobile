using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Auth;

namespace FamilyBudget.Mobile.Services.Local;

public sealed class LedgerRepository(ILocalDatabase database, IApiClient api,
    IAuthService auth) : ILedgerRepository
{
    private const int TransactionPageSize = 500;
    private const int MaxTransactionPages = 10_000;
    private readonly SemaphoreSlim refreshLock = new(1, 1);
    private int UserId => auth.CurrentUser?.Id
        ?? throw new InvalidOperationException("A signed-in user is required to access the local cache.");

    public Task<IReadOnlyList<WalletDto>> GetWalletsAsync() => database.GetWalletsAsync(UserId);
    public Task<IReadOnlyList<PeriodDto>> GetPeriodsAsync() => database.GetPeriodsAsync(UserId);
    public Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(TransactionListQuery query) =>
        database.GetTransactionsAsync(UserId, query);
    public Task<TransactionDto?> GetTransactionAsync(int id) => database.GetTransactionAsync(UserId, id);
    public Task<DateTimeOffset?> GetRefreshedAtAsync() => database.GetLedgerRefreshedAtAsync(UserId);

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await refreshLock.WaitAsync(ct);
        try
        {
            var userId = UserId;
            var wallets = await api.GetWalletsAsync(ct);
            var periods = await api.GetPeriodsAsync(ct);
            var transactions = await GetAllTransactionsAsync(ct);
            // Prevent a response begun under one account from being saved as another.
            if (auth.CurrentUser?.Id != userId) return;
            await database.ReplaceLedgerSnapshotAsync(userId, wallets, periods, transactions);
        }
        finally
        {
            refreshLock.Release();
        }
    }

    public async Task RefreshTransactionChangesAsync(IReadOnlyCollection<SyncChangeDto> changes,
        CancellationToken ct = default)
    {
        await refreshLock.WaitAsync(ct);
        try
        {
            var userId = UserId;
            var wallets = await api.GetWalletsAsync(ct);
            var latestChanges = changes.GroupBy(change => change.EntityId)
                .Select(group => group.OrderByDescending(change => change.Sequence).First()).ToList();
            var upserts = new List<TransactionDto>();
            var deletedIds = new HashSet<int>(latestChanges.Where(change => change.Operation == "delete")
                .Select(change => change.EntityId));
            foreach (var change in latestChanges.Where(change => change.Operation != "delete"))
            {
                try { upserts.Add(await api.GetTransactionAsync(change.EntityId, ct)); }
                catch (ApiException exception) when (exception.StatusCode == 404) { deletedIds.Add(change.EntityId); }
            }
            if (auth.CurrentUser?.Id != userId) return;
            await database.ApplyTransactionChangesAsync(userId, wallets, upserts, deletedIds);
        }
        finally { refreshLock.Release(); }
    }

    private async Task<List<TransactionDto>> GetAllTransactionsAsync(CancellationToken ct)
    {
        var result = new List<TransactionDto>();
        var seen = new HashSet<int>();
        var afterId = 0;
        for (var pageNumber = 1; pageNumber <= MaxTransactionPages; pageNumber++)
        {
            var page = await api.GetTransactionsAsync(
                new(null, null, null, afterId, TransactionPageSize), ct);
            foreach (var transaction in page)
                if (seen.Add(transaction.Id)) result.Add(transaction);

            if (page.Count < TransactionPageSize) break;
            var next = page.Max(transaction => transaction.Id);
            // Compatibility with an older server that ignores paging parameters and
            // returns the same full collection on the second request.
            if (next <= afterId || page.Count > TransactionPageSize) break;
            afterId = next;
            if (pageNumber == MaxTransactionPages)
                throw new InvalidOperationException("Transaction snapshot page safety limit exceeded.");
        }
        return result;
    }
}
