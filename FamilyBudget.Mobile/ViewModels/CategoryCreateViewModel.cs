using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;

namespace FamilyBudget.Mobile.ViewModels;

[QueryProperty(nameof(CategoryIdRaw), "categoryId")]
public partial class CategoryCreateViewModel(IApiClient apiClient, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    private static readonly ParentCategoryOption NoParent = new(null, "Tidak ada (kategori utama)");
    private static readonly SavingOption NoSaving = new(null, "Bukan kategori tabungan");

    public ObservableCollection<ParentCategoryOption> ParentOptions { get; } = [NoParent];
    public ObservableCollection<SavingOption> SavingOptions { get; } = [NoSaving];

    [ObservableProperty]
    private string? categoryIdRaw;

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

    [ObservableProperty]
    private SavingOption selectedSaving = NoSaving;

    [ObservableProperty]
    private bool hasChildren;

    public bool IsEditMode => int.TryParse(CategoryIdRaw, out _);
    public string PageTitle => IsEditMode ? "Ubah Kategori" : "Kategori Baru";
    public bool CanMapToSaving => !HasChildren && !IsCatchAll;

    [RelayCommand]
    private Task LoadAsync() => ExecuteSafelyAsync(async () =>
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));

        var categories = await apiClient.GetCategoriesAsync();
        var current = IsEditMode ? categories.FirstOrDefault(c => c.Id == int.Parse(CategoryIdRaw!)) : null;

        ParentOptions.Clear();
        ParentOptions.Add(NoParent);
        foreach (var topLevel in categories
                     .Where(c => c.ParentId is null && c.Id != current?.Id && c.SavingId is null)
                     .OrderBy(c => c.Name))
        {
            ParentOptions.Add(new ParentCategoryOption(topLevel.Id, topLevel.Name));
        }

        SavingOptions.Clear();
        SavingOptions.Add(NoSaving);
        foreach (var saving in await apiClient.GetSavingsAsync())
        {
            SavingOptions.Add(new SavingOption(saving.Id, saving.Name));
        }

        if (current is not null)
        {
            Name = current.Name;
            SelectedParent = ParentOptions.FirstOrDefault(p => p.Id == current.ParentId) ?? NoParent;
            IsCatchAll = current.IsCatchAll;
            HasChildren = categories.Any(c => c.ParentId == current.Id);
            SelectedSaving = SavingOptions.FirstOrDefault(s => s.Id == current.SavingId) ?? NoSaving;
        }
        else
        {
            SelectedParent = NoParent;
            SelectedSaving = NoSaving;
            HasChildren = false;
        }

        CanBeCatchAll = current?.IsCatchAll == true || !categories.Any(c => c.IsCatchAll);
        if (!CanBeCatchAll)
        {
            IsCatchAll = false;
        }
        OnPropertyChanged(nameof(CanMapToSaving));
    });

    partial void OnIsCatchAllChanged(bool value)
    {
        if (value)
        {
            SelectedSaving = NoSaving;
        }
        OnPropertyChanged(nameof(CanMapToSaving));
    }

    partial void OnHasChildrenChanged(bool value) => OnPropertyChanged(nameof(CanMapToSaving));

    [RelayCommand]
    private Task SaveAsync() => ExecuteSafelyAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await feedback.ShowErrorDialogAsync("Masukkan nama kategori.");
            return;
        }

        if (IsEditMode)
        {
            await apiClient.UpdateCategoryAsync(int.Parse(CategoryIdRaw!),
                new UpdateCategoryRequest(Name.Trim(), SelectedParent.Id, IsCatchAll, CanMapToSaving ? SelectedSaving.Id : null));
        }
        else
        {
            await apiClient.CreateCategoryAsync(
                new CreateCategoryRequest(Name.Trim(), SelectedParent.Id, IsCatchAll, SelectedSaving.Id));
        }
        await Shell.Current.GoToAsync("..");
    });
}
