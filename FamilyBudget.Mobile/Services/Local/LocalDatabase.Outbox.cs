using System.Text.Json;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Local.Entities;

namespace FamilyBudget.Mobile.Services.Local;

public sealed partial class LocalDatabase
{
    public async Task<TransactionDto> EnqueueTransactionAsync(int userId, PendingTransactionInput input)
    {
        var db = await GetConnectionAsync();
        var operationId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var transaction = new TransactionDto(0, input.PeriodId, input.Type,
            input.FromWalletId, input.FromWalletName, input.ToWalletId, input.ToWalletName,
            input.CategoryId, input.CategoryName, input.Amount, input.Note, userId,
            input.OccurredAt, now, operationId, operationId, "pending");
        var payload = JsonSerializer.Serialize(transaction);
        await db.RunInTransactionAsync(connection =>
        {
            connection.Insert(new LocalTransaction
            {
                Key = operationId, UserId = userId, ServerId = 0,
                ClientMutationId = operationId, LocalId = operationId, SyncStatus = "pending",
                PeriodId = input.PeriodId, FromWalletId = input.FromWalletId,
                ToWalletId = input.ToWalletId, Type = input.Type,
                OccurredAtUnixMilliseconds = input.OccurredAt.ToUnixTimeMilliseconds(),
                CreatedAtUnixMilliseconds = now.ToUnixTimeMilliseconds(), PayloadJson = payload,
            });
            connection.Insert(new LocalOutboxItem
            {
                OperationId = operationId, UserId = userId, EntityLocalId = operationId,
                OperationType = "create", TransactionType = input.Type, PayloadJson = payload, Status = "pending",
                CreatedAtUnixMilliseconds = now.ToUnixTimeMilliseconds(),
            });
        });
        return transaction;
    }

    public async Task<TransactionDto> EnqueueTransactionUpdateAsync(int userId, TransactionDto original,
        PendingTransactionInput input)
    {
        if (original.Id <= 0 || !original.IsSynced)
            throw new InvalidOperationException("Only a synchronized transaction can be edited offline.");
        var db = await GetConnectionAsync();
        var operationId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var key = CreateKey(userId, original.Id);
        var edited = original with
        {
            PeriodId = input.PeriodId, Type = input.Type,
            FromWalletId = input.FromWalletId, FromWalletName = input.FromWalletName,
            ToWalletId = input.ToWalletId, ToWalletName = input.ToWalletName,
            CategoryId = input.CategoryId, CategoryName = input.CategoryName,
            Amount = input.Amount, Note = input.Note, OccurredAt = input.OccurredAt,
            LocalId = operationId, SyncStatus = "pending", SyncError = null,
        };
        var previousPayload = JsonSerializer.Serialize(original with { LocalId = null, SyncStatus = "synced", SyncError = null });
        var payload = JsonSerializer.Serialize(edited);
        await db.RunInTransactionAsync(connection =>
        {
            if (connection.Find<LocalTransaction>(key) is null)
                throw new InvalidOperationException("Transaction is not available in the local cache.");
            if (connection.Table<LocalOutboxItem>().Any(item => item.UserId == userId && item.EntityLocalId == key))
                throw new InvalidOperationException("Transaction already has a pending change.");
            connection.Execute("UPDATE transactions SET LocalId=?, SyncStatus='pending', SyncError=NULL, IsDeleted=0, PeriodId=?, FromWalletId=?, ToWalletId=?, Type=?, OccurredAtUnixMilliseconds=?, PayloadJson=? WHERE Key=?",
                operationId, input.PeriodId, input.FromWalletId, input.ToWalletId, input.Type,
                input.OccurredAt.ToUnixTimeMilliseconds(), payload, key);
            connection.Insert(new LocalOutboxItem
            {
                OperationId = operationId, UserId = userId, EntityLocalId = key,
                OperationType = "update", ServerId = original.Id, ExpectedVersion = original.Version,
                TransactionType = input.Type, PayloadJson = payload, PreviousPayloadJson = previousPayload,
                Status = "pending", CreatedAtUnixMilliseconds = now.ToUnixTimeMilliseconds(),
            });
        });
        return edited;
    }

