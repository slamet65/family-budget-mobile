using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Api;

public partial class ApiClient
{
    public Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default) =>
        SendAsync<List<CategoryDto>>(HttpMethod.Get, "/categories", null, ct);

    public Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default) =>
        SendAsync<CategoryDto>(HttpMethod.Post, "/categories", request, ct);

    public Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default) =>
        SendAsync<CategoryDto>(HttpMethod.Put, $"/categories/{id}", request, ct);
}
