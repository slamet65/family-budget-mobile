using SQLite;

namespace FamilyBudget.Mobile.Services.Local.Entities;

[Table("domain_entities")]
public sealed class LocalDomainEntity
{
    [PrimaryKey] public string Key { get; set; } = string.Empty;
    [Indexed] public int UserId { get; set; }
    [Indexed] public string EntityType { get; set; } = string.Empty;
    [Indexed] public int ServerId { get; set; }
    [Indexed] public int? GroupId { get; set; }
    public string? LocalId { get; set; }
    public string? SyncStatus { get; set; }
    public string? SyncError { get; set; }
    public bool IsDeleted { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
}
