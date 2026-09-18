using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Local;

public interface IReferenceDataRepository
{
    Task<IReadOnlyList<CategoryDto>> GetCachedCategoriesAsync();
    Task<IReadOnlyList<CategoryDto>> RefreshCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PeriodDto>> GetCachedPeriodsAsync();
    Task<IReadOnlyList<PeriodDto>> RefreshPeriodsAsync(CancellationToken ct = default);
}
