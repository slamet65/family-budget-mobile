namespace FamilyBudget.Mobile.Services.Api.Dtos;

// GET /periods/:periodId/budgets — actualAmount/remainingAmount are always populated here:
// computed live while the period is open, frozen onto the row at tutup buku time once closed.
public record BudgetDto(
    int Id,
    int PeriodId,
    string PeriodName,
    int CategoryId,
    string CategoryName,
    int? SavingId,
    string? SavingName,
    long PlannedAmount,
    long ActualAmount,
    long RemainingAmount,
    DateTimeOffset CreatedAt);

public record UpsertBudgetRequest(long PlannedAmount);

// PUT response — no joined names, and actual/remaining are only frozen at period close, so a
// PUT (only ever allowed on an open period) always returns these as null.
public record BudgetUpsertResponseDto(
    int Id,
    int PeriodId,
    int CategoryId,
    long PlannedAmount,
    long? ActualAmount,
    long? RemainingAmount,
    DateTimeOffset CreatedAt);
