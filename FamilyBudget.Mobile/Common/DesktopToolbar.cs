using System.Windows.Input;

namespace FamilyBudget.Mobile.Common;

public static class DesktopToolbar
{
    // RefreshView's pull-to-refresh needs a touch drag; on the Windows desktop build
    // give mouse users a toolbar button that runs the same command instead.
    public static void AddRefresh(ContentPage page, ICommand command)
    {
#if WINDOWS
        page.ToolbarItems.Add(new ToolbarItem { Text = "Muat ulang", Command = command });
#endif
    }
}
