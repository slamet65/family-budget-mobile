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
    string? SourceCategoryName,
    string? Note,
    int UserId,
    DateTimeOffset OccurredAt,
    DateTimeOffset CreatedAt)
{
    public bool IsExpense => Type == "expense";
    public bool IsReadOnly => !IsExpense;
    public string Description => Type switch
    {
        "opening_balance" => "Saldo awal",
        "deposit" => $"Pemasukan dari {SourceCategoryName ?? "transaksi anggaran"}",
        _ => string.IsNullOrWhiteSpace(Note) ? "Pengeluaran tabungan" : Note,
    };
    public string AmountText => $"{(IsExpense ? "−" : "+")} Rp {Amount:N0}";
}

public record CreateSavingExpenseRequest(long Amount, DateTimeOffset OccurredAt, string? Note)
{
    public string Type => "expense";
}
