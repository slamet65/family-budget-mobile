namespace FamilyBudget.Mobile.Services.Api.Dtos;

public record WalletDto(int Id, string Name, DateTimeOffset CreatedAt, long Balance);

public record CreateWalletRequest(string Name);

// No Balance -- a brand-new wallet has no transactions yet, and the API's POST response
// is just the raw inserted row, not the joined/computed shape GET /wallets returns.
public record CreatedWalletDto(int Id, string Name, DateTimeOffset CreatedAt);
