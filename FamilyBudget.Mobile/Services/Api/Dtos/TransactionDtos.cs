namespace FamilyBudget.Mobile.Services.Api.Dtos;

public record CreateIncomeRequest(int ToWalletId, long Amount, DateTimeOffset OccurredAt, string? Note)
{
    public string Type => "income";
}

public record CreateExpenseRequest(int FromWalletId, int CategoryId, long Amount, DateTimeOffset OccurredAt, string? Note)
{
    public string Type => "expense";
}

public record CreateTransferRequest(int FromWalletId, int ToWalletId, long Amount, DateTimeOffset OccurredAt, string? Note)
{
    public string Type => "transfer";
}

public record TransactionDto(
    int Id,
    int PeriodId,
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
    DateTimeOffset CreatedAt);

public record TransactionListQuery(int? PeriodId, int? WalletId, string? Type);