    public async Task EnqueueTransactionDeleteAsync(int userId, TransactionDto original)
    {
        if (original.Id <= 0 || !original.IsSynced)
            throw new InvalidOperationException("Only a synchronized transaction can be deleted offline.");
        var db = await GetConnectionAsync();
        var operationId = Guid.NewGuid().ToString();
        var key = CreateKey(userId, original.Id);
        var previousPayload = JsonSerializer.Serialize(original with { LocalId = null, SyncStatus = "synced", SyncError = null });
        await db.RunInTransactionAsync(connection =>
        {
            if (connection.Find<LocalTransaction>(key) is null)
                throw new InvalidOperationException("Transaction is not available in the local cache.");
            if (connection.Table<LocalOutboxItem>().Any(item => item.UserId == userId && item.EntityLocalId == key))
                throw new InvalidOperationException("Transaction already has a pending change.");
            connection.Execute("UPDATE transactions SET LocalId=?, SyncStatus='pending', SyncError=NULL, IsDeleted=1 WHERE Key=?",
                operationId, key);
            connection.Insert(new LocalOutboxItem
            {
                OperationId = operationId, UserId = userId, EntityLocalId = key,
                OperationType = "delete", ServerId = original.Id, ExpectedVersion = original.Version,
                TransactionType = original.Type, PayloadJson = previousPayload, PreviousPayloadJson = previousPayload,
                Status = "pending", CreatedAtUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            });
        });
    }

    public async Task<IReadOnlyList<LocalOutboxItem>> GetPendingOutboxAsync(int userId)
    {
        var db = await GetConnectionAsync();
        var rows = await db.Table<LocalOutboxItem>().Where(item => item.UserId == userId).ToListAsync();
        return rows.Where(item => item.Status is "pending" or "retry" or "syncing")
            .OrderBy(item => item.CreatedAtUnixMilliseconds).ToList();
    }

