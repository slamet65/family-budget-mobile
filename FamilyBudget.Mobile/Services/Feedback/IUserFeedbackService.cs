namespace FamilyBudget.Mobile.Services.Feedback;

public interface IUserFeedbackService
{
    Task ShowErrorDialogAsync(string message);

    Task ShowInfoDialogAsync(string title, string message);

    void ShowErrorSnackbar(string message);

    Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel);
}
