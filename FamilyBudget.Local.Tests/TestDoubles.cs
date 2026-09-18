using System.Reflection;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Auth;
using FamilyBudget.Mobile.Services.Sync;
using FamilyBudget.Mobile.Services.Local;

public class ApiProxy : DispatchProxy
{
    public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = (_, _) => throw new NotImplementedException();
    protected override object? Invoke(MethodInfo? method, object?[]? args) => Handler(method!, args);
}

public sealed class TestNetwork : INetworkMonitor
{
    public bool IsInternetAvailable { get; set; }
    public event EventHandler? InternetAvailable;
    public void GoOnline()
    {
        IsInternetAvailable = true;
        InternetAvailable?.Invoke(this, EventArgs.Empty);
    }
}

public sealed class FakeReferences : IReferenceDataRepository
{
    public int RefreshCount { get; private set; }
    public Task<IReadOnlyList<CategoryDto>> GetCachedCategoriesAsync() => Task.FromResult<IReadOnlyList<CategoryDto>>([]);
    public Task<IReadOnlyList<PeriodDto>> GetCachedPeriodsAsync() => Task.FromResult<IReadOnlyList<PeriodDto>>([]);
    public Task<IReadOnlyList<CategoryDto>> RefreshCategoriesAsync(CancellationToken ct = default)
    { RefreshCount++; return Task.FromResult<IReadOnlyList<CategoryDto>>([]); }
    public Task<IReadOnlyList<PeriodDto>> RefreshPeriodsAsync(CancellationToken ct = default)
    { RefreshCount++; return Task.FromResult<IReadOnlyList<PeriodDto>>([]); }
}

public sealed class FakeLedger : ILedgerRepository
{
    public int RefreshCount { get; private set; }
    public Task RefreshAsync(CancellationToken ct = default) { RefreshCount++; return Task.CompletedTask; }
    public Task RefreshTransactionChangesAsync(IReadOnlyCollection<SyncChangeDto> changes, CancellationToken ct = default)
    { RefreshCount++; return Task.CompletedTask; }
    public Task<IReadOnlyList<WalletDto>> GetWalletsAsync() => Task.FromResult<IReadOnlyList<WalletDto>>([]);
    public Task<IReadOnlyList<PeriodDto>> GetPeriodsAsync() => Task.FromResult<IReadOnlyList<PeriodDto>>([]);
    public Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(TransactionListQuery query) => Task.FromResult<IReadOnlyList<TransactionDto>>([]);
    public Task<TransactionDto?> GetTransactionAsync(int id) => Task.FromResult<TransactionDto?>(null);
    public Task<DateTimeOffset?> GetRefreshedAtAsync() => Task.FromResult<DateTimeOffset?>(null);
}

public sealed class FakeDomain : IDomainRepository
{
    public bool Fail { get; set; }
    public int RefreshCount { get; private set; }
    public Task RefreshAsync(CancellationToken ct = default)
    { RefreshCount++; return Fail ? Task.FromException(new Exception("refresh failed")) : Task.CompletedTask; }
    public List<int> RefreshedBudgetPeriods { get; } = [];
    public Task RefreshSavingsAsync(CancellationToken ct = default) { RefreshCount++; return Task.CompletedTask; }
    public Task RefreshSavingChangesAsync(IReadOnlyCollection<SyncChangeDto> changes, CancellationToken ct = default)
    { RefreshCount++; return Task.CompletedTask; }
    public Task RefreshBudgetsAsync(IReadOnlyCollection<int> periodIds, CancellationToken ct = default)
    { RefreshedBudgetPeriods.AddRange(periodIds); return Task.CompletedTask; }
    public Task RefreshUsersAsync(CancellationToken ct = default) { RefreshCount++; return Task.CompletedTask; }
    public Task<IReadOnlyList<SavingDto>> GetSavingsAsync() => Task.FromResult<IReadOnlyList<SavingDto>>([]);
    public Task<SavingDetailDto?> GetSavingAsync(int id) => Task.FromResult<SavingDetailDto?>(null);
    public Task<IReadOnlyList<SavingTransactionDto>> GetSavingTransactionsAsync(int id) => Task.FromResult<IReadOnlyList<SavingTransactionDto>>([]);
    public Task<SavingTransactionDto?> GetSavingTransactionAsync(int id) => Task.FromResult<SavingTransactionDto?>(null);
    public Task<IReadOnlyList<BudgetDto>> GetBudgetsAsync(int id) => Task.FromResult<IReadOnlyList<BudgetDto>>([]);
    public Task<IReadOnlyList<UserDto>> GetUsersAsync() => Task.FromResult<IReadOnlyList<UserDto>>([]);
}

public sealed class FakeOutbox : IOutboxSyncService
{
    public Task<TransactionDto> EnqueueAsync(PendingTransactionInput input) => throw new NotImplementedException();
    public Task<TransactionDto> EnqueueUpdateAsync(TransactionDto original, PendingTransactionInput input) => throw new NotImplementedException();
    public Task EnqueueDeleteAsync(TransactionDto original) => throw new NotImplementedException();
    public Task<SavingTransactionDto> EnqueueSavingAsync(PendingSavingTransactionInput input) => throw new NotImplementedException();
    public Task<SavingTransactionDto> EnqueueSavingUpdateAsync(SavingTransactionDto original, PendingSavingTransactionInput input) => throw new NotImplementedException();
    public Task EnqueueSavingDeleteAsync(SavingTransactionDto original) => throw new NotImplementedException();
    public Task ProcessPendingAsync(CancellationToken ct = default) => Task.CompletedTask;
    public void TriggerSync() { }
    public Task RetryAsync(string id) => Task.CompletedTask;
    public Task CancelAsync(string id) => Task.CompletedTask;
    public Task<bool> HasUnresolvedAsync() => Task.FromResult(false);
}

public sealed class TestAuth : IAuthService
{
    public UserDto? CurrentUser { get; set; } = new(1, "Test", "test@example.com");
    public Task<string?> GetTokenAsync() => Task.FromResult<string?>("token");
    public Task SaveSessionAsync(string token, UserDto user)
    {
        CurrentUser = user;
        return Task.CompletedTask;
    }
    public Task ClearSessionAsync()
    {
        CurrentUser = null;
        return Task.CompletedTask;
    }
}
