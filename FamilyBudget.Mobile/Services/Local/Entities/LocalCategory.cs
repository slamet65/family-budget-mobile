using SQLite;

namespace FamilyBudget.Mobile.Services.Local.Entities;

[Table("categories")]
public sealed class LocalCategory
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    [Indexed(Name = "categories_user_server_idx", Order = 1, Unique = true)]
    public int UserId { get; set; }

    [Indexed(Name = "categories_user_server_idx", Order = 2, Unique = true)]
    public int ServerId { get; set; }

    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public bool IsCatchAll { get; set; }
    public int? SavingId { get; set; }
    public string? SavingName { get; set; }
    // Added after schema 8; sqlite-net's CreateTable adds the columns to existing databases.
    public int? DebtId { get; set; }
    public string? DebtName { get; set; }
    public long CreatedAtUnixMilliseconds { get; set; }
}
