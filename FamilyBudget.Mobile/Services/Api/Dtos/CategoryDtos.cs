using System.Text.Json.Serialization;

namespace FamilyBudget.Mobile.Services.Api.Dtos;

// isCatchAll: at most one category has this set ("Lain-lain") -- its budget planned
// amount is derived server-side (current total wallet balance minus every other
// category's planned amount), not settable via PUT /periods/:periodId/budgets/:categoryId
// (the API rejects that with a 400). GET /periods/:periodId/budgets does not itself
// say which row is the catch-all one, so callers must cross-reference by categoryId.
public record CategoryDto(
    int Id,
    string Name,
    int? ParentId,
    bool IsCatchAll,
    int? SavingId,
    string? SavingName,
    DateTimeOffset CreatedAt,
    // A leaf maps to a saving or a debt, never both. Expenses in a debt category pay it off.
    int? DebtId = null,
    string? DebtName = null)
{
    public bool HasSaving => SavingId is not null;
    public bool HasDebt => DebtId is not null;
}

public record CreateCategoryRequest(string Name, int? ParentId, bool IsCatchAll, int? SavingId = null,
    int? DebtId = null);

// Mappings are always written, null included: the API keeps the existing value when a
// field is absent, so with the global WhenWritingNull a mapping could never be removed.
public record UpdateCategoryRequest(
    string Name,
    int? ParentId,
    bool IsCatchAll,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] int? SavingId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] int? DebtId = null);
