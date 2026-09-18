using SQLite;

namespace FamilyBudget.Mobile.Services.Local.Entities;

[Table("cache_state")]
public sealed class LocalCacheState
{
    [PrimaryKey] public string Key { get; set; } = string.Empty;
    public long RefreshedAtUnixMilliseconds { get; set; }
}
