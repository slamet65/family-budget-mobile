using SQLite;

namespace FamilyBudget.Mobile.Services.Local.Entities;

[Table("sync_state")]
public sealed class LocalSyncState
{
    [PrimaryKey] public string Key { get; set; } = string.Empty;
    [Indexed] public int UserId { get; set; }
    public string Status { get; set; } = "idle";
    public long? LastAttemptAtUnixMilliseconds { get; set; }
    public long? LastSuccessAtUnixMilliseconds { get; set; }
    public long? LastSuccessCursor { get; set; }
    public int QueueDepth { get; set; }
    public string? LastErrorCode { get; set; }
}
