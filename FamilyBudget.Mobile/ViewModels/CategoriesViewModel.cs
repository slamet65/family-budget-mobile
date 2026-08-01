using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class CategoriesViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    public ObservableCollection<CategoryGroup> Groups { get; } = [];

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        var categories = await apiClient.GetCategoriesAsync();

        var topLevel = categories.Where(c => c.ParentId is null).OrderBy(c => c.Name);

        Groups.Clear();
        foreach (var parent in topLevel)
        {
            var children = categories.Where(c => c.ParentId == parent.Id).OrderBy(c => c.Name).ToList();
            Groups.Add(new CategoryGroup(parent.Name, children.Count > 0 ? children : [parent]));
        }
    });
}
