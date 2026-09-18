using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Local;

namespace FamilyBudget.Mobile.Services.Sync;

public interface IOutboxSyncService
{
    Task<TransactionDto> EnqueueAsync(PendingTransactionInput input);
    Task<TransactionDto> EnqueueUpdateAsync(TransactionDto original, PendingTransactionInput input);
    Task EnqueueDeleteAsync(TransactionDto original);
    Task<SavingTransactionDto> EnqueueSavingAsync(PendingSavingTransactionInput input);
    Task<SavingTransactionDto> EnqueueSavingUpdateAsync(SavingTransactionDto original, PendingSavingTransactionInput input);
    Task EnqueueSavingDeleteAsync(SavingTransactionDto original);
    Task ProcessPendingAsync(CancellationToken ct = default);
    void TriggerSync();
    Task RetryAsync(string localId);
    Task CancelAsync(string localId);
    Task<bool> HasUnresolvedAsync();
}
