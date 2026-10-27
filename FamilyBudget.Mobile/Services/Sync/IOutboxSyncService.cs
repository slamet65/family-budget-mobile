using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Local;

namespace FamilyBudget.Mobile.Services.Sync;

public interface IOutboxSyncService
{
    Task<TransactionDto> EnqueueAsync(PendingTransactionInput input);
    Task<TransactionDto> EnqueueUpdateAsync(TransactionDto original, PendingTransactionInput input);
    Task EnqueueDeleteAsync(TransactionDto original);
    // Create-only: saving movements are immutable on the API (SAVING_TRANSACTION_IMMUTABLE).
    Task<SavingTransactionDto> EnqueueSavingAsync(PendingSavingTransactionInput input);
    Task ProcessPendingAsync(CancellationToken ct = default);
    void TriggerSync();
    Task RetryAsync(string localId);
    Task CancelAsync(string localId);
    Task<bool> HasUnresolvedAsync();
}
