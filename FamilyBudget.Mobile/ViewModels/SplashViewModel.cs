using CommunityToolkit.Mvvm.ComponentModel;
using FamilyBudget.Mobile.Services.Auth;

namespace FamilyBudget.Mobile.ViewModels;

public partial class SplashViewModel(IAuthService authService) : ObservableObject
{
    [ObservableProperty]
    private double progress;

    public async Task StartAsync()
    {
        var tokenTask = authService.GetTokenAsync();

        // Animated over a fixed short duration rather than tied to how long the token
        // check actually takes -- GetTokenAsync (SecureStorage) is normally near-instant,
        // so an un-timed bar would just flash to full instead of reading as a progress bar.
        const int steps = 20;
        for (var i = 1; i <= steps; i++)
        {
            Progress = (double)i / steps;
            await Task.Delay(35);
        }

        var token = await tokenTask;
        await Shell.Current.GoToAsync(string.IsNullOrEmpty(token) ? "//login" : "//main");
    }
}
