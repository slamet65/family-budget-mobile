using SQLite;

namespace FamilyBudget.Mobile.Services.Local.Entities;

[Table("wallets")]
public sealed class LocalWallet
{
    [PrimaryKey] public string Key { get; set; } = string.Empty;
    [Indexed] public int UserId { get; set; }
    public int ServerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public long Balance { get; set; }
    public long CreatedAtUnixMilliseconds { get; set; }
}
