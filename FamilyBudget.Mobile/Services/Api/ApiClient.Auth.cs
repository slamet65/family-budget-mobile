using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Api;

public partial class ApiClient
{
    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default) =>
        SendAsync<LoginResponse>(HttpMethod.Post, "/auth/login", request, ct);

    public Task LogoutAsync(CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, "/auth/logout", null, ct);
}
