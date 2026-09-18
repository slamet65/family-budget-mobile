using SQLite;

namespace FamilyBudget.Mobile.Services.Local.Entities;

[Table("outbox")]
public sealed class LocalOutboxItem
{
    [PrimaryKey] public string OperationId { get; set; } = string.Empty;
    [Indexed] public int UserId { get; set; }
    public string EntityLocalId { get; set; } = string.Empty;
    public string EntityType { get; set; } = "transaction";
    public string OperationType { get; set; } = "create";
    public int? ServerId { get; set; }
    public int? ExpectedVersion { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string? PreviousPayloadJson { get; set; }
    public string Status { get; set; } = "pending";
    public int AttemptCount { get; set; }
    public long CreatedAtUnixMilliseconds { get; set; }
    public long? LastAttemptAtUnixMilliseconds { get; set; }
    public long? NextAttemptAtUnixMilliseconds { get; set; }
    public string? LastError { get; set; }
}
