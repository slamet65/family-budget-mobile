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

    Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default);

    Task<List<SavingDto>> GetSavingsAsync(CancellationToken ct = default);

    Task<SavingDetailDto> GetSavingAsync(int id, CancellationToken ct = default);

    Task<SavingDto> CreateSavingAsync(CreateSavingRequest request, CancellationToken ct = default);

    Task<SavingDetailDto> UpdateSavingAsync(int id, UpdateSavingRequest request, CancellationToken ct = default);

    Task<List<SavingTransactionDto>> GetSavingTransactionsAsync(int savingId, CancellationToken ct = default);

    Task<SavingTransactionDto> GetSavingTransactionAsync(int id, CancellationToken ct = default);

    Task<SavingTransactionDto> CreateSavingExpenseAsync(int savingId, CreateSavingExpenseRequest request, CancellationToken ct = default);

    Task<SavingTransactionDto> UpdateSavingExpenseAsync(int id, CreateSavingExpenseRequest request, CancellationToken ct = default);

    Task DeleteSavingExpenseAsync(int id, CancellationToken ct = default);

    Task<List<PeriodDto>> GetPeriodsAsync(CancellationToken ct = default);

    Task<PeriodDto> CreatePeriodAsync(CreatePeriodRequest request, CancellationToken ct = default);

    Task<PeriodDto?> GetCurrentPeriodAsync(CancellationToken ct = default);

    Task<TransactionDto> CreateIncomeAsync(CreateIncomeRequest request, CancellationToken ct = default);

    Task<TransactionDto> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken ct = default);

    Task<TransactionDto> CreateTransferAsync(CreateTransferRequest request, CancellationToken ct = default);

    Task<List<TransactionDto>> GetTransactionsAsync(TransactionListQuery query, CancellationToken ct = default);

    Task<TransactionDto> GetTransactionAsync(int id, CancellationToken ct = default);

    Task<TransactionDto> UpdateIncomeAsync(int id, CreateIncomeRequest request, CancellationToken ct = default);

    Task<TransactionDto> UpdateExpenseAsync(int id, CreateExpenseRequest request, CancellationToken ct = default);

    Task<TransactionDto> UpdateTransferAsync(int id, CreateTransferRequest request, CancellationToken ct = default);

    Task DeleteTransactionAsync(int id, CancellationToken ct = default);

    Task<List<BudgetDto>> GetBudgetsAsync(int periodId, CancellationToken ct = default);

    Task<BudgetUpsertResponseDto> UpsertBudgetAsync(int periodId, int categoryId, UpsertBudgetRequest request, CancellationToken ct = default);

    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);

    Task<List<UserDto>> GetUsersAsync(CancellationToken ct = default);

    Task<UserDto> ResetPasswordAsync(int userId, ResetPasswordRequest request, CancellationToken ct = default);

    Task<ClosePeriodResponse> ClosePeriodAsync(int periodId, ClosePeriodRequest request, CancellationToken ct = default);
}
