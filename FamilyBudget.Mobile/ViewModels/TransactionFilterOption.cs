namespace FamilyBudget.Mobile.ViewModels;

/// <summary>A selectable filter chip value; <see cref="Value"/> is null for the "All" option.</summary>
public record TransactionFilterOption(int? Value, string Label);

public record TransactionTypeOption(string? Value, string Label);
