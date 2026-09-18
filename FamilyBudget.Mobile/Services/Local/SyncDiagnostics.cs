namespace FamilyBudget.Mobile.Services.Local;

public sealed record SyncDiagnostics(
    string Status,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? LastSuccessAt,
    long? LastSuccessCursor,
    int QueueDepth,
    string? LastErrorCode);
