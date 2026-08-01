using CommunityToolkit.Mvvm.Messaging;

namespace FamilyBudget.Mobile.Services.Auth;

public sealed class SessionExpiredMessage;

public class SessionExpiredNotifier
{
    public void Notify() =>
        WeakReferenceMessenger.Default.Send(new SessionExpiredMessage());
}
