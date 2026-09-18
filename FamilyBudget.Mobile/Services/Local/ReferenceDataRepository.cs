using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Auth;

namespace FamilyBudget.Mobile.Services.Local;

public sealed class ReferenceDataRepository(
    ILocalDatabase localDatabase,
    IApiClient apiClient,
    IAuthService authService) : IReferenceDataRepository
{
    public Task<IReadOnlyList<CategoryDto>> GetCachedCategoriesAsync() =>
        localDatabase.GetCategoriesAsync(GetCurrentUserId());

    public async Task<IReadOnlyList<CategoryDto>> RefreshCategoriesAsync(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var categories = await apiClient.GetCategoriesAsync(ct);
        await localDatabase.ReplaceCategoriesAsync(userId, categories);
        return categories;
    }

    public Task<IReadOnlyList<PeriodDto>> GetCachedPeriodsAsync() =>
        localDatabase.GetPeriodsAsync(GetCurrentUserId());

    public async Task<IReadOnlyList<PeriodDto>> RefreshPeriodsAsync(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var periods = await apiClient.GetPeriodsAsync(ct);
        await localDatabase.ReplacePeriodsAsync(userId, periods);
        return periods;
    }

    private int GetCurrentUserId() => authService.CurrentUser?.Id
        ?? throw new InvalidOperationException("A signed-in user is required to access the local cache.");
}
