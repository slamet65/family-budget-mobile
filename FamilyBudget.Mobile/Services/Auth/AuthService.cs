using FamilyBudget.Mobile.Services.Api.Dtos;
using Microsoft.Maui.Storage;

namespace FamilyBudget.Mobile.Services.Auth;

public class AuthService : IAuthService
{
    private const string TokenKey = "auth_token";
    private const string UserIdKey = "user_id";
    private const string UserNameKey = "user_name";
    private const string UserEmailKey = "user_email";

    public AuthService()
    {
        CurrentUser = LoadCachedUser();
    }

    public UserDto? CurrentUser { get; private set; }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(TokenKey);
        }
        catch
        {
            // SecureStorage can throw if the Android Keystore-backed key became invalid
            // (e.g. after certain backup/restore or OS-upgrade edge cases) -- treat as logged-out.
            return null;
        }
    }

    public async Task SaveSessionAsync(string token, UserDto user)
    {
        await SecureStorage.Default.SetAsync(TokenKey, token);

        Preferences.Default.Set(UserIdKey, user.Id);
        Preferences.Default.Set(UserNameKey, user.Name);
        Preferences.Default.Set(UserEmailKey, user.Email);
        CurrentUser = user;
    }

    public Task ClearSessionAsync()
    {
        try
        {
            SecureStorage.Default.Remove(TokenKey);
        }
        catch
        {
            // Best-effort -- see GetTokenAsync.
        }

        Preferences.Default.Remove(UserIdKey);
        Preferences.Default.Remove(UserNameKey);
        Preferences.Default.Remove(UserEmailKey);
        CurrentUser = null;

        return Task.CompletedTask;
    }

    private static UserDto? LoadCachedUser()
    {
        var id = Preferences.Default.Get(UserIdKey, -1);
        if (id < 0)
        {
            return null;
        }

        var name = Preferences.Default.Get(UserNameKey, string.Empty);
        var email = Preferences.Default.Get(UserEmailKey, string.Empty);
        return new UserDto(id, name, email);
    }
}
