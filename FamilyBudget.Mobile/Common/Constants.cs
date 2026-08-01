namespace FamilyBudget.Mobile.Common;

public static class ApiConfig
{
    // 10.0.2.2 is the Android emulator's alias for the host machine's localhost,
    // where `wrangler dev` (default port 8787) runs during local development.
#if DEBUG
    public const string BaseUrl = "http://10.0.2.2:8787";
#else
    public const string BaseUrl = "https://family-budget-api.alannursalim.my.id";
#endif
}
