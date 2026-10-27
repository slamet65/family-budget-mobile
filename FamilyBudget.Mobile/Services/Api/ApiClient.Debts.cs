using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Api;

public partial class ApiClient
{
    public Task<List<DebtDto>> GetDebtsAsync(CancellationToken ct = default) =>
        SendAsync<List<DebtDto>>(HttpMethod.Get, "/debts", null, ct);

    public Task<DebtDetailDto> GetDebtAsync(int id, CancellationToken ct = default) =>
        SendAsync<DebtDetailDto>(HttpMethod.Get, $"/debts/{id}", null, ct);

    public Task<DebtDto> CreateDebtAsync(CreateDebtRequest request, CancellationToken ct = default) =>
        SendAsync<DebtDto>(HttpMethod.Post, "/debts", request, ct);

    public Task<DebtDto> UpdateDebtAsync(int id, UpdateDebtRequest request, CancellationToken ct = default) =>
        SendAsync<DebtDto>(HttpMethod.Put, $"/debts/{id}", request, ct);

    public Task<List<DebtTransactionDto>> GetDebtTransactionsAsync(int debtId, CancellationToken ct = default) =>
        SendAsync<List<DebtTransactionDto>>(HttpMethod.Get, $"/debts/{debtId}/transactions", null, ct);

    public Task<DebtTransactionDto> GetDebtTransactionAsync(int id, CancellationToken ct = default) =>
        SendAsync<DebtTransactionDto>(HttpMethod.Get, $"/debt-transactions/{id}", null, ct);

    public Task<DebtTransactionDto> CreateDebtTransactionAsync(int debtId, object request, CancellationToken ct = default) =>
        SendAsync<DebtTransactionDto>(HttpMethod.Post, $"/debts/{debtId}/transactions", request, ct);
}
