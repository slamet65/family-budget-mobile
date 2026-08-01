using FamilyBudget.Mobile.Services.Api.Dtos;

namespace FamilyBudget.Mobile.Services.Api;

public partial interface IApiClient
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task LogoutAsync(CancellationToken ct = default);

    Task<List<WalletDto>> GetWalletsAsync(CancellationToken ct = default);

    Task<CreatedWalletDto> CreateWalletAsync(CreateWalletRequest request, CancellationToken ct = default);

    Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);

    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default);

    Task<List<PeriodDto>> GetPeriodsAsync(CancellationToken ct = default);

    Task<PeriodDto> CreatePeriodAsync(CreatePeriodRequest request, CancellationToken ct = default);

    Task<PeriodDto?> GetCurrentPeriodAsync(CancellationToken ct = default);

    Task<TransactionDto> CreateIncomeAsync(CreateIncomeRequest request, CancellationToken ct = default);

    Task<TransactionDto> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken ct = default);

    Task<TransactionDto> CreateTransferAsync(CreateTransferRequest request, CancellationToken ct = default);

    Task<List<TransactionDto>> GetTransactionsAsync(TransactionListQuery query, CancellationToken ct = default);

    Task<List<BudgetDto>> GetBudgetsAsync(int periodId, CancellationToken ct = default);

    Task<BudgetUpsertResponseDto> UpsertBudgetAsync(int periodId, int categoryId, UpsertBudgetRequest request, CancellationToken ct = default);

    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);

    Task<List<UserDto>> GetUsersAsync(CancellationToken ct = default);

    Task<UserDto> ResetPasswordAsync(int userId, ResetPasswordRequest request, CancellationToken ct = default);

    Task<ClosePeriodResponse> ClosePeriodAsync(int periodId, ClosePeriodRequest request, CancellationToken ct = default);
}
