using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Api;

public partial class ApiClient
{
    public Task<List<PeriodDto>> GetPeriodsAsync(CancellationToken ct = default) =>
        SendAsync<List<PeriodDto>>(HttpMethod.Get, "/periods", null, ct);

    public Task<PeriodDto> CreatePeriodAsync(CreatePeriodRequest request, CancellationToken ct = default) =>
        SendAsync<PeriodDto>(HttpMethod.Post, "/periods", request, ct);

    public async Task<PeriodDto?> GetCurrentPeriodAsync(CancellationToken ct = default)
    {
        try
        {
            return await SendAsync<PeriodDto>(HttpMethod.Get, "/periods/current", null, ct);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            // "No open period" is a normal, expected state -- not a real error.
            return null;
        }
    }

    public Task<ClosePeriodResponse> ClosePeriodAsync(int periodId, ClosePeriodRequest request, CancellationToken ct = default) =>
        SendAsync<ClosePeriodResponse>(HttpMethod.Post, $"/periods/{periodId}/close", request, ct);
}
