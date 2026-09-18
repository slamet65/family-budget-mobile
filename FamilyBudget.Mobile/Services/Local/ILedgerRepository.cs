using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Local;

public interface ILedgerRepository
{
    Task<IReadOnlyList<WalletDto>> GetWalletsAsync();
    Task<IReadOnlyList<PeriodDto>> GetPeriodsAsync();
    Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(TransactionListQuery query);
    Task<TransactionDto?> GetTransactionAsync(int id);
    Task<DateTimeOffset?> GetRefreshedAtAsync();
    Task RefreshAsync(CancellationToken ct = default);
    Task RefreshTransactionChangesAsync(IReadOnlyCollection<SyncChangeDto> changes, CancellationToken ct = default);
}
