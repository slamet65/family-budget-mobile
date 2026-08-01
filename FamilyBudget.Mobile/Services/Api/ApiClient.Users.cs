using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Api;

public partial class ApiClient
{
    public Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default) =>
        SendAsync<UserDto>(HttpMethod.Post, "/users", request, ct);

    public Task<List<UserDto>> GetUsersAsync(CancellationToken ct = default) =>
        SendAsync<List<UserDto>>(HttpMethod.Get, "/users", null, ct);

    public Task<UserDto> ResetPasswordAsync(int userId, ResetPasswordRequest request, CancellationToken ct = default) =>
        SendAsync<UserDto>(HttpMethod.Post, $"/users/{userId}/reset-password", request, ct);
}
