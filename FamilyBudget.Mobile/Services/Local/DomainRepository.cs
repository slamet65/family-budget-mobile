using FamilyBudget.Mobile.Services.Api;
using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Auth;

namespace FamilyBudget.Mobile.Services.Local;

public sealed class DomainRepository(ILocalDatabase database, IApiClient api, IAuthService auth) : IDomainRepository
{
    private int UserId => auth.CurrentUser?.Id ?? throw new InvalidOperationException("Sign-in required.");

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var userId = UserId;
        var savings = await api.GetSavingsAsync(ct);
        var savingTransactions = new List<SavingTransactionDto>();
        foreach (var saving in savings) savingTransactions.AddRange(await api.GetSavingTransactionsAsync(saving.Id, ct));
        var periods = await api.GetPeriodsAsync(ct);
        var budgets = new List<BudgetDto>();
        foreach (var period in periods) budgets.AddRange(await api.GetBudgetsAsync(period.Id, ct));
        var users = await api.GetUsersAsync(ct);
        if (auth.CurrentUser?.Id != userId) return;
        await database.ReplaceDomainSnapshotAsync(userId, new(savings, savingTransactions, budgets, users));
    }

    public async Task RefreshSavingsAsync(CancellationToken ct = default)
    {
        var userId = UserId;
        var savings = await api.GetSavingsAsync(ct);
        var transactions = new List<SavingTransactionDto>();
        foreach (var saving in savings) transactions.AddRange(await api.GetSavingTransactionsAsync(saving.Id, ct));
        if (auth.CurrentUser?.Id != userId) return;
        await database.ReplaceSavingsSnapshotAsync(userId, savings, transactions);
    }

    public async Task RefreshSavingChangesAsync(IReadOnlyCollection<SyncChangeDto> changes,
        CancellationToken ct = default)
    {
        var userId = UserId;
        var savingIds = changes.Where(change => change.EntityType == "savings")
            .Select(change => change.EntityId).ToHashSet();
        foreach (var change in changes.Where(change => change.EntityType == "saving_transactions"))
        {
            var cached = await database.GetSavingTransactionAsync(userId, change.EntityId);
            if (cached is not null)
            {
                savingIds.Add(cached.SavingId);
                if (cached.RelatedSavingId is { } relatedId) savingIds.Add(relatedId);
            }
            if (change.Operation != "delete")
            {
                try
                {
                    var current = await api.GetSavingTransactionAsync(change.EntityId, ct);
                    savingIds.Add(current.SavingId);
                    if (current.RelatedSavingId is { } relatedId) savingIds.Add(relatedId);
                }
                catch (ApiException exception) when (exception.StatusCode == 404) { }
            }
        }

        var savings = new List<SavingDto>();
        var transactions = new List<SavingTransactionDto>();
        foreach (var savingId in savingIds)
        {
            try
            {
                var detail = await api.GetSavingAsync(savingId, ct);
                savings.Add(new SavingDto(detail.Id, detail.Name, detail.Note, detail.Balance, detail.CreatedAt));
                transactions.AddRange(await api.GetSavingTransactionsAsync(savingId, ct));
            }
            catch (ApiException exception) when (exception.StatusCode == 404) { }
        }
        if (auth.CurrentUser?.Id != userId) return;
        await database.ReplaceSavingsAsync(userId, savingIds, savings, transactions);
    }

    public async Task RefreshBudgetsAsync(IReadOnlyCollection<int> periodIds, CancellationToken ct = default)
    {
        var uniquePeriodIds = periodIds.Distinct().ToList();
        if (uniquePeriodIds.Count == 0) return;
        var userId = UserId;
        var budgets = new List<BudgetDto>();
        foreach (var periodId in uniquePeriodIds) budgets.AddRange(await api.GetBudgetsAsync(periodId, ct));
        if (auth.CurrentUser?.Id != userId) return;
        await database.ReplaceBudgetsAsync(userId, uniquePeriodIds, budgets);
    }

    public async Task RefreshUsersAsync(CancellationToken ct = default)
    {
        var userId = UserId;
        var users = await api.GetUsersAsync(ct);
        if (auth.CurrentUser?.Id != userId) return;
        await database.ReplaceUsersAsync(userId, users);
    }

    public Task<IReadOnlyList<SavingDto>> GetSavingsAsync() => database.GetSavingsAsync(UserId);
    public Task<IReadOnlyList<SavingTransactionDto>> GetSavingTransactionsAsync(int id) => database.GetSavingTransactionsAsync(UserId, id);
    public Task<SavingTransactionDto?> GetSavingTransactionAsync(int id) => database.GetSavingTransactionAsync(UserId, id);
    public Task<IReadOnlyList<BudgetDto>> GetBudgetsAsync(int id) => database.GetBudgetsAsync(UserId, id);
    public Task<IReadOnlyList<UserDto>> GetUsersAsync() => database.GetUsersAsync(UserId);

    public async Task<SavingDetailDto?> GetSavingAsync(int savingId)
    {
        var saving = (await GetSavingsAsync()).FirstOrDefault(item => item.Id == savingId);
        if (saving is null) return null;
        var opening = (await GetSavingTransactionsAsync(savingId)).FirstOrDefault(item => item.Type == "opening_balance");
        return new(saving.Id, saving.Name, saving.Note, saving.Balance, opening?.Amount ?? 0,
            opening?.OccurredAt, saving.CreatedAt);
    }
}
