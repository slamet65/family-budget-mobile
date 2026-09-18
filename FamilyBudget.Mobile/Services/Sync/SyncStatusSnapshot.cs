namespace FamilyBudget.Mobile.Services.Sync;

public sealed record SyncStatusSnapshot(
    string Status,
    DateTimeOffset? LastSuccessAt,
    long? Cursor,
    int QueueDepth,
    string? LastErrorCode);
