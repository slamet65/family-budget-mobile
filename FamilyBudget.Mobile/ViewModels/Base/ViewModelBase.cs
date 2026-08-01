using CommunityToolkit.Mvvm.ComponentModel;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Feedback;

namespace FamilyBudget.Mobile.ViewModels.Base;

public abstract partial class ViewModelBase(IUserFeedbackService feedback) : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    /// <summary>
    /// Runs <paramref name="action"/> guarded by <see cref="IsBusy"/>, routing any
    /// <see cref="ApiException"/> to a blocking dialog (foreground actions) or a
    /// non-blocking snackbar (<paramref name="background"/> refreshes).
    /// </summary>
    protected async Task ExecuteSafelyAsync(Func<Task> action, bool background = false)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await action();
        }
        catch (ApiException ex) when (background)
        {
            feedback.ShowErrorSnackbar(ex.Message);
        }
        catch (ApiException ex)
        {
            await feedback.ShowErrorDialogAsync(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
