namespace FamilyBudget.Mobile.Services.Api.Dtos;

public record CreateIncomeRequest(int ToWalletId, long Amount, DateTimeOffset OccurredAt, string? Note,
    string? ClientMutationId = null, int? ExpectedVersion = null)
{
    public string Type => "income";
}

public record CreateExpenseRequest(int FromWalletId, int CategoryId, long Amount, DateTimeOffset OccurredAt, string? Note,
    string? ClientMutationId = null, int? ExpectedVersion = null)
{
    public string Type => "expense";
}

public record CreateTransferRequest(int FromWalletId, int ToWalletId, long Amount, DateTimeOffset OccurredAt, string? Note,
    string? ClientMutationId = null, int? ExpectedVersion = null)
{
    public string Type => "transfer";
}

public record TransactionDto(
    int Id,
    int? PeriodId,
    string Type,
    int? FromWalletId,
    string? FromWalletName,
    int? ToWalletId,
    string? ToWalletName,
    int? CategoryId,
    string? CategoryName,
    long Amount,
    string? Note,
    int UserId,
    DateTimeOffset OccurredAt,
    DateTimeOffset CreatedAt,
    string? ClientMutationId = null,
    string? LocalId = null,
    string SyncStatus = "synced",
    string? SyncError = null,
    int Version = 1)
{
    public bool IsSynced => SyncStatus == "synced";
    public string SyncStatusText => SyncStatus switch
    {
        "pending" => "Belum tersinkron",
        "syncing" => "Sedang disinkronkan",
        "retry" => "Menunggu koneksi",
        "failed" => $"Gagal: {SyncError}",
        _ => string.Empty,
    };
}

public record TransactionListQuery(int? PeriodId, int? WalletId, string? Type, int? AfterId = null, int? Limit = null);

public record DeleteTransactionRequest(string ClientMutationId, int ExpectedVersion);
