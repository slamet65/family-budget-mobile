namespace FamilyBudget.Mobile.Services.Api.Dtos;

public record SavingDto(
    int Id,
    string Name,
    string? Note,
    long Balance,
    DateTimeOffset CreatedAt)
{
    public string Initials => GetInitials(Name);

    // Also used for debt cards, with their own fallback.
    internal static string GetInitials(string name, string fallback = "TB")
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0) return fallback;
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
    int Version = 1,
    // Set on a withdrawal spent on a budget category, or a deposit returning that money.
    // Unlike SourceCategoryName, it doesn't make the row read-only.
    int? FundedCategoryId = null,
    string? FundedCategoryName = null,
    // Set on an expense that paid off a debt (debt `saving_payment`).
    int? RelatedDebtId = null,
    string? RelatedDebtName = null)
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
        "deposit" when FundedCategoryName is not null => $"Kembalikan sisa {FundedCategoryName} dari {FromWalletName ?? "dompet"}",
        "deposit" => $"Setor dari {FromWalletName ?? "dompet"}",
        "withdrawal" when FundedCategoryName is not null => $"Tarik untuk {FundedCategoryName} ke {ToWalletName ?? "dompet"}",
        "withdrawal" => $"Tarik ke {ToWalletName ?? "dompet"}",
        "transfer_in" => $"Transfer dari {RelatedSavingName ?? "tabungan"}",
        "transfer_out" => $"Transfer ke {RelatedSavingName ?? "tabungan"}",
        "expense" when RelatedDebtName is not null => $"Bayar utang {RelatedDebtName}",
        _ => string.IsNullOrWhiteSpace(Note) ? "Pengeluaran langsung" : Note,
    };
    public string Detail => string.IsNullOrWhiteSpace(Note) || (Type == "expense" && RelatedDebtName is null) ? string.Empty : Note;
    public string AmountText => $"{(IsOutgoing ? "−" : "+")} Rp {Amount:N0}";
}

public record CreateSavingExpenseRequest(long Amount, DateTimeOffset OccurredAt, string? Note,
    string? ClientMutationId = null)
{
    public string Type => "expense";
}

// CategoryId: null = plain deposit / loan repayment; set = returning unspent money
// that an earlier withdrawal earmarked for that category.
public record CreateSavingDepositRequest(int FromWalletId, long Amount, DateTimeOffset OccurredAt, string? Note,
    string? ClientMutationId = null, int? CategoryId = null)
{
    public string Type => "deposit";
}

// CategoryId: null = a loan to the wallet; set = the money is spent on that category
// and counts toward its budget (fundedAmount) for the open period.
public record CreateSavingWithdrawalRequest(int ToWalletId, long Amount, DateTimeOffset OccurredAt, string? Note,
    string? ClientMutationId = null, int? CategoryId = null)
{
    public string Type => "withdrawal";
}

public record CreateSavingTransferRequest(int ToSavingId, long Amount, DateTimeOffset OccurredAt, string? Note,
    string? ClientMutationId = null)
{
    public string Type => "transfer";
}

