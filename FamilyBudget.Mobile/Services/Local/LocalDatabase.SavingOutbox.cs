using System.Text.Json;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Local.Entities;

namespace FamilyBudget.Mobile.Services.Local;

public sealed partial class LocalDatabase
{
    public Task<SavingTransactionDto> EnqueueSavingTransactionAsync(int userId, PendingSavingTransactionInput input) =>
        WriteSavingMutationAsync(userId, null, input, "create");

    public Task<SavingTransactionDto> EnqueueSavingTransactionUpdateAsync(int userId, SavingTransactionDto original,
        PendingSavingTransactionInput input)
    {
        if (original.Id <= 0 || !original.IsSynced || original.IsReadOnly)
            throw new InvalidOperationException("Only a synchronized editable saving transaction can be changed offline.");
        return WriteSavingMutationAsync(userId, original, input, "update");
    }

    public async Task EnqueueSavingTransactionDeleteAsync(int userId, SavingTransactionDto original)
    {
        if (original.Id <= 0 || !original.IsSynced || original.IsReadOnly)
            throw new InvalidOperationException("Only a synchronized editable saving transaction can be deleted offline.");
        var db = await GetConnectionAsync();
        var key = $"{userId}:saving_transaction:{original.Id}";
        var operationId = Guid.NewGuid().ToString();
        var previous = JsonSerializer.Serialize(original with { LocalId = null, SyncStatus = "synced", SyncError = null });
        await db.RunInTransactionAsync(connection =>
        {
            if (connection.Find<LocalDomainEntity>(key) is null) throw new InvalidOperationException("Saving transaction is not cached.");
            EnsureNoSavingMutation(connection, userId, key);
            connection.Execute("UPDATE domain_entities SET LocalId=?, SyncStatus='pending', SyncError=NULL, IsDeleted=1 WHERE Key=?",
                operationId, key);
            connection.Insert(new LocalOutboxItem
            {
                OperationId = operationId, UserId = userId, EntityLocalId = key, EntityType = "saving_transaction",
                OperationType = "delete", ServerId = original.Id, ExpectedVersion = original.Version,
                TransactionType = original.Type, PayloadJson = previous, PreviousPayloadJson = previous,
                Status = "pending", CreatedAtUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            });
        });
    }

    private async Task<SavingTransactionDto> WriteSavingMutationAsync(int userId, SavingTransactionDto? original,
        PendingSavingTransactionInput input, string operationType)
    {
        var db = await GetConnectionAsync();
        var operationId = Guid.NewGuid().ToString();
        var key = original is null ? $"{userId}:saving_transaction:local:{operationId}"
            : $"{userId}:saving_transaction:{original.Id}";
        var now = DateTimeOffset.UtcNow;
        var edited = new SavingTransactionDto(
            original?.Id ?? 0, input.SavingId, input.Type, input.Amount,
            original?.SourceTransactionId, original?.TransferGroupId, original?.SourceCategoryName,
            input.FromWalletId, input.FromWalletName, input.ToWalletId, input.ToWalletName,
            input.RelatedSavingId, original?.RelatedTransactionId, input.RelatedSavingName,
            input.Note, userId, input.OccurredAt, original?.CreatedAt ?? now,
            original?.ClientMutationId ?? operationId, operationId, "pending", null, original?.Version ?? 1);
        var payload = JsonSerializer.Serialize(edited);
        var previous = original is null ? null : JsonSerializer.Serialize(original with
        {
            LocalId = null, SyncStatus = "synced", SyncError = null,
        });
        await db.RunInTransactionAsync(connection =>
        {
            if (original is not null)
            {
                if (connection.Find<LocalDomainEntity>(key) is null) throw new InvalidOperationException("Saving transaction is not cached.");
                EnsureNoSavingMutation(connection, userId, key);
                connection.Execute("UPDATE domain_entities SET GroupId=?, LocalId=?, SyncStatus='pending', SyncError=NULL, IsDeleted=0, PayloadJson=? WHERE Key=?",
                    input.SavingId, operationId, payload, key);
            }
            else
            {
                connection.Insert(new LocalDomainEntity
                {
                    Key = key, UserId = userId, EntityType = "saving_transaction", ServerId = 0,
                    GroupId = input.SavingId, LocalId = operationId, SyncStatus = "pending", PayloadJson = payload,
                });
            }
            connection.Insert(new LocalOutboxItem
            {
                OperationId = operationId, UserId = userId, EntityLocalId = key, EntityType = "saving_transaction",
                OperationType = operationType, ServerId = original?.Id, ExpectedVersion = original?.Version,
                TransactionType = input.Type, PayloadJson = payload, PreviousPayloadJson = previous,
                Status = "pending", CreatedAtUnixMilliseconds = now.ToUnixTimeMilliseconds(),
            });
        });
        return edited;
    }

    public async Task CompleteSavingOutboxAsync(string operationId, SavingTransactionDto? serverTransaction)
    {
        var db = await GetConnectionAsync();
        await db.RunInTransactionAsync(connection =>
        {
            var item = connection.Find<LocalOutboxItem>(operationId);
            if (item is null) return;
            var local = connection.Find<LocalDomainEntity>(item.EntityLocalId);
            if (item.OperationType == "delete")
            {
                if (local is not null) connection.Delete(local);
                connection.Delete(item);
                return;
            }
            if (local is null || serverTransaction is null) return;
            var display = serverTransaction with { LocalId = null, SyncStatus = "synced", SyncError = null };
            var serverKey = $"{item.UserId}:saving_transaction:{serverTransaction.Id}";
            if (serverKey != local.Key)
            {
                connection.Delete(local);
                var existingServer = connection.Find<LocalDomainEntity>(serverKey);
                if (existingServer is not null) connection.Delete(existingServer);
                local.Key = serverKey;
            }
            local.ServerId = serverTransaction.Id;
            local.GroupId = serverTransaction.SavingId;
            local.LocalId = null;
            local.SyncStatus = "synced";
            local.SyncError = null;
            local.IsDeleted = false;
            local.PayloadJson = JsonSerializer.Serialize(display);
            connection.InsertOrReplace(local);
            connection.Delete(item);
        });
    }

    private static void EnsureNoSavingMutation(SQLite.SQLiteConnection connection, int userId, string key)
    {
        if (connection.Table<LocalOutboxItem>().Any(item => item.UserId == userId && item.EntityLocalId == key))
            throw new InvalidOperationException("Saving transaction already has a pending change.");
    }
}
