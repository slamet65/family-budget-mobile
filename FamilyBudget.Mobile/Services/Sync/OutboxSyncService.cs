using System.Text.Json;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Auth;
using FamilyBudget.Mobile.Services.Local;
using FamilyBudget.Mobile.Services.Local.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FamilyBudget.Mobile.Services.Sync;

public sealed class OutboxSyncService : IOutboxSyncService
{
    private readonly ILocalDatabase database;
    private readonly IApiClient api;
    private readonly IAuthService auth;
    private readonly INetworkMonitor network;
    private readonly ILogger<OutboxSyncService> logger;
    private readonly SemaphoreSlim processLock = new(1, 1);

    public OutboxSyncService(ILocalDatabase database, IApiClient api, IAuthService auth, INetworkMonitor network,
        ILogger<OutboxSyncService>? logger = null)
    {
        this.database = database;
        this.api = api;
        this.auth = auth;
        this.network = network;
        this.logger = logger ?? NullLogger<OutboxSyncService>.Instance;
        network.InternetAvailable += (_, _) => TriggerSync();
    }

    public async Task<TransactionDto> EnqueueAsync(PendingTransactionInput input)
    {
        var userId = GetUserId();
        var transaction = await database.EnqueueTransactionAsync(userId, input);
        TriggerSync();
        return transaction;
    }

    public async Task<TransactionDto> EnqueueUpdateAsync(TransactionDto original, PendingTransactionInput input)
    {
        var transaction = await database.EnqueueTransactionUpdateAsync(GetUserId(), original, input);
        TriggerSync();
        return transaction;
    }

    public async Task EnqueueDeleteAsync(TransactionDto original)
    {
        await database.EnqueueTransactionDeleteAsync(GetUserId(), original);
        TriggerSync();
    }

    public async Task<SavingTransactionDto> EnqueueSavingAsync(PendingSavingTransactionInput input)
    {
        var transaction = await database.EnqueueSavingTransactionAsync(GetUserId(), input);
        TriggerSync();
        return transaction;
    }

    public async Task<SavingTransactionDto> EnqueueSavingUpdateAsync(SavingTransactionDto original,
        PendingSavingTransactionInput input)
    {
        var transaction = await database.EnqueueSavingTransactionUpdateAsync(GetUserId(), original, input);
        TriggerSync();
        return transaction;
    }

    public async Task EnqueueSavingDeleteAsync(SavingTransactionDto original)
    {
        await database.EnqueueSavingTransactionDeleteAsync(GetUserId(), original);
        TriggerSync();
    }

    public void TriggerSync() => _ = ProcessPendingSafelyAsync();

    private async Task ProcessPendingSafelyAsync()
    {
        try { await ProcessPendingAsync(); }
        catch (Exception exception)
        {
            logger.LogWarning("Background outbox processing stopped with {ErrorCode}", exception.GetType().Name);
            // Queue state retains the item; foreground refresh surfaces connectivity/auth.
        }
    }

    public async Task ProcessPendingAsync(CancellationToken ct = default)
    {
        if (!network.IsInternetAvailable || auth.CurrentUser is null) return;
        await processLock.WaitAsync(ct);
        try
        {
            var userId = GetUserId();
            var pending = await database.GetPendingOutboxAsync(userId);
            logger.LogInformation("Outbox processing started with {QueueDepth} retryable items", pending.Count);
            foreach (var item in pending)
            {
                ct.ThrowIfCancellationRequested();
                if (auth.CurrentUser?.Id != userId) return;
                await database.MarkOutboxSyncingAsync(item.OperationId);
                try
                {
                    if (item.EntityType == "saving_transaction")
                    {
                        var savingTransaction = JsonSerializer.Deserialize<SavingTransactionDto>(item.PayloadJson)!;
                        var response = await SendSavingAsync(item, savingTransaction, ct);
                        await database.CompleteSavingOutboxAsync(item.OperationId, response);
                    }
                    else
                    {
                        var transaction = JsonSerializer.Deserialize<TransactionDto>(item.PayloadJson)!;
                        var response = await SendAsync(item, transaction, ct);
                        await database.CompleteOutboxAsync(item.OperationId, response);
                    }
                    logger.LogInformation("Outbox item synchronized on attempt {AttemptCount}", item.AttemptCount + 1);
                }
                catch (ApiException exception)
                {
                    var permanent = IsPermanentFailure(exception.StatusCode);
                    await database.FailOutboxAsync(item.OperationId, exception.Message, permanent);
                    logger.LogWarning("Outbox item failed with {FailureClass}; permanent={Permanent}",
                        exception.StatusCode?.ToString() ?? "Transport", permanent);
                    if (!permanent)
                    {
                        ScheduleRetry(LocalDatabase.OutboxRetryDelay(item.AttemptCount + 1));
                        return;
                    }
                }
            }
        }
        finally
        {
            processLock.Release();
        }
    }

