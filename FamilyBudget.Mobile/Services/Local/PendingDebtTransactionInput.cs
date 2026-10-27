namespace FamilyBudget.Mobile.Services.Local;

// Type: increase | reduction | saving_payment. `payment` rows are never queued; the API
// creates them from expenses in a debt-mapped category.
public sealed record PendingDebtTransactionInput(
    int DebtId,
    string Type,
    long Amount,
    int? FromSavingId,
    string? FromSavingName,
    string? Note,
    DateTimeOffset OccurredAt);
