using System.Collections.ObjectModel;
using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.ViewModels;

/// <summary>
/// One top-level category and the rows shown under it: its sub-categories, or
/// itself alone when it has none (so parent-only categories, e.g. "Internet", stay visible).
/// </summary>
public class CategoryGroup(CategoryDto parent, IEnumerable<CategoryDto> items) : ObservableCollection<CategoryDto>(items)
{
    public string Name => parent.Name;
    public CategoryDto Parent { get; } = parent;
}