    public async Task MarkOutboxSyncingAsync(string operationId)
    {
        var db = await GetConnectionAsync();
        await db.RunInTransactionAsync(connection =>
        {
            connection.Execute("UPDATE outbox SET Status='syncing', AttemptCount=AttemptCount+1, LastAttemptAtUnixMilliseconds=?, NextAttemptAtUnixMilliseconds=NULL WHERE OperationId=?",
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), operationId);
            var item = connection.Find<LocalOutboxItem>(operationId);
            if (item is null) return;
            if (item.EntityType == "saving_transaction")
            {
                connection.Execute("UPDATE domain_entities SET SyncStatus='syncing', SyncError=NULL WHERE Key=?", item.EntityLocalId);
                RefreshSavingPayload(connection, item.EntityLocalId, "syncing", null);
                return;
            }
            connection.Execute("UPDATE transactions SET SyncStatus='syncing', SyncError=NULL WHERE Key=?", item.EntityLocalId);
            RefreshPayload(connection, item.EntityLocalId, "syncing", null);
        });
    }

    public async Task CompleteOutboxAsync(string operationId, TransactionDto? serverTransaction)
    {
        var db = await GetConnectionAsync();
        await db.RunInTransactionAsync(connection =>
        {
            var item = connection.Find<LocalOutboxItem>(operationId);
            if (item is null) return;
            var local = connection.Find<LocalTransaction>(item.EntityLocalId);
            if (item.OperationType == "delete")
            {
                if (local is not null) connection.Delete(local);
                connection.Delete(item);
                return;
            }
            if (local is null || serverTransaction is null) return;
            var display = serverTransaction with
            {
                LocalId = item.OperationType == "create" ? item.EntityLocalId : null,
                SyncStatus = "synced", SyncError = null,
            };
            local.ServerId = serverTransaction.Id;
            local.LocalId = display.LocalId;
            local.SyncStatus = "synced";
            local.SyncError = null;
            local.IsDeleted = false;
            local.PayloadJson = JsonSerializer.Serialize(display);
            connection.Update(local);
            connection.Delete(item);
        });
    }

    public async Task FailOutboxAsync(string operationId, string error, bool permanent)
    {
        var status = permanent ? "failed" : "retry";
        var db = await GetConnectionAsync();
        await db.RunInTransactionAsync(connection =>
        {
            connection.Execute("UPDATE outbox SET Status=?, LastError=?, NextAttemptAtUnixMilliseconds=? WHERE OperationId=?",
                status, error, null, operationId);
            var item = connection.Find<LocalOutboxItem>(operationId);
            if (item is null) return;
            if (item.EntityType == "saving_transaction")
            {
                connection.Execute("UPDATE domain_entities SET SyncStatus=?, SyncError=?, IsDeleted=? WHERE Key=?",
                    status, error, permanent && item.OperationType == "delete" ? 0 : item.OperationType == "delete" ? 1 : 0,
                    item.EntityLocalId);
                RefreshSavingPayload(connection, item.EntityLocalId, status, error);
                return;
            }
            connection.Execute("UPDATE transactions SET SyncStatus=?, SyncError=?, IsDeleted=? WHERE Key=?",
                status, error, permanent && item.OperationType == "delete" ? 0 : item.OperationType == "delete" ? 1 : 0,
                item.EntityLocalId);
            RefreshPayload(connection, item.EntityLocalId, status, error);
        });
    }

    public async Task RetryOutboxAsync(int userId, string localId)
    {
        var db = await GetConnectionAsync();
        await db.RunInTransactionAsync(connection =>
        {
            var item = connection.Find<LocalOutboxItem>(localId);
            if (item is null || item.UserId != userId) return;
            connection.Execute("UPDATE outbox SET Status='pending', LastError=NULL, NextAttemptAtUnixMilliseconds=NULL WHERE OperationId=?", item.OperationId);
            if (item.EntityType == "saving_transaction")
            {
                connection.Execute("UPDATE domain_entities SET SyncStatus='pending', SyncError=NULL, IsDeleted=? WHERE Key=?",
                    item.OperationType == "delete" ? 1 : 0, item.EntityLocalId);
                RefreshSavingPayload(connection, item.EntityLocalId, "pending", null);
                return;
            }
            connection.Execute("UPDATE transactions SET SyncStatus='pending', SyncError=NULL, IsDeleted=? WHERE Key=?",
                item.OperationType == "delete" ? 1 : 0, item.EntityLocalId);
            RefreshPayload(connection, item.EntityLocalId, "pending", null);
        });
    }

    public async Task CancelOutboxAsync(int userId, string localId)
    {
        var db = await GetConnectionAsync();
        await db.RunInTransactionAsync(connection =>
        {
            var item = connection.Find<LocalOutboxItem>(localId);
            if (item is null || item.UserId != userId) return;
            if (item.EntityType == "saving_transaction")
            {
                var domain = connection.Find<LocalDomainEntity>(item.EntityLocalId);
                if (item.OperationType == "create")
                {
                    if (domain is not null) connection.Delete(domain);
                }
                else if (domain is not null && item.PreviousPayloadJson is not null)
                {
                    var previousSaving = JsonSerializer.Deserialize<SavingTransactionDto>(item.PreviousPayloadJson)!;
                    domain.GroupId = previousSaving.SavingId;
                    domain.LocalId = null;
                    domain.SyncStatus = "synced";
                    domain.SyncError = null;
                    domain.IsDeleted = false;
                    domain.PayloadJson = JsonSerializer.Serialize(previousSaving);
                    connection.Update(domain);
                }
                connection.Delete(item);
                return;
            }
            var local = connection.Find<LocalTransaction>(item.EntityLocalId);
            if (item.OperationType == "create")
            {
                if (local is not null) connection.Delete(local);
            }
            else if (local is not null && item.PreviousPayloadJson is not null)
            {
                var previous = JsonSerializer.Deserialize<TransactionDto>(item.PreviousPayloadJson)!;
                local.LocalId = null;
                local.SyncStatus = "synced";
                local.SyncError = null;
                local.IsDeleted = false;
                local.PeriodId = previous.PeriodId;
                local.FromWalletId = previous.FromWalletId;
                local.ToWalletId = previous.ToWalletId;
                local.Type = previous.Type;
                local.OccurredAtUnixMilliseconds = previous.OccurredAt.ToUnixTimeMilliseconds();
                local.PayloadJson = JsonSerializer.Serialize(previous);
                connection.Update(local);
            }
            connection.Delete(item);
        });
    }

    public async Task<bool> HasUnresolvedOutboxAsync(int userId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<LocalOutboxItem>().Where(item => item.UserId == userId).CountAsync() > 0;
    }

    private static void RefreshPayload(SQLite.SQLiteConnection connection, string key, string status, string? error)
    {
        var row = connection.Find<LocalTransaction>(key);
        if (row is null) return;
        var transaction = JsonSerializer.Deserialize<TransactionDto>(row.PayloadJson)! with
        {
            SyncStatus = status, SyncError = error,
        };
        row.PayloadJson = JsonSerializer.Serialize(transaction);
        connection.Update(row);
    }

    private static void RefreshSavingPayload(SQLite.SQLiteConnection connection, string key, string status, string? error)
    {
        var row = connection.Find<LocalDomainEntity>(key);
        if (row is null) return;
        var transaction = JsonSerializer.Deserialize<SavingTransactionDto>(row.PayloadJson)! with
        {
            LocalId = row.LocalId, SyncStatus = status, SyncError = error,
        };
        row.PayloadJson = JsonSerializer.Serialize(transaction);
        connection.Update(row);
    }

    internal static TimeSpan OutboxRetryDelay(int attemptCount) =>
        TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, Math.Clamp(attemptCount, 1, 8))));
}
