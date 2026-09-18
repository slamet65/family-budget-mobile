using SQLite;

namespace FamilyBudget.Mobile.Services.Local.Entities;

[Table("transactions")]
public sealed class LocalTransaction
{
    [PrimaryKey] public string Key { get; set; } = string.Empty;
    [Indexed] public int UserId { get; set; }
    public int ServerId { get; set; }
    [Indexed] public string? ClientMutationId { get; set; }
    public string? LocalId { get; set; }
    public string? SyncStatus { get; set; }
    public string? SyncError { get; set; }
    public bool IsDeleted { get; set; }
    [Indexed] public int? PeriodId { get; set; }
    [Indexed] public int? FromWalletId { get; set; }
    [Indexed] public int? ToWalletId { get; set; }
    public string Type { get; set; } = string.Empty;
    public long OccurredAtUnixMilliseconds { get; set; }
    public long CreatedAtUnixMilliseconds { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
}
