namespace FamilyBudget.Mobile.Services.Local;

public sealed record PendingSavingTransactionInput(
    int SavingId,
    string Type,
    long Amount,
    int? FromWalletId,
    string? FromWalletName,
    int? ToWalletId,
    string? ToWalletName,
    int? RelatedSavingId,
    string? RelatedSavingName,
    string? Note,
    DateTimeOffset OccurredAt);
