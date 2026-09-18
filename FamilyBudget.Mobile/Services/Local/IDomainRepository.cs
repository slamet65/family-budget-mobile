using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Local;

public interface IDomainRepository
{
    Task RefreshAsync(CancellationToken ct = default);
    Task RefreshSavingsAsync(CancellationToken ct = default);
    Task RefreshSavingChangesAsync(IReadOnlyCollection<SyncChangeDto> changes, CancellationToken ct = default);
    Task RefreshBudgetsAsync(IReadOnlyCollection<int> periodIds, CancellationToken ct = default);
    Task RefreshUsersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SavingDto>> GetSavingsAsync();
    Task<SavingDetailDto?> GetSavingAsync(int savingId);
    Task<IReadOnlyList<SavingTransactionDto>> GetSavingTransactionsAsync(int savingId);
    Task<SavingTransactionDto?> GetSavingTransactionAsync(int transactionId);
    Task<IReadOnlyList<BudgetDto>> GetBudgetsAsync(int periodId);
    Task<IReadOnlyList<UserDto>> GetUsersAsync();
}
