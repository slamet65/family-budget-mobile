using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Api;

public partial class ApiClient
{
    public Task<TransactionDto> CreateIncomeAsync(CreateIncomeRequest request, CancellationToken ct = default) =>
        SendAsync<TransactionDto>(HttpMethod.Post, "/transactions", request, ct);

    public Task<TransactionDto> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken ct = default) =>
        SendAsync<TransactionDto>(HttpMethod.Post, "/transactions", request, ct);

    public Task<TransactionDto> CreateTransferAsync(CreateTransferRequest request, CancellationToken ct = default) =>
        SendAsync<TransactionDto>(HttpMethod.Post, "/transactions", request, ct);

    public Task<List<TransactionDto>> GetTransactionsAsync(TransactionListQuery query, CancellationToken ct = default)
    {
        var parameters = new List<string>();
        if (query.PeriodId is { } periodId)
        {
            parameters.Add($"periodId={periodId}");
        }
        if (query.WalletId is { } walletId)
        {
            parameters.Add($"walletId={walletId}");
        }
        if (query.Type is { } type)
        {
            parameters.Add($"type={Uri.EscapeDataString(type)}");
        }

        var uri = parameters.Count > 0 ? $"/transactions?{string.Join('&', parameters)}" : "/transactions";
        return SendAsync<List<TransactionDto>>(HttpMethod.Get, uri, null, ct);
    }

    public Task<TransactionDto> GetTransactionAsync(int id, CancellationToken ct = default) =>
        SendAsync<TransactionDto>(HttpMethod.Get, $"/transactions/{id}", null, ct);

    public Task<TransactionDto> UpdateIncomeAsync(int id, CreateIncomeRequest request, CancellationToken ct = default) =>
        SendAsync<TransactionDto>(HttpMethod.Put, $"/transactions/{id}", request, ct);

    public Task<TransactionDto> UpdateExpenseAsync(int id, CreateExpenseRequest request, CancellationToken ct = default) =>
        SendAsync<TransactionDto>(HttpMethod.Put, $"/transactions/{id}", request, ct);

    public Task<TransactionDto> UpdateTransferAsync(int id, CreateTransferRequest request, CancellationToken ct = default) =>
        SendAsync<TransactionDto>(HttpMethod.Put, $"/transactions/{id}", request, ct);

    public Task DeleteTransactionAsync(int id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"/transactions/{id}", null, ct);
}
