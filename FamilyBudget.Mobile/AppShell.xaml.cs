using CommunityToolkit.Mvvm.Messaging;
using FamilyBudget.Mobile.Services.Auth;
using FamilyBudget.Mobile.Views;

namespace FamilyBudget.Mobile;

public partial class AppShell : Shell
{
    private readonly IAuthService authService;
    private bool isHandlingSessionExpired;

    public AppShell(IAuthService authService)
    {
        InitializeComponent();
        this.authService = authService;

        Routing.RegisterRoute("categories", typeof(CategoriesPage));
        Routing.RegisterRoute("periods", typeof(PeriodsPage));
        Routing.RegisterRoute("transactionForm", typeof(TransactionFormPage));
        Routing.RegisterRoute("budgetEdit", typeof(BudgetEditPage));
        Routing.RegisterRoute("periodClose", typeof(PeriodClosePage));
        Routing.RegisterRoute("walletCreate", typeof(WalletCreatePage));
        Routing.RegisterRoute("categoryCreate", typeof(CategoryCreatePage));
        Routing.RegisterRoute("periodCreate", typeof(PeriodCreatePage));
        Routing.RegisterRoute("addUser", typeof(AddUserPage));
        Routing.RegisterRoute("familyMembers", typeof(FamilyMembersPage));
        Routing.RegisterRoute("resetPassword", typeof(ResetPasswordPage));

        WeakReferenceMessenger.Default.Register<SessionExpiredMessage>(this, async (_, _) => await HandleSessionExpiredAsync());
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
            await this.DisplayAlertAsync("Session expired", "Please log in again.", "OK");
            await GoToAsync("//login");
        }
        finally
        {
            isHandlingSessionExpired = false;
        }
    }
}
