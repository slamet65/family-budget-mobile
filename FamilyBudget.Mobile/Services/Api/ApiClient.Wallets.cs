using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Api;

public partial class ApiClient
{
    public Task<List<WalletDto>> GetWalletsAsync(CancellationToken ct = default) =>
        SendAsync<List<WalletDto>>(HttpMethod.Get, "/wallets", null, ct);

    public Task<CreatedWalletDto> CreateWalletAsync(CreateWalletRequest request, CancellationToken ct = default) =>
        SendAsync<CreatedWalletDto>(HttpMethod.Post, "/wallets", request, ct);
}
