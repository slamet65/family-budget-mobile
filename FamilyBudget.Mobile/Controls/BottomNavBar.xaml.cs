namespace FamilyBudget.Mobile.Controls;

// Shell's native TabBar chrome is hidden (Shell.TabBarIsVisible="False" on each tab page) in
// favor of this custom-styled bar, since the Material Components version bundled with the
// current Android workload doesn't support the M3 "active indicator" pill attribute
// (itemActiveIndicatorColor) needed to match the approved mockup natively. Still routes
// through Shell (Shell.Current.GoToAsync) so back-stack/tab-switching behavior is unchanged.
public partial class BottomNavBar : ContentView
{
    public static readonly BindableProperty ActiveTabProperty = BindableProperty.Create(
        nameof(ActiveTab), typeof(string), typeof(BottomNavBar), "wallets",
        propertyChanged: (bindable, _, _) => ((BottomNavBar)bindable).ApplyActiveTab());

    public string ActiveTab
    {
        get => (string)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    public BottomNavBar()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyActiveTab();
    }

    private void ApplyActiveTab()
    {
        var activeColor = (Color)Application.Current!.Resources["SecondaryContainer"];
        var activeTextColor = (Color)Application.Current!.Resources["OnSecondaryContainer"];
        var inactiveTextColor = (Color)Application.Current!.Resources["OnSurfaceVariant"];

        foreach (var (key, pill, icon, label) in Items)
        {
            var isActive = key == ActiveTab;
            pill.BackgroundColor = isActive ? activeColor : Colors.Transparent;
            icon.TextColor = label.TextColor = isActive ? activeTextColor : inactiveTextColor;
            label.FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None;
        }
    }

    private IEnumerable<(string Key, Border Pill, Label Icon, Label Label)> Items =>
    [
        ("wallets", WalletsPill, WalletsIcon, WalletsLabel),
        ("transactions", TransactionsPill, TransactionsIcon, TransactionsLabel),
        ("budgets", BudgetsPill, BudgetsIcon, BudgetsLabel),
        ("more", MorePill, MoreIcon, MoreLabel),
    ];

    private static async void OnWalletsTapped(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/wallets");

    private static async void OnTransactionsTapped(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/transactions");

    private static async void OnBudgetsTapped(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/budgets");

    private static async void OnMoreTapped(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/more");
}
