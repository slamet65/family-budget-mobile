using CommunityToolkit.Mvvm.Messaging;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Auth;
using FamilyBudget.Mobile.Views;

namespace FamilyBudget.Mobile;

public partial class AppShell : Shell
{
    private readonly IApiClient apiClient;
    private readonly IAuthService authService;
    private bool isHandlingSessionExpired;

    public AppShell(IApiClient apiClient, IAuthService authService)
    {
        InitializeComponent();
        this.apiClient = apiClient;
        this.authService = authService;

        // Wallets/Transactions/Budgets/Categories/Periods/Family members are declared directly
        // in AppShell.xaml as FlyoutItems, so they don't need registering here -- only pages
        // pushed on top of the current flyout item do.
        Routing.RegisterRoute("transactionForm", typeof(TransactionFormPage));
        Routing.RegisterRoute("budgetEdit", typeof(BudgetEditPage));
        Routing.RegisterRoute("periodClose", typeof(PeriodClosePage));
        Routing.RegisterRoute("walletCreate", typeof(WalletCreatePage));
        Routing.RegisterRoute("categoryCreate", typeof(CategoryCreatePage));
        Routing.RegisterRoute("savingForm", typeof(SavingFormPage));
        Routing.RegisterRoute("savingDetail", typeof(SavingDetailPage));
        Routing.RegisterRoute("savingExpenseForm", typeof(SavingExpenseFormPage));
        Routing.RegisterRoute("periodCreate", typeof(PeriodCreatePage));
        Routing.RegisterRoute("addUser", typeof(AddUserPage));
        Routing.RegisterRoute("resetPassword", typeof(ResetPasswordPage));

        WeakReferenceMessenger.Default.Register<SessionExpiredMessage>(this, async (_, _) => await HandleSessionExpiredAsync());
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        try
        {
            await apiClient.LogoutAsync();
        }
        catch (ApiException)
        {
            // Best-effort -- the local session is cleared regardless, so a dead/expired
            // token or unreachable server doesn't strand the user unable to log out.
        }

        await authService.ClearSessionAsync();
        await GoToAsync("//login");
    }

    private async Task HandleSessionExpiredAsync()
    {
        if (isHandlingSessionExpired)
        {
            return;
        }

        isHandlingSessionExpired = true;
        try
        {
            await authService.ClearSessionAsync();
            await this.DisplayAlertAsync("Sesi berakhir", "Silakan masuk kembali.", "OK");
            await GoToAsync("//login");
        }
        finally
        {
            isHandlingSessionExpired = false;
        }
    }
}
