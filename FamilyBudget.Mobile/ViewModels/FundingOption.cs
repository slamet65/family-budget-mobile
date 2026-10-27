namespace FamilyBudget.Mobile.ViewModels;

// Picker entry for "what is this saving withdrawal/deposit for". CategoryId null means
// a plain loan (withdrawal) or plain deposit/repayment, neither of which touches a budget.
public record FundingOption(int? CategoryId, string? CategoryName, string Label);
