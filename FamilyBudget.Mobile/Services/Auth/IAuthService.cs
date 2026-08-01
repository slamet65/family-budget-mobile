using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Auth;

public interface IAuthService
{
    UserDto? CurrentUser { get; }

    Task<string?> GetTokenAsync();

    Task SaveSessionAsync(string token, UserDto user);

    Task ClearSessionAsync();
}
