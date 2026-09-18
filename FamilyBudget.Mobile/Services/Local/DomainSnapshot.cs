using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Local;

public sealed record DomainSnapshot(
    IReadOnlyCollection<SavingDto> Savings,
    IReadOnlyCollection<SavingTransactionDto> SavingTransactions,
    IReadOnlyCollection<BudgetDto> Budgets,
    IReadOnlyCollection<UserDto> Users);
