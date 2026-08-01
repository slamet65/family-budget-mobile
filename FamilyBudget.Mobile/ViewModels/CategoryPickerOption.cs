namespace FamilyBudget.Mobile.ViewModels;

/// <summary>A category selectable in a transaction/budget form; <see cref="Label"/> is
/// parent-prefixed ("Jajan &gt; jajan izzan") for sub-categories.</summary>
public record CategoryPickerOption(int Id, string Label);
