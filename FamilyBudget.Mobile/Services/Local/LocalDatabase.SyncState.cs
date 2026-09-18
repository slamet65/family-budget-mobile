using FamilyBudget.Mobile.Services.Local.Entities;

namespace FamilyBudget.Mobile.Services.Local;

public sealed partial class LocalDatabase
{
    public async Task ClearSyncCursorAsync(int userId)
    {
        var db = await GetConnectionAsync();
        await db.ExecuteAsync("DELETE FROM cache_state WHERE Key=?", $"{userId}:sync-cursor");
    }

    public async Task<SyncDiagnostics> GetSyncDiagnosticsAsync(int userId)
    {
        var db = await GetConnectionAsync();
        var row = await db.FindAsync<LocalSyncState>($"{userId}:sync");
        var queueDepth = await db.Table<LocalOutboxItem>().Where(item => item.UserId == userId).CountAsync();
        return new(
            row?.Status ?? "idle",
            FromUnixMilliseconds(row?.LastAttemptAtUnixMilliseconds),
            FromUnixMilliseconds(row?.LastSuccessAtUnixMilliseconds),
            row?.LastSuccessCursor,
            queueDepth,
            row?.LastErrorCode);
    }

    public Task MarkSyncStartedAsync(int userId) => UpdateSyncStateAsync(userId, state =>
    {
        state.Status = "syncing";
        state.LastAttemptAtUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        state.LastErrorCode = null;
    });

    public Task MarkSyncSucceededAsync(int userId, long cursor) => UpdateSyncStateAsync(userId, state =>
    {
        state.Status = "synced";
        state.LastSuccessAtUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        state.LastSuccessCursor = cursor;
        state.LastErrorCode = null;
    });

    public Task MarkSyncFailedAsync(int userId, string errorCode) => UpdateSyncStateAsync(userId, state =>
    {
        state.Status = "failed";
        // Store only a bounded exception/category code, never response bodies, notes,
        // amounts, tokens, or other financial/user payloads.
        state.LastErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "Unknown" : errorCode[..Math.Min(80, errorCode.Length)];
    });

    private async Task UpdateSyncStateAsync(int userId, Action<LocalSyncState> update)
    {
        var db = await GetConnectionAsync();
        var state = await db.FindAsync<LocalSyncState>($"{userId}:sync")
            ?? new LocalSyncState { Key = $"{userId}:sync", UserId = userId };
        state.QueueDepth = await db.Table<LocalOutboxItem>().Where(item => item.UserId == userId).CountAsync();
        update(state);
        await db.InsertOrReplaceAsync(state);
    }

    private static DateTimeOffset? FromUnixMilliseconds(long? value) =>
        value is { } milliseconds ? DateTimeOffset.FromUnixTimeMilliseconds(milliseconds) : null;
}
