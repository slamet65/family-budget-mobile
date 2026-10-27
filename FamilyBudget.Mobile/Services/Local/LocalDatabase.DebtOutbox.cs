using System.Text.Json;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Local.Entities;

namespace FamilyBudget.Mobile.Services.Local;

public sealed partial class LocalDatabase
{
    // Create-only, like saving movements: the API rejects edits and deletes
    // (DEBT_TRANSACTION_IMMUTABLE), so corrections are new opposite movements.
    public async Task<DebtTransactionDto> EnqueueDebtTransactionAsync(int userId, PendingDebtTransactionInput input)
    {
        var db = await GetConnectionAsync();
        var operationId = Guid.NewGuid().ToString();
        var key = $"{userId}:debt_transaction:local:{operationId}";
        var now = DateTimeOffset.UtcNow;
        var pending = new DebtTransactionDto(
            0, input.DebtId, input.Type, input.Amount,
            null, null, null, null, null, null,
            input.FromSavingId, input.FromSavingName,
            input.Note, userId, input.OccurredAt, now,
            operationId, operationId, "pending", null, 1);
        var payload = JsonSerializer.Serialize(pending);
        await db.RunInTransactionAsync(connection =>
        {
            connection.Insert(new LocalDomainEntity
            {
                Key = key, UserId = userId, EntityType = "debt_transaction", ServerId = 0,
                GroupId = input.DebtId, LocalId = operationId, SyncStatus = "pending", PayloadJson = payload,
            });
            connection.Insert(new LocalOutboxItem
            {
                OperationId = operationId, UserId = userId, EntityLocalId = key, EntityType = "debt_transaction",
                OperationType = "create", TransactionType = input.Type, PayloadJson = payload,
                Status = "pending", CreatedAtUnixMilliseconds = now.ToUnixTimeMilliseconds(),
            });
        });
        return pending;
    }

    public async Task CompleteDebtOutboxAsync(string operationId, DebtTransactionDto serverTransaction)
    {
        var db = await GetConnectionAsync();
        await db.RunInTransactionAsync(connection =>
        {
            var item = connection.Find<LocalOutboxItem>(operationId);
            if (item is null) return;
            var local = connection.Find<LocalDomainEntity>(item.EntityLocalId);
            if (local is null) return;
            var display = serverTransaction with { LocalId = null, SyncStatus = "synced", SyncError = null };
            var serverKey = $"{item.UserId}:debt_transaction:{serverTransaction.Id}";
            if (serverKey != local.Key)
            {
                connection.Delete(local);
                var existingServer = connection.Find<LocalDomainEntity>(serverKey);
                if (existingServer is not null) connection.Delete(existingServer);
                local.Key = serverKey;
            }
            local.ServerId = serverTransaction.Id;
            local.GroupId = serverTransaction.DebtId;
            local.LocalId = null;
            local.SyncStatus = "synced";
            local.SyncError = null;
            local.IsDeleted = false;
            local.PayloadJson = JsonSerializer.Serialize(display);
            connection.InsertOrReplace(local);
            connection.Delete(item);
        });
    }
}
