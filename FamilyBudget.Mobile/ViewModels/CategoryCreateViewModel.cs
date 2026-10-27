using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Feedback;
using FamilyBudget.Mobile.ViewModels.Base;
using FamilyBudget.Mobile.Services.Local;

namespace FamilyBudget.Mobile.ViewModels;

[QueryProperty(nameof(CategoryIdRaw), "categoryId")]
public partial class CategoryCreateViewModel(IApiClient apiClient, IReferenceDataRepository references,
    IDomainRepository domain, IUserFeedbackService feedback) : ViewModelBase(feedback)
{
    private static readonly ParentCategoryOption NoParent = new(null, "Tidak ada (kategori utama)");
    private static readonly SavingOption NoSaving = new(null, "Bukan kategori tabungan");
    private static readonly DebtOption NoDebt = new(null, "Bukan kategori utang");

    public ObservableCollection<ParentCategoryOption> ParentOptions { get; } = [NoParent];
    public ObservableCollection<SavingOption> SavingOptions { get; } = [NoSaving];
    public ObservableCollection<DebtOption> DebtOptions { get; } = [NoDebt];

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

    // A leaf maps to a saving or a debt, never both (CATEGORY_MAPPING_CONFLICT):
    // picking one resets the other.
    [ObservableProperty]
    private DebtOption selectedDebt = NoDebt;

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

        var categories = await references.GetCachedCategoriesAsync();
        var current = IsEditMode ? categories.FirstOrDefault(c => c.Id == int.Parse(CategoryIdRaw!)) : null;

        ParentOptions.Clear();
        ParentOptions.Add(NoParent);
        foreach (var topLevel in categories
                     .Where(c => c.ParentId is null && c.Id != current?.Id && c.SavingId is null && c.DebtId is null)
                     .OrderBy(c => c.Name))
        {
            ParentOptions.Add(new ParentCategoryOption(topLevel.Id, topLevel.Name));
        }

        SavingOptions.Clear();
        SavingOptions.Add(NoSaving);
        foreach (var saving in await domain.GetSavingsAsync())
        {
            SavingOptions.Add(new SavingOption(saving.Id, saving.Name));
        }

        DebtOptions.Clear();
        DebtOptions.Add(NoDebt);
        foreach (var debt in (await domain.GetDebtsAsync()).OrderBy(d => d.Name))
        {
            DebtOptions.Add(new DebtOption(debt.Id, debt.Name));
        }

        if (current is not null)
        {
            Name = current.Name;
            SelectedParent = ParentOptions.FirstOrDefault(p => p.Id == current.ParentId) ?? NoParent;
            IsCatchAll = current.IsCatchAll;
            HasChildren = categories.Any(c => c.ParentId == current.Id);
            SelectedSaving = SavingOptions.FirstOrDefault(s => s.Id == current.SavingId) ?? NoSaving;
            SelectedDebt = DebtOptions.FirstOrDefault(d => d.Id == current.DebtId) ?? NoDebt;
        }
        else
        {
            SelectedParent = NoParent;
            SelectedSaving = NoSaving;
            SelectedDebt = NoDebt;
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
            SelectedDebt = NoDebt;
        }
        OnPropertyChanged(nameof(CanMapToSaving));
    }

    partial void OnSelectedSavingChanged(SavingOption value)
    {
        if (value?.Id is not null) SelectedDebt = NoDebt;
    }

    partial void OnSelectedDebtChanged(DebtOption value)
    {
        if (value?.Id is not null) SelectedSaving = NoSaving;
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
                new UpdateCategoryRequest(Name.Trim(), SelectedParent.Id, IsCatchAll,
                    CanMapToSaving ? SelectedSaving.Id : null,
                    CanMapToSaving ? SelectedDebt.Id : null));
        }
        else
        {
            await apiClient.CreateCategoryAsync(
                new CreateCategoryRequest(Name.Trim(), SelectedParent.Id, IsCatchAll, SelectedSaving.Id, SelectedDebt.Id));
        }
        await Shell.Current.GoToAsync("..");
    });
}
