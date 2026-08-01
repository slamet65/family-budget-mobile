using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

public partial class CategoryCreateViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    private static readonly ParentCategoryOption NoParent = new(null, "Tidak ada (kategori utama)");

    public ObservableCollection<ParentCategoryOption> ParentOptions { get; } = [NoParent];

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private ParentCategoryOption selectedParent = NoParent;

    [ObservableProperty]
    private bool isCatchAll;

    // The catch-all ("Lain-lain") category is a singleton -- the API rejects creating a
    // second one (400) -- so the toggle is only offered when none exists yet.
    [ObservableProperty]
    private bool canBeCatchAll;

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        var categories = await apiClient.GetCategoriesAsync();

        ParentOptions.Clear();
        ParentOptions.Add(NoParent);
        foreach (var topLevel in categories.Where(c => c.ParentId is null).OrderBy(c => c.Name))
        {
            ParentOptions.Add(new ParentCategoryOption(topLevel.Id, topLevel.Name));
        }
        SelectedParent = NoParent;

        CanBeCatchAll = !categories.Any(c => c.IsCatchAll);
        if (!CanBeCatchAll)
        {
            IsCatchAll = false;
        }
    });

    [RelayCommand]
    private Task SaveAsync() => ExecuteSafelyAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await feedback.ShowErrorDialogAsync("Masukkan nama kategori.");
            return;
        }

        await apiClient.CreateCategoryAsync(new CreateCategoryRequest(Name.Trim(), SelectedParent.Id, IsCatchAll));
        await Shell.Current.GoToAsync("..");
    });
}
