using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Api;

public partial class ApiClient
{
    public Task<List<SavingDto>> GetSavingsAsync(CancellationToken ct = default) =>
        SendAsync<List<SavingDto>>(HttpMethod.Get, "/savings", null, ct);

    public Task<SavingDetailDto> GetSavingAsync(int id, CancellationToken ct = default) =>
        SendAsync<SavingDetailDto>(HttpMethod.Get, $"/savings/{id}", null, ct);

    public Task<SavingDto> CreateSavingAsync(CreateSavingRequest request, CancellationToken ct = default) =>
        SendAsync<SavingDto>(HttpMethod.Post, "/savings", request, ct);

    public Task<SavingDetailDto> UpdateSavingAsync(int id, UpdateSavingRequest request, CancellationToken ct = default) =>
        SendAsync<SavingDetailDto>(HttpMethod.Put, $"/savings/{id}", request, ct);

    public Task<List<SavingTransactionDto>> GetSavingTransactionsAsync(int savingId, CancellationToken ct = default) =>
        SendAsync<List<SavingTransactionDto>>(HttpMethod.Get, $"/savings/{savingId}/transactions", null, ct);

    public Task<SavingTransactionDto> GetSavingTransactionAsync(int id, CancellationToken ct = default) =>
        SendAsync<SavingTransactionDto>(HttpMethod.Get, $"/saving-transactions/{id}", null, ct);

    public Task<SavingTransactionDto> CreateSavingExpenseAsync(int savingId, CreateSavingExpenseRequest request, CancellationToken ct = default) =>
        SendAsync<SavingTransactionDto>(HttpMethod.Post, $"/savings/{savingId}/transactions", request, ct);

    public Task<SavingTransactionDto> UpdateSavingExpenseAsync(int id, CreateSavingExpenseRequest request, CancellationToken ct = default) =>
        SendAsync<SavingTransactionDto>(HttpMethod.Put, $"/saving-transactions/{id}", request, ct);

    public Task DeleteSavingExpenseAsync(int id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"/saving-transactions/{id}", null, ct);
}
