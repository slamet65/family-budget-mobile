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
    DateTimeOffset CreatedAt)
{
    public bool HasSaving => SavingId is not null;
}

public record CreateCategoryRequest(string Name, int? ParentId, bool IsCatchAll, int? SavingId = null);

public record UpdateCategoryRequest(string Name, int? ParentId, bool IsCatchAll, int? SavingId);
