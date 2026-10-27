using System.Text.Json;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Local.Entities;

namespace FamilyBudget.Mobile.Services.Local;

public sealed partial class LocalDatabase
{
    // Create-only: saving movements are immutable on the API, so there is no offline
    // update/delete path. Update/delete items queued by older app versions are still read
    // (balance overlay, cancel restore) until the user cancels them.
    public async Task<SavingTransactionDto> EnqueueSavingTransactionAsync(int userId, PendingSavingTransactionInput input)
    {
        var db = await GetConnectionAsync();
        var operationId = Guid.NewGuid().ToString();
        var key = $"{userId}:saving_transaction:local:{operationId}";
        var now = DateTimeOffset.UtcNow;
        var pending = new SavingTransactionDto(
            0, input.SavingId, input.Type, input.Amount,
            null, null, null,
            input.FromWalletId, input.FromWalletName, input.ToWalletId, input.ToWalletName,
            input.RelatedSavingId, null, input.RelatedSavingName,
            input.Note, userId, input.OccurredAt, now,
            operationId, operationId, "pending", null, 1,
            input.FundedCategoryId, input.FundedCategoryName);
        var payload = JsonSerializer.Serialize(pending);
        await db.RunInTransactionAsync(connection =>
        {
            connection.Insert(new LocalDomainEntity
            {
                Key = key, UserId = userId, EntityType = "saving_transaction", ServerId = 0,
                GroupId = input.SavingId, LocalId = operationId, SyncStatus = "pending", PayloadJson = payload,
            });
            connection.Insert(new LocalOutboxItem
            {
                OperationId = operationId, UserId = userId, EntityLocalId = key, EntityType = "saving_transaction",
                OperationType = "create", TransactionType = input.Type, PayloadJson = payload,
                Status = "pending", CreatedAtUnixMilliseconds = now.ToUnixTimeMilliseconds(),
            });
        });
        return pending;
    }

    public async Task CompleteSavingOutboxAsync(string operationId, SavingTransactionDto serverTransaction)
    {
        var db = await GetConnectionAsync();
        await db.RunInTransactionAsync(connection =>
        {
            var item = connection.Find<LocalOutboxItem>(operationId);
            if (item is null) return;
            var local = connection.Find<LocalDomainEntity>(item.EntityLocalId);
            if (local is null) return;
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
}
