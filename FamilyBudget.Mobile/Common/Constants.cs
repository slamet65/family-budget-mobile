namespace FamilyBudget.Mobile.Common;

public static class ApiConfig
{
    // `wrangler dev` (default port 8787) runs on the host machine during local development.
    // 10.0.2.2 is the Android emulator's alias for the host's localhost; the Windows
    // desktop build runs on the host itself.
#if DEBUG && ANDROID
    public const string BaseUrl = "http://10.0.2.2:8787";
#elif DEBUG
    public const string BaseUrl = "http://localhost:8787";
#else
    public const string BaseUrl = "https://family-budget-api.alannursalim.my.id";
#endif
}
