namespace FamilyBudget.Mobile.Services.Api.Dtos;

// Balance = remaining debt. It can go negative (overpaid): the API never rejects a
// payment for exceeding it, so offline-recorded payments always sync.
public record DebtDto(
    int Id,
    string Name,
    string? Note,
    long Balance,
    DateTimeOffset CreatedAt)
{
    public string Initials => SavingDto.GetInitials(Name, "UT");
    public bool IsPaidOff => Balance == 0;
    public bool IsOverpaid => Balance < 0;
    public string StatusText => Balance switch
    {
        0 => "Lunas",
        < 0 => $"Lebih bayar Rp {-Balance:N0}",
        _ => string.Empty,
    };
    public bool HasStatus => Balance <= 0;
}

public record DebtDetailDto(
    int Id,
    string Name,
    string? Note,
    long Balance,
    long OpeningBalance,
    DateTimeOffset? OpeningBalanceDate,
    DateTimeOffset CreatedAt)
{
    public bool IsOverpaid => Balance < 0;
    public string BalanceLabel => Balance < 0 ? "Lebih bayar" : "Sisa utang";
    public long DisplayBalance => Math.Abs(Balance);
    public string StatusText => Balance == 0 ? "Lunas" : string.Empty;
}

public record CreateDebtRequest(
    string Name,
    string? Note,
    long OpeningBalance,
    DateTimeOffset? OpeningBalanceDate);

public record UpdateDebtRequest(
    string Name,
    string? Note,
    long? OpeningBalance = null,
    DateTimeOffset? OpeningBalanceDate = null);

public record DebtTransactionDto(
    int Id,
    int DebtId,
    string Type,
    long Amount,
    int? SourceTransactionId,
    int? SourceSavingTransactionId,
    int? SourceCategoryId,
    string? SourceCategoryName,
    int? FromWalletId,
    string? FromWalletName,
    int? FromSavingId,
    string? FromSavingName,
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
    // Increases the remaining debt; everything else pays it down.
    public bool IsIncrease => Type is "opening_balance" or "increase";
    public bool IsReadOnly => Type is "opening_balance" or "payment";
    public string Description => Type switch
    {
        "opening_balance" => "Saldo awal",
        "increase" => string.IsNullOrWhiteSpace(Note) ? "Tambah utang" : Note,
        "payment" => $"Bayar · {SourceCategoryName ?? "pengeluaran"}",
        "saving_payment" => $"Bayar dari {FromSavingName ?? "tabungan"}",
        "reduction" => string.IsNullOrWhiteSpace(Note) ? "Pengurangan utang" : Note,
        _ => Type,
    };
    // Increase/reduction already show the note as their description; the opening balance's
    // note is just "Saldo awal" again.
    public string Detail => string.IsNullOrWhiteSpace(Note) || Type is "increase" or "reduction" or "opening_balance"
        ? string.Empty : Note;
    public string AmountText => $"{(IsIncrease ? "+" : "−")} Rp {Amount:N0}";
}

public record CreateDebtIncreaseRequest(long Amount, DateTimeOffset OccurredAt, string? Note,
    string? ClientMutationId = null)
{
    public string Type => "increase";
}

public record CreateDebtReductionRequest(long Amount, DateTimeOffset OccurredAt, string? Note,
    string? ClientMutationId = null)
{
    public string Type => "reduction";
}

// Atomically records a saving expense and the debt payment it funds.
public record CreateDebtSavingPaymentRequest(int FromSavingId, long Amount, DateTimeOffset OccurredAt, string? Note,
    string? ClientMutationId = null)
{
    public string Type => "saving_payment";
}
