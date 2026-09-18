using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Api;

public partial class ApiClient
{
    public Task<SyncBootstrapDto> GetSyncBootstrapAsync(CancellationToken ct = default) =>
        SendAsync<SyncBootstrapDto>(HttpMethod.Get, "/sync/bootstrap", null, ct);

    public Task<SyncChangesDto> GetSyncChangesAsync(long after, CancellationToken ct = default) =>
        SendAsync<SyncChangesDto>(HttpMethod.Get, $"/sync/changes?after={after}&limit=500", null, ct);
}
