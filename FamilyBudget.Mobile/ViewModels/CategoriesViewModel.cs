using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.Services.Local;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Sync;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class CategoriesViewModel(IReferenceDataRepository repository, ISyncCoordinator sync,
    IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    public ObservableCollection<CategoryGroup> Groups { get; } = [];

    [RelayCommand]
    private async Task LoadAsync()
    {
        var cached = await repository.GetCachedCategoriesAsync();
        if (cached.Count > 0)
        {
            ReplaceGroups(cached);
        }

        await ExecuteSafelyAsync(async () =>
        {
            await sync.SynchronizeAsync();
            var fresh = await repository.GetCachedCategoriesAsync();
            ReplaceGroups(fresh);
        }, background: cached.Count > 0);
    }

    private void ReplaceGroups(IReadOnlyList<CategoryDto> categories)
    {
        var topLevel = categories.Where(c => c.ParentId is null).OrderBy(c => c.Name);

        Groups.Clear();
        foreach (var parent in topLevel)
        {
            var children = categories.Where(c => c.ParentId == parent.Id).OrderBy(c => c.Name).ToList();
            Groups.Add(new CategoryGroup(parent, children.Count > 0 ? children : [parent]));
        }
    }
}
