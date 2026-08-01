namespace FamilyBudget.Mobile.Services.Api.Dtos;

public record WalletBalanceEntry(int WalletId, long CountedBalance);

public record ClosePeriodRequest(DateTimeOffset NewPeriodStartDate, string? NewPeriodName, List<WalletBalanceEntry> WalletBalances);

public record WalletReconciliationDto(
    int Id,
    int PeriodId,
    int WalletId,
    long SystemBalance,
    long CountedBalance,
    long Difference,
    DateTimeOffset CreatedAt);

// closedBudgets/copiedBudgets are raw budget rows (no joined categoryName/periodName), same
// shape as the PUT-upsert response -- reused rather than duplicated.
public record ClosePeriodResponse(
    PeriodDto ClosedPeriod,
    List<BudgetUpsertResponseDto> ClosedBudgets,
    List<WalletReconciliationDto> Reconciliations,
    PeriodDto NewPeriod,
    List<BudgetUpsertResponseDto> CopiedBudgets);
