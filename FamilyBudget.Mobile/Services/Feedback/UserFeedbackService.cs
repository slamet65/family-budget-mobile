using CommunityToolkit.Maui.Alerts;

namespace FamilyBudget.Mobile.Services.Feedback;

public class UserFeedbackService : IUserFeedbackService
{
    public Task ShowErrorDialogAsync(string message) =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlertAsync("Something went wrong", message, "OK"));

    public Task ShowInfoDialogAsync(string title, string message) =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlertAsync(title, message, "OK"));

    public void ShowErrorSnackbar(string message) =>
        MainThread.BeginInvokeOnMainThread(() => Snackbar.Make(message).Show());

    public Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel) =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlertAsync(title, message, accept, cancel));
}
