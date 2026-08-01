using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Api;

public partial class ApiClient
{
    public Task<List<BudgetDto>> GetBudgetsAsync(int periodId, CancellationToken ct = default) =>
        SendAsync<List<BudgetDto>>(HttpMethod.Get, $"/periods/{periodId}/budgets", null, ct);

    public Task<BudgetUpsertResponseDto> UpsertBudgetAsync(int periodId, int categoryId, UpsertBudgetRequest request, CancellationToken ct = default) =>
        SendAsync<BudgetUpsertResponseDto>(HttpMethod.Put, $"/periods/{periodId}/budgets/{categoryId}", request, ct);
}
