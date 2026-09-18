using System.Text.Json;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Local.Entities;

namespace FamilyBudget.Mobile.Services.Local;

public sealed partial class LocalDatabase
{
    public async Task<long?> GetSyncCursorAsync(int userId)
    {
        var db = await GetConnectionAsync();
        var row = await db.FindAsync<LocalCacheState>($"{userId}:sync-cursor");
        return row?.RefreshedAtUnixMilliseconds;
    }

    public async Task SetSyncCursorAsync(int userId, long cursor)
    {
        var db = await GetConnectionAsync();
        await db.InsertOrReplaceAsync(new LocalCacheState { Key = $"{userId}:sync-cursor", RefreshedAtUnixMilliseconds = cursor });
    }

    public async Task ReplaceDomainSnapshotAsync(int userId, DomainSnapshot snapshot)
    {
        var rows = new List<LocalDomainEntity>();
        rows.AddRange(snapshot.Savings.Select(item => Row(userId, "saving", item.Id, null, item)));
        rows.AddRange(snapshot.SavingTransactions.Select(item => Row(userId, "saving_transaction", item.Id, item.SavingId, item)));
        rows.AddRange(snapshot.Budgets.Select(item => Row(userId, "budget", item.Id, item.PeriodId, item)));
        rows.AddRange(snapshot.Users.Select(item => Row(userId, "user", item.Id, null, item)));
        var db = await GetConnectionAsync();
        await db.RunInTransactionAsync(connection =>
        {
            var protectedKeys = connection.Query<LocalDomainEntity>(
                    "SELECT * FROM domain_entities WHERE UserId=? AND SyncStatus IS NOT NULL AND SyncStatus != 'synced'", userId)
                .Select(row => row.Key).ToHashSet(StringComparer.Ordinal);
            connection.Execute("DELETE FROM domain_entities WHERE UserId=? AND (SyncStatus IS NULL OR SyncStatus='synced')", userId);
            connection.InsertAll(rows.Where(row => !protectedKeys.Contains(row.Key)));
        });
    }

    public async Task ReplaceSavingsSnapshotAsync(int userId, IReadOnlyCollection<SavingDto> savings,
        IReadOnlyCollection<SavingTransactionDto> transactions)
    {
        var rows = savings.Select(item => Row(userId, "saving", item.Id, null, item))
            .Concat(transactions.Select(item => Row(userId, "saving_transaction", item.Id, item.SavingId, item))).ToList();
        var db = await GetConnectionAsync();
        await db.RunInTransactionAsync(connection =>
        {
            var protectedKeys = connection.Query<LocalDomainEntity>(
                    "SELECT * FROM domain_entities WHERE UserId=? AND EntityType='saving_transaction' AND SyncStatus IS NOT NULL AND SyncStatus != 'synced'", userId)
                .Select(row => row.Key).ToHashSet(StringComparer.Ordinal);
            connection.Execute("DELETE FROM domain_entities WHERE UserId=? AND EntityType='saving'", userId);
            connection.Execute("DELETE FROM domain_entities WHERE UserId=? AND EntityType='saving_transaction' AND (SyncStatus IS NULL OR SyncStatus='synced')", userId);
            connection.InsertAll(rows.Where(row => !protectedKeys.Contains(row.Key)));
        });
    }

    public async Task ReplaceSavingsAsync(int userId, IReadOnlyCollection<int> savingIds,
        IReadOnlyCollection<SavingDto> savings, IReadOnlyCollection<SavingTransactionDto> transactions)
    {
        if (savingIds.Count == 0) return;
        var rows = savings.Select(item => Row(userId, "saving", item.Id, null, item))
            .Concat(transactions.GroupBy(item => item.Id).Select(group => group.First())
                .Select(item => Row(userId, "saving_transaction", item.Id, item.SavingId, item))).ToList();
        var db = await GetConnectionAsync();
        await db.RunInTransactionAsync(connection =>
        {
            var protectedKeys = connection.Query<LocalDomainEntity>(
                    "SELECT * FROM domain_entities WHERE UserId=? AND EntityType='saving_transaction' AND SyncStatus IS NOT NULL AND SyncStatus != 'synced'", userId)
                .Select(row => row.Key).ToHashSet(StringComparer.Ordinal);
            foreach (var savingId in savingIds)
            {
                connection.Execute("DELETE FROM domain_entities WHERE UserId=? AND EntityType='saving' AND ServerId=?", userId, savingId);
                connection.Execute("DELETE FROM domain_entities WHERE UserId=? AND EntityType='saving_transaction' AND GroupId=? AND (SyncStatus IS NULL OR SyncStatus='synced')", userId, savingId);
            }
            connection.InsertAll(rows.Where(row => !protectedKeys.Contains(row.Key)));
        });
    }

    public async Task ReplaceBudgetsAsync(int userId, IReadOnlyCollection<int> periodIds,
        IReadOnlyCollection<BudgetDto> budgets)
    {
        if (periodIds.Count == 0) return;
        var rows = budgets.Select(item => Row(userId, "budget", item.Id, item.PeriodId, item)).ToList();
        var db = await GetConnectionAsync();
        await db.RunInTransactionAsync(connection =>
        {
            foreach (var periodId in periodIds)
                connection.Execute("DELETE FROM domain_entities WHERE UserId=? AND EntityType='budget' AND GroupId=?", userId, periodId);
            connection.InsertAll(rows);
        });
    }

