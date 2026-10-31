using Microsoft.Extensions.DependencyInjection;

namespace FamilyBudget.Mobile;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		UserAppTheme = AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var shell = Handler!.MauiContext!.Services.GetRequiredService<AppShell>();
		var window = new Window(shell);
#if WINDOWS
		// The layouts are phone-first; open the desktop window at a phone-like size.
		window.Width = 480;
		window.Height = 900;
#endif
		return window;
	}
}