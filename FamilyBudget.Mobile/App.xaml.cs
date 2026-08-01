using Microsoft.Extensions.DependencyInjection;

namespace FamilyBudget.Mobile;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var shell = Handler!.MauiContext!.Services.GetRequiredService<AppShell>();
		return new Window(shell);
	}
}