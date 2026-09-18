using System.Text.Json;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Local.Entities;

namespace FamilyBudget.Mobile.Services.Local;

public sealed partial class LocalDatabase
{
    public async Task<IReadOnlyList<WalletDto>> GetWalletsAsync(int userId)
    {
        var db = await GetConnectionAsync();
        var rows = await db.Table<LocalWallet>().Where(row => row.UserId == userId).ToListAsync();
        var result = rows.OrderBy(row => row.ServerId).Select(row => new WalletDto(row.ServerId,
            row.Name, DateTimeOffset.FromUnixTimeMilliseconds(row.CreatedAtUnixMilliseconds), row.Balance)).ToList();
        var pending = await db.Table<LocalOutboxItem>().Where(row => row.UserId == userId).ToListAsync();
        foreach (var operation in pending.Where(row => row.Status is "pending" or "syncing" or "retry"))
        {
            if (operation.EntityType == "saving_transaction")
            {
                if (operation.OperationType is "update" or "delete" && operation.PreviousPayloadJson is not null)
                    ApplySavingWalletEffect(result, JsonSerializer.Deserialize<SavingTransactionDto>(operation.PreviousPayloadJson)!, -1);
                if (operation.OperationType is "create" or "update")
                    ApplySavingWalletEffect(result, JsonSerializer.Deserialize<SavingTransactionDto>(operation.PayloadJson)!, 1);
                continue;
            }
            if (operation.OperationType is "update" or "delete" && operation.PreviousPayloadJson is not null)
                ApplyWalletEffect(result, JsonSerializer.Deserialize<TransactionDto>(operation.PreviousPayloadJson)!, -1);
            if (operation.OperationType is "create" or "update")
                ApplyWalletEffect(result, JsonSerializer.Deserialize<TransactionDto>(operation.PayloadJson)!, 1);
        }
        return result;
    }

    private static void ApplySavingWalletEffect(List<WalletDto> wallets, SavingTransactionDto transaction, int direction)
    {
        var walletId = transaction.Type == "deposit" ? transaction.FromWalletId
            : transaction.Type == "withdrawal" ? transaction.ToWalletId : null;
        if (walletId is null) return;
        var index = wallets.FindIndex(wallet => wallet.Id == walletId);
        if (index < 0) return;
        var sign = transaction.Type == "deposit" ? -1 : 1;
        wallets[index] = wallets[index] with { Balance = wallets[index].Balance + direction * sign * transaction.Amount };
    }

    private static void ApplyWalletEffect(List<WalletDto> wallets, TransactionDto transaction, int direction)
    {
        var from = wallets.FindIndex(wallet => wallet.Id == transaction.FromWalletId);
        var to = wallets.FindIndex(wallet => wallet.Id == transaction.ToWalletId);
        if (from >= 0) wallets[from] = wallets[from] with { Balance = wallets[from].Balance - direction * transaction.Amount };
        if (to >= 0) wallets[to] = wallets[to] with { Balance = wallets[to].Balance + direction * transaction.Amount };
    }

    public async Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(int userId, TransactionListQuery query)
    {
        var db = await GetConnectionAsync();
        var clauses = new List<string> { "UserId = ?", "IsDeleted = 0" };
        var parameters = new List<object> { userId };
        if (query.PeriodId is { } periodId)
        {
            clauses.Add("PeriodId = ?");
            parameters.Add(periodId);
        }
        if (query.WalletId is { } walletId)
        {
            clauses.Add("(FromWalletId = ? OR ToWalletId = ?)");
            parameters.Add(walletId);
            parameters.Add(walletId);
        }
        if (query.Type is { } type)
        {
            clauses.Add("Type = ?");
            parameters.Add(type);
        }
        var sql = $"SELECT * FROM transactions WHERE {string.Join(" AND ", clauses)} "
            + "ORDER BY OccurredAtUnixMilliseconds DESC, CreatedAtUnixMilliseconds DESC, ServerId DESC";
        var rows = await db.QueryAsync<LocalTransaction>(sql, parameters.ToArray());
        return rows.Select(row => JsonSerializer.Deserialize<TransactionDto>(row.PayloadJson)! with
        {
            SyncStatus = row.SyncStatus ?? "synced",
            SyncError = row.SyncError,
        }).ToList();
    }

    public async Task<TransactionDto?> GetTransactionAsync(int userId, int serverId)
    {
        var db = await GetConnectionAsync();
        var row = await db.Table<LocalTransaction>()
            .Where(item => item.UserId == userId && item.ServerId == serverId && !item.IsDeleted)
            .FirstOrDefaultAsync();
        return row is null ? null : JsonSerializer.Deserialize<TransactionDto>(row.PayloadJson)! with
        {
            SyncStatus = row.SyncStatus ?? "synced", SyncError = row.SyncError, LocalId = row.LocalId,
        };
    }

    public async Task<DateTimeOffset?> GetLedgerRefreshedAtAsync(int userId)
    {
        var db = await GetConnectionAsync();
        var row = await db.FindAsync<LocalCacheState>($"{userId}:ledger");
        return row is null ? null : DateTimeOffset.FromUnixTimeMilliseconds(row.RefreshedAtUnixMilliseconds);
    }

