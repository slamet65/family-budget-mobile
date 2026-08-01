using CommunityToolkit.Maui;
using FamilyBudget.Mobile.Common;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Auth;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels;
using FamilyBudget.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace FamilyBudget.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		RegisterServices(builder.Services);
		RegisterPagesAndViewModels(builder.Services);

		return builder.Build();
	}

	private static void RegisterServices(IServiceCollection services)
	{
		services.AddSingleton<IAuthService, AuthService>();
		services.AddSingleton<SessionExpiredNotifier>();
		services.AddSingleton<IUserFeedbackService, UserFeedbackService>();

		services.AddTransient<AuthTokenHandler>();
		services.AddHttpClient<IApiClient, ApiClient>(client =>
			{
				client.BaseAddress = new Uri(ApiConfig.BaseUrl);
				client.Timeout = TimeSpan.FromSeconds(30);
			})
			.AddHttpMessageHandler<AuthTokenHandler>();
	}

	private static void RegisterPagesAndViewModels(IServiceCollection services)
	{
		services.AddSingleton<AppShell>();

		services.AddTransient<SplashPage>();
		services.AddTransient<SplashViewModel>();

		services.AddTransient<LoginPage>();
		services.AddTransient<LoginViewModel>();

		services.AddTransient<WalletsPage>();
		services.AddTransient<WalletsViewModel>();

		services.AddTransient<WalletCreatePage>();
		services.AddTransient<WalletCreateViewModel>();

		services.AddTransient<TransactionsPage>();
		services.AddTransient<TransactionsViewModel>();

		services.AddTransient<TransactionFormPage>();
		services.AddTransient<TransactionFormViewModel>();

		services.AddTransient<BudgetsPage>();
		services.AddTransient<BudgetsViewModel>();

		services.AddTransient<BudgetEditPage>();
		services.AddTransient<BudgetEditViewModel>();

		services.AddTransient<CategoriesPage>();
		services.AddTransient<CategoriesViewModel>();

		services.AddTransient<CategoryCreatePage>();
		services.AddTransient<CategoryCreateViewModel>();

		services.AddTransient<PeriodsPage>();
		services.AddTransient<PeriodsViewModel>();

		services.AddTransient<PeriodCreatePage>();
		services.AddTransient<PeriodCreateViewModel>();

		services.AddTransient<PeriodClosePage>();
		services.AddTransient<PeriodCloseViewModel>();

		services.AddTransient<MorePage>();
		services.AddTransient<MoreViewModel>();

		services.AddTransient<AddUserPage>();
		services.AddTransient<AddUserViewModel>();

		services.AddTransient<FamilyMembersPage>();
		services.AddTransient<FamilyMembersViewModel>();

		services.AddTransient<ResetPasswordPage>();
		services.AddTransient<ResetPasswordViewModel>();
	}
}
