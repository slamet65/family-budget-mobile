using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.ViewModels;

/// <summary>Wraps a <see cref="BudgetDto"/> with whether its category is the "Lain-lain"
/// catch-all (derived plannedAmount, not directly editable) -- the API's budgets response
/// doesn't say this itself, it has to be cross-referenced against GET /categories.</summary>
public record BudgetDisplayItem(BudgetDto Budget, bool IsCatchAll)
{
    public bool HasSaving => Budget.SavingId is not null;
}