    public async Task ReplaceLedgerSnapshotAsync(int userId, IReadOnlyCollection<WalletDto> wallets,
        IReadOnlyCollection<PeriodDto> periods, IReadOnlyCollection<TransactionDto> transactions)
    {
        var db = await GetConnectionAsync();
        var walletRows = wallets.Select(wallet => new LocalWallet
        {
            Key = CreateKey(userId, wallet.Id), UserId = userId, ServerId = wallet.Id,
            Name = wallet.Name, Balance = wallet.Balance,
            CreatedAtUnixMilliseconds = wallet.CreatedAt.ToUnixTimeMilliseconds(),
        }).ToList();
        var periodRows = periods.Select(period => new LocalPeriod
        {
            Key = CreateKey(userId, period.Id), UserId = userId, ServerId = period.Id,
            Name = period.Name, Status = period.Status,
            StartDateUnixMilliseconds = period.StartDate.ToUnixTimeMilliseconds(),
            ClosedAtUnixMilliseconds = period.ClosedAt?.ToUnixTimeMilliseconds(),
            CreatedAtUnixMilliseconds = period.CreatedAt.ToUnixTimeMilliseconds(),
        }).ToList();
        var transactionRows = transactions.Select(transaction => new LocalTransaction
        {
            Key = CreateKey(userId, transaction.Id), UserId = userId, ServerId = transaction.Id,
            PeriodId = transaction.PeriodId, FromWalletId = transaction.FromWalletId,
            ToWalletId = transaction.ToWalletId, Type = transaction.Type,
            OccurredAtUnixMilliseconds = transaction.OccurredAt.ToUnixTimeMilliseconds(),
            CreatedAtUnixMilliseconds = transaction.CreatedAt.ToUnixTimeMilliseconds(),
            PayloadJson = JsonSerializer.Serialize(transaction),
        }).ToList();
        // Never replace a full ledger using a filtered server response. All fetches
        // must complete first; failure rolls back both rows and the refresh marker.
        await db.RunInTransactionAsync(connection =>
        {
            var protectedKeys = connection.Query<LocalTransaction>(
                    "SELECT * FROM transactions WHERE UserId = ? AND SyncStatus IS NOT NULL AND SyncStatus != 'synced'", userId)
                .Select(row => row.Key).ToHashSet(StringComparer.Ordinal);
            connection.Execute("DELETE FROM wallets WHERE UserId = ?", userId);
            connection.Execute("DELETE FROM periods WHERE UserId = ?", userId);
            connection.Execute("DELETE FROM transactions WHERE UserId = ? AND (SyncStatus IS NULL OR SyncStatus = 'synced')", userId);
            connection.InsertAll(walletRows);
            connection.InsertAll(periodRows);
            connection.InsertAll(transactionRows.Where(row => !protectedKeys.Contains(row.Key)));
            connection.InsertOrReplace(new LocalCacheState
            {
                Key = $"{userId}:ledger",
                RefreshedAtUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            });
        });
    }

    public async Task ApplyTransactionChangesAsync(int userId, IReadOnlyCollection<WalletDto> wallets,
        IReadOnlyCollection<TransactionDto> upserts, IReadOnlyCollection<int> deletedIds)
    {
        var db = await GetConnectionAsync();
        var walletRows = wallets.Select(wallet => new LocalWallet
        {
            Key = CreateKey(userId, wallet.Id), UserId = userId, ServerId = wallet.Id,
            Name = wallet.Name, Balance = wallet.Balance,
            CreatedAtUnixMilliseconds = wallet.CreatedAt.ToUnixTimeMilliseconds(),
        }).ToList();
        var transactionRows = upserts.Select(transaction => new LocalTransaction
        {
            Key = CreateKey(userId, transaction.Id), UserId = userId, ServerId = transaction.Id,
            PeriodId = transaction.PeriodId, FromWalletId = transaction.FromWalletId,
            ToWalletId = transaction.ToWalletId, Type = transaction.Type,
            OccurredAtUnixMilliseconds = transaction.OccurredAt.ToUnixTimeMilliseconds(),
            CreatedAtUnixMilliseconds = transaction.CreatedAt.ToUnixTimeMilliseconds(),
            PayloadJson = JsonSerializer.Serialize(transaction),
        }).ToList();
        await db.RunInTransactionAsync(connection =>
        {
            connection.Execute("DELETE FROM wallets WHERE UserId=?", userId);
            connection.InsertAll(walletRows);
            foreach (var serverId in deletedIds)
            {
                var key = CreateKey(userId, serverId);
                var existing = connection.Find<LocalTransaction>(key);
                if (existing is not null && (existing.SyncStatus is null or "synced")) connection.Delete(existing);
            }
            foreach (var row in transactionRows)
            {
                var existing = connection.Find<LocalTransaction>(row.Key);
                if (existing is not null && existing.SyncStatus is not null and not "synced") continue;
                connection.InsertOrReplace(row);
            }
            connection.InsertOrReplace(new LocalCacheState
            {
                Key = $"{userId}:ledger", RefreshedAtUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            });
        });
    }
}
