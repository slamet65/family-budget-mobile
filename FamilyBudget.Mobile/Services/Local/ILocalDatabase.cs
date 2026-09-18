using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Local.Entities;

namespace FamilyBudget.Mobile.Services.Local;

public interface ILocalDatabase
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(int userId);
    Task ReplaceCategoriesAsync(int userId, IReadOnlyCollection<CategoryDto> categories);
    Task<IReadOnlyList<PeriodDto>> GetPeriodsAsync(int userId);
    Task ReplacePeriodsAsync(int userId, IReadOnlyCollection<PeriodDto> periods);
    Task<IReadOnlyList<WalletDto>> GetWalletsAsync(int userId);
    Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(int userId, TransactionListQuery query);
    Task<TransactionDto?> GetTransactionAsync(int userId, int serverId);
    Task<DateTimeOffset?> GetLedgerRefreshedAtAsync(int userId);
    Task ReplaceLedgerSnapshotAsync(int userId, IReadOnlyCollection<WalletDto> wallets,
        IReadOnlyCollection<PeriodDto> periods, IReadOnlyCollection<TransactionDto> transactions);
    Task ApplyTransactionChangesAsync(int userId, IReadOnlyCollection<WalletDto> wallets,
        IReadOnlyCollection<TransactionDto> upserts, IReadOnlyCollection<int> deletedIds);
    Task<TransactionDto> EnqueueTransactionAsync(int userId, PendingTransactionInput input);
    Task<TransactionDto> EnqueueTransactionUpdateAsync(int userId, TransactionDto original, PendingTransactionInput input);
    Task EnqueueTransactionDeleteAsync(int userId, TransactionDto original);
    Task<IReadOnlyList<LocalOutboxItem>> GetPendingOutboxAsync(int userId);
    Task MarkOutboxSyncingAsync(string operationId);
    Task CompleteOutboxAsync(string operationId, TransactionDto? serverTransaction);
    Task FailOutboxAsync(string operationId, string error, bool permanent);
    Task RetryOutboxAsync(int userId, string localId);
    Task CancelOutboxAsync(int userId, string localId);
    Task<bool> HasUnresolvedOutboxAsync(int userId);
    Task<long?> GetSyncCursorAsync(int userId);
    Task SetSyncCursorAsync(int userId, long cursor);
    Task ClearSyncCursorAsync(int userId);
    Task<SyncDiagnostics> GetSyncDiagnosticsAsync(int userId);
    Task MarkSyncStartedAsync(int userId);
    Task MarkSyncSucceededAsync(int userId, long cursor);
    Task MarkSyncFailedAsync(int userId, string errorCode);
    Task ReplaceDomainSnapshotAsync(int userId, DomainSnapshot snapshot);
    Task ReplaceSavingsSnapshotAsync(int userId, IReadOnlyCollection<SavingDto> savings,
        IReadOnlyCollection<SavingTransactionDto> transactions);
    Task ReplaceSavingsAsync(int userId, IReadOnlyCollection<int> savingIds,
        IReadOnlyCollection<SavingDto> savings, IReadOnlyCollection<SavingTransactionDto> transactions);
    Task ReplaceBudgetsAsync(int userId, IReadOnlyCollection<int> periodIds, IReadOnlyCollection<BudgetDto> budgets);
    Task ReplaceUsersAsync(int userId, IReadOnlyCollection<UserDto> users);
    Task<int?> GetBudgetPeriodIdAsync(int userId, int budgetId);
    Task<IReadOnlyList<SavingDto>> GetSavingsAsync(int userId);
    Task<IReadOnlyList<SavingTransactionDto>> GetSavingTransactionsAsync(int userId, int savingId);
    Task<SavingTransactionDto?> GetSavingTransactionAsync(int userId, int serverId);
    Task<SavingTransactionDto> EnqueueSavingTransactionAsync(int userId, PendingSavingTransactionInput input);
    Task<SavingTransactionDto> EnqueueSavingTransactionUpdateAsync(int userId, SavingTransactionDto original, PendingSavingTransactionInput input);
    Task EnqueueSavingTransactionDeleteAsync(int userId, SavingTransactionDto original);
    Task CompleteSavingOutboxAsync(string operationId, SavingTransactionDto? serverTransaction);
    Task<IReadOnlyList<BudgetDto>> GetBudgetsAsync(int userId, int periodId);
    Task<IReadOnlyList<UserDto>> GetUsersAsync(int userId);
}
