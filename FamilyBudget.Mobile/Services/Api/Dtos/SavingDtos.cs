namespace FamilyBudget.Mobile.Services.Api.Dtos;

public record SavingDto(
    int Id,
    string Name,
    string? Note,
    long Balance,
    DateTimeOffset CreatedAt)
{
    public string Initials => GetInitials(Name);

    private static string GetInitials(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0) return "TB";
        if (words.Length == 1)
        {
            return words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant();
        }
        return string.Concat(words[0][0], words[1][0]).ToUpperInvariant();
    }
}

public record SavingDetailDto(
    int Id,
    string Name,
    string? Note,
    long Balance,
    long OpeningBalance,
    DateTimeOffset? OpeningBalanceDate,
    DateTimeOffset CreatedAt);

public record CreateSavingRequest(
    string Name,
    string? Note,
    long OpeningBalance,
    DateTimeOffset? OpeningBalanceDate);

public record UpdateSavingRequest(
    string Name,
    string? Note,
    long? OpeningBalance = null,
    DateTimeOffset? OpeningBalanceDate = null);

public record SavingTransactionDto(
    int Id,
    int SavingId,
    string Type,
    long Amount,
    int? SourceTransactionId,
    string? TransferGroupId,
    string? SourceCategoryName,
    int? FromWalletId,
    string? FromWalletName,
    int? ToWalletId,
    string? ToWalletName,
    int? RelatedSavingId,
    int? RelatedTransactionId,
    string? RelatedSavingName,
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
    public bool IsOutgoing => Type is "withdrawal" or "expense" or "transfer_out";
    public bool IsReadOnly => Type == "opening_balance" || (Type == "deposit" && SourceCategoryName is not null);
    public string Description => Type switch
    {
        "opening_balance" => "Saldo awal",
        "deposit" when SourceCategoryName is not null => $"Alokasi · {SourceCategoryName}",
        "deposit" => $"Setor dari {FromWalletName ?? "dompet"}",
        "withdrawal" => $"Tarik ke {ToWalletName ?? "dompet"}",
        "transfer_in" => $"Transfer dari {RelatedSavingName ?? "tabungan"}",
        "transfer_out" => $"Transfer ke {RelatedSavingName ?? "tabungan"}",
        _ => string.IsNullOrWhiteSpace(Note) ? "Pengeluaran langsung" : Note,
    };
    public string Detail => string.IsNullOrWhiteSpace(Note) || Type == "expense" ? string.Empty : Note;
    public string AmountText => $"{(IsOutgoing ? "−" : "+")} Rp {Amount:N0}";
}

public record CreateSavingExpenseRequest(long Amount, DateTimeOffset OccurredAt, string? Note,
    string? ClientMutationId = null, int? ExpectedVersion = null)
{
    public string Type => "expense";
}

public record CreateSavingDepositRequest(int FromWalletId, long Amount, DateTimeOffset OccurredAt, string? Note,
    string? ClientMutationId = null, int? ExpectedVersion = null)
{
    public string Type => "deposit";
}

public record CreateSavingWithdrawalRequest(int ToWalletId, long Amount, DateTimeOffset OccurredAt, string? Note,
    string? ClientMutationId = null, int? ExpectedVersion = null)
{
    public string Type => "withdrawal";
}

public record CreateSavingTransferRequest(int ToSavingId, long Amount, DateTimeOffset OccurredAt, string? Note,
    string? ClientMutationId = null, int? ExpectedVersion = null)
{
    public string Type => "transfer";
}

public record DeleteSavingTransactionRequest(string ClientMutationId, int ExpectedVersion);
