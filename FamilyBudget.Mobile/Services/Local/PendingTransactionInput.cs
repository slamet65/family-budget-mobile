namespace FamilyBudget.Mobile.Services.Local;

public sealed record PendingTransactionInput(
    int PeriodId,
    string Type,
    int? FromWalletId,
    string? FromWalletName,
    int? ToWalletId,
    string? ToWalletName,
    int? CategoryId,
    string? CategoryName,
    long Amount,
    string? Note,
    DateTimeOffset OccurredAt);
