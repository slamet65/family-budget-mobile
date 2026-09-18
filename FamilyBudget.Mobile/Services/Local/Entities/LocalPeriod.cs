using SQLite;

namespace FamilyBudget.Mobile.Services.Local.Entities;

[Table("periods")]
public sealed class LocalPeriod
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    [Indexed(Name = "periods_user_server_idx", Order = 1, Unique = true)]
    public int UserId { get; set; }

    [Indexed(Name = "periods_user_server_idx", Order = 2, Unique = true)]
    public int ServerId { get; set; }

    public string? Name { get; set; }
    public long StartDateUnixMilliseconds { get; set; }
    public string Status { get; set; } = string.Empty;
    public long? ClosedAtUnixMilliseconds { get; set; }
    public long CreatedAtUnixMilliseconds { get; set; }
}