    public async Task RetryAsync(string localId)
    {
        await database.RetryOutboxAsync(GetUserId(), localId);
        TriggerSync();
    }

    public Task CancelAsync(string localId) => database.CancelOutboxAsync(GetUserId(), localId);

    public Task<bool> HasUnresolvedAsync() => database.HasUnresolvedOutboxAsync(GetUserId());

    private async Task<TransactionDto?> SendAsync(LocalOutboxItem item,
        TransactionDto transaction, CancellationToken ct)
    {
        if (item.OperationType == "delete")
        {
            await api.DeleteTransactionAsync(item.ServerId!.Value,
                new DeleteTransactionRequest(item.OperationId, item.ExpectedVersion!.Value), ct);
            return null;
        }

        var expectedVersion = item.OperationType == "update" ? item.ExpectedVersion : null;
        return transaction.Type switch
        {
            "income" when item.OperationType == "update" => await api.UpdateIncomeAsync(item.ServerId!.Value,
                new(transaction.ToWalletId!.Value, transaction.Amount, transaction.OccurredAt,
                    transaction.Note, item.OperationId, expectedVersion), ct),
            "expense" when item.OperationType == "update" => await api.UpdateExpenseAsync(item.ServerId!.Value,
                new(transaction.FromWalletId!.Value, transaction.CategoryId!.Value, transaction.Amount,
                    transaction.OccurredAt, transaction.Note, item.OperationId, expectedVersion), ct),
            "transfer" when item.OperationType == "update" => await api.UpdateTransferAsync(item.ServerId!.Value,
                new(transaction.FromWalletId!.Value, transaction.ToWalletId!.Value, transaction.Amount,
                    transaction.OccurredAt, transaction.Note, item.OperationId, expectedVersion), ct),
            "income" => await api.CreateIncomeAsync(new(transaction.ToWalletId!.Value, transaction.Amount,
                transaction.OccurredAt, transaction.Note, item.OperationId), ct),
            "expense" => await api.CreateExpenseAsync(new(transaction.FromWalletId!.Value, transaction.CategoryId!.Value,
                transaction.Amount, transaction.OccurredAt, transaction.Note, item.OperationId), ct),
            "transfer" => await api.CreateTransferAsync(new(transaction.FromWalletId!.Value, transaction.ToWalletId!.Value,
                transaction.Amount, transaction.OccurredAt, transaction.Note, item.OperationId), ct),
            _ => throw new InvalidOperationException($"Unsupported outbox transaction type: {transaction.Type}"),
        };
    }

    private async Task<SavingTransactionDto?> SendSavingAsync(LocalOutboxItem item,
        SavingTransactionDto transaction, CancellationToken ct)
    {
        if (item.OperationType == "delete")
        {
            await api.DeleteSavingTransactionAsync(item.ServerId!.Value,
                new DeleteSavingTransactionRequest(item.OperationId, item.ExpectedVersion!.Value), ct);
            return null;
        }
        object request = transaction.Type switch
        {
            "deposit" => new CreateSavingDepositRequest(transaction.FromWalletId!.Value, transaction.Amount,
                transaction.OccurredAt, transaction.Note, item.OperationId, item.ExpectedVersion),
            "withdrawal" => new CreateSavingWithdrawalRequest(transaction.ToWalletId!.Value, transaction.Amount,
                transaction.OccurredAt, transaction.Note, item.OperationId, item.ExpectedVersion),
            "transfer_out" => new CreateSavingTransferRequest(transaction.RelatedSavingId!.Value, transaction.Amount,
                transaction.OccurredAt, transaction.Note, item.OperationId, item.ExpectedVersion),
            "expense" => new CreateSavingExpenseRequest(transaction.Amount, transaction.OccurredAt,
                transaction.Note, item.OperationId, item.ExpectedVersion),
            _ => throw new InvalidOperationException($"Unsupported saving outbox type: {transaction.Type}"),
        };
        return item.OperationType == "update"
            ? await api.UpdateSavingTransactionAsync(item.ServerId!.Value, request, ct)
            : await api.CreateSavingTransactionAsync(transaction.SavingId, request, ct);
    }

    private int GetUserId() => auth.CurrentUser?.Id
        ?? throw new InvalidOperationException("A signed-in user is required to synchronize transactions.");

    internal static bool IsPermanentFailure(int? statusCode) =>
        statusCode is >= 400 and < 500 and not 401 and not 408 and not 429;

    private void ScheduleRetry(TimeSpan delay) => _ = RetryAfterDelayAsync(delay);

    private async Task RetryAfterDelayAsync(TimeSpan delay)
    {
        if (delay > TimeSpan.Zero) await Task.Delay(delay);
        TriggerSync();
    }
}
