using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Local.Entities;
using SQLite;

namespace FamilyBudget.Mobile.Services.Local;

public sealed partial class LocalDatabase(string databasePath) : ILocalDatabase
{
    internal const int CurrentSchemaVersion = 8;
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private SQLiteAsyncConnection? connection;

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (connection is not null)
        {
            return connection;
        }

        await initializationLock.WaitAsync();
        try
        {
            if (connection is not null)
            {
                return connection;
            }

            var flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;
            var newConnection = new SQLiteAsyncConnection(databasePath, flags);
            await newConnection.CreateTableAsync<LocalCategory>();
            await newConnection.CreateTableAsync<LocalPeriod>();
            await newConnection.CreateTableAsync<LocalWallet>();
            await newConnection.CreateTableAsync<LocalTransaction>();
            await newConnection.CreateTableAsync<LocalCacheState>();
            await newConnection.CreateTableAsync<LocalOutboxItem>();
            await newConnection.CreateTableAsync<LocalDomainEntity>();
            await newConnection.CreateTableAsync<LocalSyncState>();
            await RunMigrationsAsync(newConnection);
            connection = newConnection;
            return newConnection;
        }
        finally
        {
            initializationLock.Release();
        }
    }

    private static async Task RunMigrationsAsync(SQLiteAsyncConnection db)
    {
        var version = await db.ExecuteScalarAsync<int>("PRAGMA user_version");
        if (version > CurrentSchemaVersion)
        {
            throw new InvalidOperationException($"Local database schema {version} is newer than supported schema {CurrentSchemaVersion}.");
        }

        // Explicit indexes for the hot offline queries. CREATE INDEX IF NOT EXISTS keeps
        // upgrades idempotent for databases created by every previous release.
        await db.ExecuteAsync("CREATE INDEX IF NOT EXISTS transactions_user_order_idx ON transactions(UserId, OccurredAtUnixMilliseconds DESC, CreatedAtUnixMilliseconds DESC, ServerId DESC)");
        await db.ExecuteAsync("CREATE INDEX IF NOT EXISTS transactions_user_period_idx ON transactions(UserId, PeriodId)");
        await db.ExecuteAsync("CREATE INDEX IF NOT EXISTS transactions_user_wallet_from_idx ON transactions(UserId, FromWalletId)");
        await db.ExecuteAsync("CREATE INDEX IF NOT EXISTS transactions_user_wallet_to_idx ON transactions(UserId, ToWalletId)");
        await db.ExecuteAsync("CREATE INDEX IF NOT EXISTS outbox_user_status_created_idx ON outbox(UserId, Status, CreatedAtUnixMilliseconds)");
        await db.ExecuteAsync("CREATE INDEX IF NOT EXISTS domain_user_type_group_idx ON domain_entities(UserId, EntityType, GroupId)");
        await db.ExecuteAsync("CREATE INDEX IF NOT EXISTS domain_user_type_server_idx ON domain_entities(UserId, EntityType, ServerId)");

        // A process can die after the durable item is marked syncing but before the HTTP
        // result is committed. Idempotency makes it safe to retry on the next process.
        await db.ExecuteAsync("UPDATE outbox SET Status='retry', NextAttemptAtUnixMilliseconds=NULL WHERE Status='syncing'");
        await db.ExecuteAsync("UPDATE transactions SET SyncStatus='retry', SyncError='Sinkronisasi terputus; akan dicoba lagi.' WHERE SyncStatus='syncing'");
        await db.ExecuteAsync("UPDATE domain_entities SET SyncStatus='retry', SyncError='Sinkronisasi terputus; akan dicoba lagi.' WHERE SyncStatus='syncing'");
        await db.ExecuteAsync($"PRAGMA user_version = {CurrentSchemaVersion}");
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(int userId)
    {
        var db = await GetConnectionAsync();
        var rows = await db.Table<LocalCategory>()
            .Where(row => row.UserId == userId)
            .ToListAsync();

        return rows.Select(row => new CategoryDto(
            row.ServerId,
            row.Name,
            row.ParentId,
            row.IsCatchAll,
            row.SavingId,
            row.SavingName,
            DateTimeOffset.FromUnixTimeMilliseconds(row.CreatedAtUnixMilliseconds)))
            .ToList();
    }

    public async Task ReplaceCategoriesAsync(int userId, IReadOnlyCollection<CategoryDto> categories)
    {
        var db = await GetConnectionAsync();
        var rows = categories.Select(category => new LocalCategory
        {
            Key = CreateKey(userId, category.Id),
            UserId = userId,
            ServerId = category.Id,
            Name = category.Name,
            ParentId = category.ParentId,
            IsCatchAll = category.IsCatchAll,
            SavingId = category.SavingId,
            SavingName = category.SavingName,
            CreatedAtUnixMilliseconds = category.CreatedAt.ToUnixTimeMilliseconds(),
        }).ToList();

        await db.RunInTransactionAsync(transaction =>
        {
            transaction.Execute("DELETE FROM categories WHERE UserId = ?", userId);
            transaction.InsertAll(rows);
        });
    }

    public async Task<IReadOnlyList<PeriodDto>> GetPeriodsAsync(int userId)
    {
        var db = await GetConnectionAsync();
        var rows = await db.Table<LocalPeriod>()
            .Where(row => row.UserId == userId)
            .ToListAsync();

        return rows.Select(row => new PeriodDto(
            row.ServerId,
            row.Name,
            DateTimeOffset.FromUnixTimeMilliseconds(row.StartDateUnixMilliseconds),
            row.Status,
            row.ClosedAtUnixMilliseconds is { } closedAt
                ? DateTimeOffset.FromUnixTimeMilliseconds(closedAt)
                : null,
            DateTimeOffset.FromUnixTimeMilliseconds(row.CreatedAtUnixMilliseconds)))
            .ToList();
    }

    public async Task ReplacePeriodsAsync(int userId, IReadOnlyCollection<PeriodDto> periods)
    {
        var db = await GetConnectionAsync();
        var rows = periods.Select(period => new LocalPeriod
        {
            Key = CreateKey(userId, period.Id),
            UserId = userId,
            ServerId = period.Id,
            Name = period.Name,
            StartDateUnixMilliseconds = period.StartDate.ToUnixTimeMilliseconds(),
            Status = period.Status,
            ClosedAtUnixMilliseconds = period.ClosedAt?.ToUnixTimeMilliseconds(),
            CreatedAtUnixMilliseconds = period.CreatedAt.ToUnixTimeMilliseconds(),
        }).ToList();

        await db.RunInTransactionAsync(transaction =>
        {
            transaction.Execute("DELETE FROM periods WHERE UserId = ?", userId);
            transaction.InsertAll(rows);
        });
    }

    private static string CreateKey(int userId, int serverId) => $"{userId}:{serverId}";
}