    public async Task ReplaceUsersAsync(int userId, IReadOnlyCollection<UserDto> users)
    {
        var rows = users.Select(item => Row(userId, "user", item.Id, null, item)).ToList();
        var db = await GetConnectionAsync();
        await db.RunInTransactionAsync(connection =>
        {
            connection.Execute("DELETE FROM domain_entities WHERE UserId=? AND EntityType='user'", userId);
            connection.InsertAll(rows);
        });
    }

    public async Task<int?> GetBudgetPeriodIdAsync(int userId, int budgetId)
    {
        var db = await GetConnectionAsync();
        var row = await db.Table<LocalDomainEntity>().Where(item => item.UserId == userId
            && item.EntityType == "budget" && item.ServerId == budgetId).FirstOrDefaultAsync();
        return row?.GroupId;
    }

    public async Task<IReadOnlyList<SavingDto>> GetSavingsAsync(int userId)
    {
        var result = (await Read<SavingDto>(userId, "saving", null)).ToList();
        var db = await GetConnectionAsync();
        var operations = await db.Table<LocalOutboxItem>().Where(row => row.UserId == userId).ToListAsync();
        foreach (var operation in operations.Where(row => row.EntityType == "saving_transaction" && row.Status is "pending" or "syncing" or "retry"))
        {
            if (operation.OperationType is "update" or "delete" && operation.PreviousPayloadJson is not null)
                ApplySavingEffect(result, JsonSerializer.Deserialize<SavingTransactionDto>(operation.PreviousPayloadJson)!, -1);
            if (operation.OperationType is "create" or "update")
                ApplySavingEffect(result, JsonSerializer.Deserialize<SavingTransactionDto>(operation.PayloadJson)!, 1);
        }
        return result;
    }

    public async Task<IReadOnlyList<SavingTransactionDto>> GetSavingTransactionsAsync(int userId, int savingId)
    {
        var db = await GetConnectionAsync();
        var rows = await db.Table<LocalDomainEntity>().Where(row => row.UserId == userId
            && row.EntityType == "saving_transaction" && row.GroupId == savingId && !row.IsDeleted).ToListAsync();
        var result = rows.Select(row => JsonSerializer.Deserialize<SavingTransactionDto>(row.PayloadJson)! with
        {
            LocalId = row.LocalId, SyncStatus = row.SyncStatus ?? "synced", SyncError = row.SyncError,
        }).ToList();
        return result.GroupBy(item => item.ClientMutationId is null ? $"server:{item.Id}" : $"mutation:{item.ClientMutationId}")
            .Select(group => group.OrderBy(item => item.IsSynced).First())
            .OrderByDescending(item => item.OccurredAt).ThenByDescending(item => item.CreatedAt).ToList();
    }

    public async Task<SavingTransactionDto?> GetSavingTransactionAsync(int userId, int serverId)
    {
        var db = await GetConnectionAsync();
        var row = await db.Table<LocalDomainEntity>().Where(item => item.UserId == userId
            && item.EntityType == "saving_transaction" && item.ServerId == serverId && !item.IsDeleted).FirstOrDefaultAsync();
        return row is null ? null : JsonSerializer.Deserialize<SavingTransactionDto>(row.PayloadJson)! with
        {
            LocalId = row.LocalId, SyncStatus = row.SyncStatus ?? "synced", SyncError = row.SyncError,
        };
    }
    public Task<IReadOnlyList<BudgetDto>> GetBudgetsAsync(int userId, int periodId) => Read<BudgetDto>(userId, "budget", periodId);
    public Task<IReadOnlyList<UserDto>> GetUsersAsync(int userId) => Read<UserDto>(userId, "user", null);

    private async Task<IReadOnlyList<T>> Read<T>(int userId, string entityType, int? groupId)
    {
        var db = await GetConnectionAsync();
        var rows = await db.Table<LocalDomainEntity>().Where(row => row.UserId == userId && row.EntityType == entityType).ToListAsync();
        if (groupId is not null) rows = rows.Where(row => row.GroupId == groupId).ToList();
        return rows.Select(row => JsonSerializer.Deserialize<T>(row.PayloadJson)!).ToList();
    }

    private static LocalDomainEntity Row<T>(int userId, string type, int id, int? groupId, T value) => new()
    {
        Key = $"{userId}:{type}:{id}", UserId = userId, EntityType = type,
        ServerId = id, GroupId = groupId, PayloadJson = JsonSerializer.Serialize(value),
    };

    private static void ApplySavingEffect(List<SavingDto> savings, SavingTransactionDto transaction, int direction)
    {
        static long Sign(string type) => type is "deposit" or "opening_balance" or "transfer_in" ? 1 : -1;
        var source = savings.FindIndex(item => item.Id == transaction.SavingId);
        if (source >= 0)
            savings[source] = savings[source] with { Balance = savings[source].Balance + direction * Sign(transaction.Type) * transaction.Amount };
        if (transaction.Type == "transfer_out" && transaction.RelatedSavingId is { } targetId)
        {
            var target = savings.FindIndex(item => item.Id == targetId);
            if (target >= 0) savings[target] = savings[target] with { Balance = savings[target].Balance + direction * transaction.Amount };
        }
    }
}
