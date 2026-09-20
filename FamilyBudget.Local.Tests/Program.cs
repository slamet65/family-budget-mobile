using FamilyBudget.Mobile.Services.Api.Dtos;
using FamilyBudget.Mobile.Services.Local;
using FamilyBudget.Mobile.Services.Api;
using System.Reflection;
using FamilyBudget.Mobile.Services.Sync;
using FamilyBudget.Mobile.Common;
using System.Text.Json;

// Dependency-light executable regression suite against a real SQLite database.
// A failing assertion exits non-zero; source files are linked from the mobile app.
var path = Path.Combine(Path.GetTempPath(), $"family-budget-test-{Guid.NewGuid():N}.db3");
var database = new LocalDatabase(path);
var date = DateTimeOffset.Parse("2026-09-15T00:00:00Z");
var period = new PeriodDto(1, "September", date, "open", null, date);
var wallet = new WalletDto(1, "Cash", date, 900);
TransactionDto Entry(int id, string type, int? from, int? to, int? periodId, int day = 0) =>
    new(id, periodId, type, from, from is null ? null : "Cash", to, to is null ? null : "Bank",
        type == "expense" ? 1 : null, type == "expense" ? "Food" : null, 100, null, 99,
        date.AddDays(day), date.AddSeconds(id));

void Check(bool condition, string name)
{
    if (!condition) throw new Exception($"FAIL: {name}");
    Console.WriteLine($"PASS: {name}");
}

var aggregateBudgetJson = """
    {"id":1,"periodId":1,"periodName":"September","categoryId":1,"categoryName":"Food","savingId":null,"savingName":null,"plannedAmount":500000,"actualAmount":200000.0,"remainingAmount":"300000","createdAt":"2026-09-15T00:00:00Z"}
    """;
var aggregateBudget = JsonSerializer.Deserialize<BudgetDto>(aggregateBudgetJson, JsonOptions.Default);
Check(aggregateBudget is { ActualAmount: 200_000, RemainingAmount: 300_000 },
    "D1 aggregate decimal and numeric-string amounts deserialize without precision loss");
try
{
    JsonSerializer.Deserialize<long>("1.5", JsonOptions.Default);
    throw new Exception("Expected fractional money to be rejected");
}
catch (JsonException) { }
Check(true, "fractional money remains invalid");

Check(await database.GetLedgerRefreshedAtAsync(1) is null, "new database has no snapshot marker");
await database.ReplaceCategoriesAsync(1, [new CategoryDto(1, "Food", null, false, null, null, date)]);
var rows = new[]
{
    Entry(1, "income", null, 1, 1),
    Entry(2, "expense", 1, null, 1),
    Entry(3, "transfer", 1, 2, 1, 1),
    Entry(4, "saving_withdrawal", null, 2, null, 2),
};
await database.ReplaceLedgerSnapshotAsync(1, [wallet], [period], rows);
Check((await database.GetCategoriesAsync(1)).Count == 1, "ledger refresh preserves reference cache");
Check((await database.GetWalletsAsync(1)).Single() == wallet, "wallet fields and balance round-trip");
Check((await database.GetPeriodsAsync(1)).Single() == period, "period UTC dates round-trip");
Check(await database.GetLedgerRefreshedAtAsync(1) is not null, "successful snapshot records refresh time");
var all = await database.GetTransactionsAsync(1, new(null, null, null));
Check(all.Select(row => row.Id).SequenceEqual([4, 3, 2, 1]), "date/created-at ordering matches server");
Check(all.Single(row => row.Id == 3) == rows[2], "transaction payload and author round-trip");
Check((await database.GetTransactionsAsync(1, new(1, 2, "transfer"))).Single().Id == 3,
    "combined period/wallet/type filters include transfer destination");
Check((await database.GetTransactionsAsync(1, new(null, 1, null))).Count == 3,
    "wallet filter includes both incoming and outgoing transactions");
Check((await database.GetTransactionsAsync(1, new(null, null, "' OR 1=1 --"))).Count == 0,
    "filter parameters are not interpreted as SQL");
Check((await database.GetTransactionsAsync(2, new(null, null, null))).Count == 0,
    "account B cannot read account A snapshot");
await database.ReplaceLedgerSnapshotAsync(2, [wallet with { Name = "Other account" }], [], [rows[0]]);
Check((await database.GetTransactionsAsync(1, new(null, null, null))).Count == 4,
    "same server IDs in another scope do not replace account A");
var marker = await database.GetLedgerRefreshedAtAsync(1);
try
{
    await database.ReplaceLedgerSnapshotAsync(1, [wallet with { Balance = 0 }], [], [rows[0], rows[0]]);
    throw new Exception("Expected duplicate primary key failure");
}
catch (SQLite.SQLiteException) { }
Check((await database.GetWalletsAsync(1)).Single().Balance == 900
    && (await database.GetTransactionsAsync(1, new(null, null, null))).Count == 4
    && await database.GetLedgerRefreshedAtAsync(1) == marker,
    "snapshot failure rolls back deletes, wallets, transactions and marker");
var reopened = new LocalDatabase(path);
Check((await reopened.GetTransactionsAsync(1, new(null, null, null))).Count == 4,
    "snapshot survives a new database instance");
var scalePath = Path.Combine(Path.GetTempPath(), $"family-budget-scale-test-{Guid.NewGuid():N}.db3");
var scaleDatabase = new LocalDatabase(scalePath);
var scaleRows = Enumerable.Range(1, 5_000)
    .Select(id => Entry(id, id % 2 == 0 ? "expense" : "income", id % 2 == 0 ? 1 : null,
        id % 2 == 0 ? null : 1, 1, id % 28))
    .ToList();
var scaleTimer = System.Diagnostics.Stopwatch.StartNew();
await scaleDatabase.ReplaceLedgerSnapshotAsync(1, [wallet], [period], scaleRows);
var scaleExpenses = await scaleDatabase.GetTransactionsAsync(1, new(1, 1, "expense"));
scaleTimer.Stop();
Check(scaleExpenses.Count == 2_500 && scaleTimer.Elapsed < TimeSpan.FromSeconds(10),
    $"indexed local ledger remains responsive with 5,000 transactions ({scaleTimer.ElapsedMilliseconds} ms)");
await database.ReplaceLedgerSnapshotAsync(1, [], [], []);
Check((await database.GetTransactionsAsync(1, new(null, null, null))).Count == 0
    && await database.GetLedgerRefreshedAtAsync(1) is not null,
    "empty authoritative snapshot removes rows but remains distinguishable from never-loaded cache");
await database.ReplaceLedgerSnapshotAsync(1, [wallet], [period], rows);
var auth = new TestAuth();
var api = DispatchProxy.Create<IApiClient, ApiProxy>();
var proxy = (ApiProxy)(object)api;
var failFetch = true;
var switchAccount = false;
var fullQueryRequested = false;
proxy.Handler = (method, args) => method.Name switch
{
    nameof(IApiClient.GetWalletsAsync) => Task.FromResult(new List<WalletDto> { wallet with { Balance = 1234 } }),
    nameof(IApiClient.GetPeriodsAsync) => Task.FromResult(new List<PeriodDto> { period }),
    nameof(IApiClient.GetTransactionsAsync) => FetchTransactions(args),
    _ => throw new NotImplementedException(method.Name),
};
Task<List<TransactionDto>> FetchTransactions(object?[]? args)
{
    fullQueryRequested = args?[0] is TransactionListQuery { PeriodId: null, WalletId: null, Type: null };
    if (switchAccount) auth.CurrentUser = new(2, "Other", "other@example.com");
    return failFetch ? Task.FromException<List<TransactionDto>>(new HttpRequestException("Offline"))
        : Task.FromResult(rows.ToList());
}
var repository = new LedgerRepository(database, api, auth);
try { await repository.RefreshAsync(); }
catch (HttpRequestException) { }
Check((await repository.GetWalletsAsync()).Single().Balance == 900,
    "partial network refresh never overwrites the last complete snapshot");
Check(fullQueryRequested, "repository fetches the unfiltered full ledger before replacement");
failFetch = false;
await repository.RefreshAsync();
Check((await repository.GetWalletsAsync()).Single().Balance == 1234,
    "successful repository refresh commits all fetched data");
switchAccount = true;
await repository.RefreshAsync();
Check((await database.GetWalletsAsync(2)).Single().Name == "Other account",
    "account switch during network fetch prevents writing the response into another account cache");
auth.CurrentUser = new(1, "Test", "test@example.com");
var targetedPath = Path.Combine(Path.GetTempPath(), $"family-budget-targeted-sync-{Guid.NewGuid():N}.db3");
var targetedDatabase = new LocalDatabase(targetedPath);
await targetedDatabase.ReplaceLedgerSnapshotAsync(1, [wallet], [period], rows);
var targetedApi = DispatchProxy.Create<IApiClient, ApiProxy>();
var targetedFullListCalled = false;
((ApiProxy)(object)targetedApi).Handler = (method, args) => method.Name switch
{
    nameof(IApiClient.GetWalletsAsync) => Task.FromResult(new List<WalletDto> { wallet with { Balance = 777 } }),
    nameof(IApiClient.GetTransactionAsync) => Task.FromResult(rows[1] with { Amount = 222 }),
    nameof(IApiClient.GetTransactionsAsync) => ThrowFullTransactionList(),
    _ => throw new NotImplementedException(method.Name),
};
Task<List<TransactionDto>> ThrowFullTransactionList()
{
    targetedFullListCalled = true;
    throw new Exception("Full transaction list should not be requested");
}
var targetedRepository = new LedgerRepository(targetedDatabase, targetedApi, auth);
await targetedRepository.RefreshTransactionChangesAsync([
    new(1, "transactions", 1, "delete", date),
    new(2, "transactions", 2, "upsert", date),
]);
Check(!targetedFullListCalled
    && !(await targetedDatabase.GetTransactionsAsync(1, new(null, null, null))).Any(item => item.Id == 1)
    && (await targetedDatabase.GetTransactionAsync(1, 2))?.Amount == 222,
    "incremental transaction sync fetches only changed IDs and applies tombstones locally");
var pending = await database.EnqueueTransactionAsync(1, new PendingTransactionInput(
    1, "expense", 1, "Cash", null, null, 1, "Food", 200, "Offline", date));
Check(pending.LocalId == pending.ClientMutationId && pending.SyncStatus == "pending",
    "enqueue uses one stable UUID for local identity and server idempotency");
Check((await database.GetPendingOutboxAsync(1)).Single().OperationId == pending.LocalId,
    "enqueue atomically creates an outbox operation");
Check((await database.GetTransactionsAsync(1, new(1, 1, "expense"))).Any(row => row.LocalId == pending.LocalId),
    "pending transaction is immediately queryable with normal filters");
Check((await database.GetWalletsAsync(1)).Single().Balance == 1034,
    "pending expense overlays the last authoritative wallet balance");
var afterRestart = new LocalDatabase(path);
Check((await afterRestart.GetPendingOutboxAsync(1)).Single().OperationId == pending.LocalId,
    "pending outbox survives a new database instance");
await database.MarkOutboxSyncingAsync(pending.LocalId!);
var afterInterruptedSync = new LocalDatabase(path);
Check((await afterInterruptedSync.GetPendingOutboxAsync(1)).Single().Status == "retry"
    && (await afterInterruptedSync.GetTransactionsAsync(1, new(null, null, null)))
        .Single(row => row.LocalId == pending.LocalId).SyncStatus == "retry",
    "process restart recovers an interrupted syncing item as retryable");
await database.FailOutboxAsync(pending.LocalId!, "No connection", permanent: false);
Check((await database.GetTransactionsAsync(1, new(null, null, null)))
        .Single(row => row.LocalId == pending.LocalId).SyncStatus == "retry",
    "transient failure keeps the transaction and marks it for retry");
await database.RetryOutboxAsync(1, pending.LocalId!);
var serverRow = rows[1] with { Id = 55, ClientMutationId = pending.ClientMutationId };
await database.CompleteOutboxAsync(pending.LocalId!, serverRow);
Check((await database.GetPendingOutboxAsync(1)).Count == 0
    && (await database.GetTransactionsAsync(1, new(null, null, null)))
        .Single(row => row.LocalId == pending.LocalId).SyncStatus == "synced",
    "successful sync removes outbox and preserves a synced display row");
Check((await database.GetWalletsAsync(1)).Single().Balance == 1234,
    "synced pending overlay is removed until authoritative refresh arrives");
await database.ApplyTransactionChangesAsync(1, [wallet with { Balance = 1034 }], [serverRow], []);
Check((await database.GetTransactionsAsync(1, new(null, null, null)))
        .Count(row => row.Id == serverRow.Id) == 1,
    "incremental refresh replaces a completed offline create instead of duplicating it");
await database.ReplaceLedgerSnapshotAsync(1, [wallet with { Balance = 1034 }], [period], [serverRow]);
Check((await database.GetTransactionsAsync(1, new(null, null, null))).Single().Id == 55,
    "authoritative refresh replaces the temporary synced row");
var cancelled = await database.EnqueueTransactionAsync(1, new PendingTransactionInput(
    1, "income", null, null, 1, "Cash", null, null, 50, null, date));
await database.CancelOutboxAsync(1, cancelled.LocalId!);
Check((await database.GetPendingOutboxAsync(1)).Count == 0
    && !(await database.GetTransactionsAsync(1, new(null, null, null))).Any(row => row.LocalId == cancelled.LocalId),
    "cancel removes both unsynced display row and outbox atomically");
var rejected = await database.EnqueueTransactionAsync(1, new PendingTransactionInput(
    1, "income", null, null, 1, "Cash", null, null, 50, null, date));
await database.FailOutboxAsync(rejected.LocalId!, "Period closed", permanent: true);
Check((await database.GetPendingOutboxAsync(1)).Count == 0
    && (await database.GetTransactionsAsync(1, new(null, null, null)))
        .Single(row => row.LocalId == rejected.LocalId).SyncStatus == "failed",
    "permanent rejection stays visible but is excluded from automatic retries and balance overlay");
await database.CancelOutboxAsync(1, rejected.LocalId!);
var syncNetwork = new TestNetwork { IsInternetAvailable = false };
CreateExpenseRequest? capturedRequest = null;
var syncApi = DispatchProxy.Create<IApiClient, ApiProxy>();
var syncProxy = (ApiProxy)(object)syncApi;
var syncFailure = "none";
syncProxy.Handler = (method, args) =>
{
    if (method.Name != nameof(IApiClient.CreateExpenseAsync)) throw new NotImplementedException(method.Name);
    capturedRequest = (CreateExpenseRequest)args![0]!;
    return syncFailure switch
    {
        "transient" => Task.FromException<TransactionDto>(new ApiException(null, "No connection")),
        "permanent" => Task.FromException<TransactionDto>(new ApiException(400, "Period closed")),
        _ => Task.FromResult(rows[1] with { Id = 70, ClientMutationId = capturedRequest.ClientMutationId }),
    };
};
var sync = new OutboxSyncService(database, syncApi, auth, syncNetwork);
var queuedByService = await sync.EnqueueAsync(new PendingTransactionInput(
    1, "expense", 1, "Cash", null, null, 1, "Food", 75, null, date));
Check((await database.GetPendingOutboxAsync(1)).Count == 1,
    "offline sync service leaves newly enqueued mutation durable");
syncNetwork.IsInternetAvailable = true;
await sync.ProcessPendingAsync();
Check(capturedRequest?.ClientMutationId == queuedByService.LocalId
    && (await database.GetPendingOutboxAsync(1)).Count == 0,
    "processor sends stable clientMutationId and completes a successful item");
syncNetwork.IsInternetAvailable = false;
var retryByService = await sync.EnqueueAsync(new PendingTransactionInput(
    1, "expense", 1, "Cash", null, null, 1, "Food", 80, null, date));
syncFailure = "transient";
syncNetwork.IsInternetAvailable = true;
await sync.ProcessPendingAsync();
Check((await database.GetPendingOutboxAsync(1)).Single().OperationId == retryByService.LocalId,
    "network/API failure remains eligible for automatic retry");
using (var raw = new SQLite.SQLiteConnection(path))
{
    raw.Execute("UPDATE outbox SET NextAttemptAtUnixMilliseconds=? WHERE OperationId=?", long.MaxValue, retryByService.LocalId);
}
syncFailure = "none";
await sync.ProcessPendingAsync();
Check((await database.GetPendingOutboxAsync(1)).Count == 0,
    "retry correctness does not depend on the device wall clock");
syncNetwork.IsInternetAvailable = false;
var permanentByService = await sync.EnqueueAsync(new PendingTransactionInput(
    1, "expense", 1, "Cash", null, null, 1, "Food", 85, null, date));
syncFailure = "permanent";
syncNetwork.IsInternetAvailable = true;
await sync.ProcessPendingAsync();
Check((await database.GetPendingOutboxAsync(1)).Count == 0
    && (await database.GetTransactionsAsync(1, new(null, null, null)))
        .Single(row => row.LocalId == permanentByService.LocalId).SyncStatus == "failed",
    "business validation failure is visible but not retried automatically");
Check(await sync.HasUnresolvedAsync(), "permanently failed mutation blocks destructive period close workflow");
await sync.CancelAsync(permanentByService.LocalId!);
Check(!await sync.HasUnresolvedAsync(), "cancelled mutation no longer blocks period close workflow");

var mutationPath = Path.Combine(Path.GetTempPath(), $"family-budget-mutation-test-{Guid.NewGuid():N}.db3");
var mutationDatabase = new LocalDatabase(mutationPath);
var originalExpense = Entry(101, "expense", 1, null, 1) with { Version = 3 };
await mutationDatabase.ReplaceLedgerSnapshotAsync(1, [wallet], [period], [originalExpense]);
var editInput = new PendingTransactionInput(1, "expense", 1, "Cash", null, null, 1, "Food",
    250, "edited offline", date.AddDays(1));
var optimisticEdit = await mutationDatabase.EnqueueTransactionUpdateAsync(1, originalExpense, editInput);
var updateOperation = (await mutationDatabase.GetPendingOutboxAsync(1)).Single();
Check(optimisticEdit.SyncStatus == "pending" && optimisticEdit.Amount == 250
    && updateOperation.OperationType == "update" && updateOperation.ServerId == 101
    && updateOperation.ExpectedVersion == 3,
    "offline edit is optimistic and records server id plus expected version");
Check((await mutationDatabase.GetWalletsAsync(1)).Single().Balance == 750,
    "offline edit reverses the old wallet effect before applying the new amount");
await mutationDatabase.ReplaceLedgerSnapshotAsync(1, [wallet], [period], [originalExpense]);
Check((await mutationDatabase.GetTransactionAsync(1, 101)) is { Amount: 250, SyncStatus: "pending" },
    "incoming snapshot does not overwrite or collide with an optimistic edit");
await mutationDatabase.CancelOutboxAsync(1, optimisticEdit.LocalId!);
Check((await mutationDatabase.GetTransactionAsync(1, 101)) is { Amount: 100, Version: 3, SyncStatus: "synced" }
    && (await mutationDatabase.GetWalletsAsync(1)).Single().Balance == 900,
    "cancelling an offline edit restores the original payload and balance");

optimisticEdit = await mutationDatabase.EnqueueTransactionUpdateAsync(1, originalExpense, editInput);
updateOperation = (await mutationDatabase.GetPendingOutboxAsync(1)).Single();
await mutationDatabase.MarkOutboxSyncingAsync(updateOperation.OperationId);
var updatedServerExpense = originalExpense with { Amount = 250, Note = "edited offline", Version = 4 };
await mutationDatabase.CompleteOutboxAsync(updateOperation.OperationId, updatedServerExpense);
Check((await mutationDatabase.GetTransactionAsync(1, 101)) is { Amount: 250, Version: 4, SyncStatus: "synced" }
    && (await mutationDatabase.GetPendingOutboxAsync(1)).Count == 0,
    "successful offline edit stores the authoritative server version and clears its outbox item");

var conflictEdit = await mutationDatabase.EnqueueTransactionUpdateAsync(1, updatedServerExpense,
    editInput with { Amount = 300, Note = "keep this input" });
await mutationDatabase.FailOutboxAsync(conflictEdit.LocalId!,
    "Transaksi ini sudah berubah di perangkat lain.", permanent: true);
Check((await mutationDatabase.GetTransactionAsync(1, 101)) is { Amount: 300, Note: "keep this input", SyncStatus: "failed" },
    "version conflict keeps the user's optimistic edit visible for recovery");
await mutationDatabase.CancelOutboxAsync(1, conflictEdit.LocalId!);

var deleteSource = (await mutationDatabase.GetTransactionAsync(1, 101))!;
await mutationDatabase.EnqueueTransactionDeleteAsync(1, deleteSource);
var deleteOperation = (await mutationDatabase.GetPendingOutboxAsync(1)).Single();
Check(await mutationDatabase.GetTransactionAsync(1, 101) is null
    && (await mutationDatabase.GetWalletsAsync(1)).Single().Balance == 1150
    && deleteOperation.OperationType == "delete" && deleteOperation.ExpectedVersion == 4,
    "offline delete hides the transaction and reverses its wallet effect");
await mutationDatabase.FailOutboxAsync(deleteOperation.OperationId, "conflict", permanent: true);
Check((await mutationDatabase.GetTransactionAsync(1, 101)) is { SyncStatus: "failed" },
    "rejected offline delete restores the transaction instead of losing it");
await mutationDatabase.CancelOutboxAsync(1, deleteOperation.OperationId);
deleteSource = (await mutationDatabase.GetTransactionAsync(1, 101))!;
await mutationDatabase.EnqueueTransactionDeleteAsync(1, deleteSource);
deleteOperation = (await mutationDatabase.GetPendingOutboxAsync(1)).Single();
await mutationDatabase.CompleteOutboxAsync(deleteOperation.OperationId, null);
Check(await mutationDatabase.GetTransactionAsync(1, 101) is null
    && (await mutationDatabase.GetPendingOutboxAsync(1)).Count == 0,
    "successful offline delete removes the local tombstone and outbox atomically");

var processorMutationPath = Path.Combine(Path.GetTempPath(), $"family-budget-mutation-processor-{Guid.NewGuid():N}.db3");
var processorMutationDatabase = new LocalDatabase(processorMutationPath);
await processorMutationDatabase.ReplaceLedgerSnapshotAsync(1, [wallet], [period], [originalExpense]);
var processorNetwork = new TestNetwork { IsInternetAvailable = false };
CreateExpenseRequest? capturedUpdate = null;
DeleteTransactionRequest? capturedDelete = null;
var mutationApi = DispatchProxy.Create<IApiClient, ApiProxy>();
((ApiProxy)(object)mutationApi).Handler = (method, args) => method.Name switch
{
    nameof(IApiClient.UpdateExpenseAsync) => CaptureUpdate(args),
    nameof(IApiClient.DeleteTransactionAsync) when args?[1] is DeleteTransactionRequest => CaptureDelete(args),
    _ => throw new NotImplementedException(method.Name),
};
Task<TransactionDto> CaptureUpdate(object?[]? args)
{
    capturedUpdate = (CreateExpenseRequest)args![1]!;
    return Task.FromResult(originalExpense with
    {
        Amount = capturedUpdate.Amount, Note = capturedUpdate.Note, Version = 4,
    });
}
Task CaptureDelete(object?[]? args)
{
    capturedDelete = (DeleteTransactionRequest)args![1]!;
    return Task.CompletedTask;
}
var mutationProcessor = new OutboxSyncService(processorMutationDatabase, mutationApi, auth, processorNetwork);
await mutationProcessor.EnqueueUpdateAsync(originalExpense, editInput);
processorNetwork.IsInternetAvailable = true;
await mutationProcessor.ProcessPendingAsync();
Check(capturedUpdate is { ExpectedVersion: 3 } && capturedUpdate.ClientMutationId is not null
    && (await processorMutationDatabase.GetTransactionAsync(1, 101))?.Version == 4,
    "outbox processor sends mutation id and expectedVersion for offline update");
processorNetwork.IsInternetAvailable = false;
var deleteThroughProcessor = (await processorMutationDatabase.GetTransactionAsync(1, 101))!;
await mutationProcessor.EnqueueDeleteAsync(deleteThroughProcessor);
processorNetwork.IsInternetAvailable = true;
await mutationProcessor.ProcessPendingAsync();
Check(capturedDelete is { ExpectedVersion: 4 } && capturedDelete.ClientMutationId is not null
    && await processorMutationDatabase.GetTransactionAsync(1, 101) is null,
    "outbox processor sends mutation id and expectedVersion for offline delete");

Check(!OutboxSyncService.IsPermanentFailure(null)
    && !OutboxSyncService.IsPermanentFailure(401)
    && !OutboxSyncService.IsPermanentFailure(429)
    && OutboxSyncService.IsPermanentFailure(400)
    && OutboxSyncService.IsPermanentFailure(409),
    "retry policy classifies transport/auth/throttle and business failures");
Check(LocalDatabase.OutboxRetryDelay(1) == TimeSpan.FromSeconds(2)
    && LocalDatabase.OutboxRetryDelay(20) == TimeSpan.FromSeconds(256),
    "transient retry uses bounded exponential backoff");
var saving = new SavingDto(7, "Emergency", "note", 500, date);
var savingEntry = new SavingTransactionDto(Id: 8, SavingId: 7, Type: "opening_balance", Amount: 500,
    SourceTransactionId: null, TransferGroupId: null, SourceCategoryName: null,
    FromWalletId: null, FromWalletName: null, ToWalletId: null, ToWalletName: null,
    RelatedSavingId: null, RelatedTransactionId: null, RelatedSavingName: null,
    Note: "Opening", UserId: 1, OccurredAt: date, CreatedAt: date);
var budget = new BudgetDto(9, 1, "September", 1, "Food", null, null, 1000, 250, 750, date);
var member = new UserDto(2, "Partner", "partner@example.com");
await database.ReplaceDomainSnapshotAsync(1, new([saving], [savingEntry], [budget], [member]));
Check((await database.GetSavingsAsync(1)).Single() == saving
    && (await database.GetSavingTransactionsAsync(1, 7)).Single() == savingEntry,
    "saving list/detail ledger round-trips through domain cache");
Check((await database.GetBudgetsAsync(1, 1)).Single() == budget
    && (await database.GetUsersAsync(1)).Single() == member,
    "budget and public family member snapshots round-trip locally");
var historicalBudget = budget with { Id = 10, PeriodId = 2, PeriodName = "August" };
await database.ReplaceDomainSnapshotAsync(1, new([saving], [savingEntry], [budget, historicalBudget], [member]));
await database.ReplaceBudgetsAsync(1, [1], [budget with { PlannedAmount = 1_500 }]);
Check((await database.GetBudgetsAsync(1, 1)).Single().PlannedAmount == 1_500
    && (await database.GetBudgetsAsync(1, 2)).Single() == historicalBudget,
    "targeted budget refresh replaces only requested periods and preserves history");
await database.ReplaceSavingsSnapshotAsync(1, [saving], [savingEntry]);
Check((await database.GetBudgetsAsync(1, 2)).Single() == historicalBudget
    && (await database.GetUsersAsync(1)).Single() == member,
    "saving-only refresh does not trigger or erase budget and member projections");
Check((await database.GetSavingsAsync(2)).Count == 0,
    "domain cache is isolated by signed-in account");

var savingMutationPath = Path.Combine(Path.GetTempPath(), $"family-budget-saving-mutation-{Guid.NewGuid():N}.db3");
var savingMutationDatabase = new LocalDatabase(savingMutationPath);
var targetSaving = new SavingDto(9, "Education", null, 100, date);
var directExpense = new SavingTransactionDto(Id: 81, SavingId: 7, Type: "expense", Amount: 100,
    SourceTransactionId: null, TransferGroupId: null, SourceCategoryName: null,
    FromWalletId: null, FromWalletName: null, ToWalletId: null, ToWalletName: null,
    RelatedSavingId: null, RelatedTransactionId: null, RelatedSavingName: null,
    Note: "Old", UserId: 1, OccurredAt: date, CreatedAt: date, Version: 2);
var targetedSavingPath = Path.Combine(Path.GetTempPath(), $"family-budget-targeted-saving-{Guid.NewGuid():N}.db3");
var targetedSavingDatabase = new LocalDatabase(targetedSavingPath);
await targetedSavingDatabase.ReplaceDomainSnapshotAsync(1,
    new([saving, targetSaving], [savingEntry, directExpense], [], []));
var requestedSavingIds = new List<int>();
var targetedSavingApi = DispatchProxy.Create<IApiClient, ApiProxy>();
((ApiProxy)(object)targetedSavingApi).Handler = (method, args) => method.Name switch
{
    nameof(IApiClient.GetSavingTransactionAsync) => Task.FromResult(directExpense with { Amount = 110 }),
    nameof(IApiClient.GetSavingAsync) => GetTargetedSaving((int)args![0]!),
    nameof(IApiClient.GetSavingTransactionsAsync) => GetTargetedSavingTransactions((int)args![0]!),
    _ => throw new NotImplementedException(method.Name),
};
Task<SavingDetailDto> GetTargetedSaving(int id)
{
    requestedSavingIds.Add(id);
    if (id != 7) throw new Exception("Unrelated saving requested");
    return Task.FromResult(new SavingDetailDto(7, "Emergency", "note", 490, 500, date, date));
}
Task<List<SavingTransactionDto>> GetTargetedSavingTransactions(int id) =>
    Task.FromResult(new List<SavingTransactionDto> { savingEntry, directExpense with { Amount = 110 } });
var targetedSavingRepository = new DomainRepository(targetedSavingDatabase, targetedSavingApi, auth);
await targetedSavingRepository.RefreshSavingChangesAsync([new(1, "saving_transactions", 81, "upsert", date)]);
Check(requestedSavingIds.SequenceEqual([7])
    && (await targetedSavingDatabase.GetSavingsAsync(1)).Single(item => item.Id == 9) == targetSaving
    && (await targetedSavingDatabase.GetSavingTransactionAsync(1, 81))?.Amount == 110,
    "incremental saving sync refreshes only affected saving IDs and preserves unrelated savings");
await savingMutationDatabase.ReplaceLedgerSnapshotAsync(1, [wallet], [period], []);
await savingMutationDatabase.ReplaceDomainSnapshotAsync(1,
    new([saving, targetSaving], [savingEntry, directExpense], [], []));
var pendingDeposit = await savingMutationDatabase.EnqueueSavingTransactionAsync(1,
    new(7, "deposit", 200, 1, "Cash", null, null, null, null, "Offline deposit", date));
Check((await savingMutationDatabase.GetSavingsAsync(1)).Single(item => item.Id == 7).Balance == 700
    && (await savingMutationDatabase.GetWalletsAsync(1)).Single().Balance == 700
    && pendingDeposit.SyncStatus == "pending",
    "offline saving deposit optimistically updates saving and wallet balances");
await savingMutationDatabase.CancelOutboxAsync(1, pendingDeposit.LocalId!);
Check((await savingMutationDatabase.GetSavingsAsync(1)).Single(item => item.Id == 7).Balance == 500
    && (await savingMutationDatabase.GetWalletsAsync(1)).Single().Balance == 900,
    "cancelling saving deposit restores both balances");

var pendingTransfer = await savingMutationDatabase.EnqueueSavingTransactionAsync(1,
    new(7, "transfer_out", 100, null, null, null, null, 9, "Education", "Move", date));
var savingBalances = await savingMutationDatabase.GetSavingsAsync(1);
Check(savingBalances.Single(item => item.Id == 7).Balance == 400
    && savingBalances.Single(item => item.Id == 9).Balance == 200,
    "offline saving transfer applies both sides atomically in the local projection");
await savingMutationDatabase.CancelOutboxAsync(1, pendingTransfer.LocalId!);

var editedSavingExpense = await savingMutationDatabase.EnqueueSavingTransactionUpdateAsync(1, directExpense,
    new(7, "expense", 150, null, null, null, null, null, null, "Edited", date));
Check((await savingMutationDatabase.GetSavingsAsync(1)).Single(item => item.Id == 7).Balance == 450
    && (await savingMutationDatabase.GetSavingTransactionAsync(1, 81)) is { Amount: 150, SyncStatus: "pending" },
    "offline saving edit reverses the previous effect before applying the optimistic value");
await savingMutationDatabase.FailOutboxAsync(editedSavingExpense.LocalId!, "conflict", permanent: true);
Check((await savingMutationDatabase.GetSavingTransactionAsync(1, 81)) is { Amount: 150, SyncStatus: "failed" },
    "rejected saving edit keeps user input available");
await savingMutationDatabase.CancelOutboxAsync(1, editedSavingExpense.LocalId!);
await savingMutationDatabase.EnqueueSavingTransactionDeleteAsync(1, directExpense);
var savingDelete = (await savingMutationDatabase.GetPendingOutboxAsync(1)).Single();
Check(await savingMutationDatabase.GetSavingTransactionAsync(1, 81) is null
    && (await savingMutationDatabase.GetSavingsAsync(1)).Single(item => item.Id == 7).Balance == 600,
    "offline saving delete hides the row and reverses its balance effect");
await savingMutationDatabase.CompleteSavingOutboxAsync(savingDelete.OperationId, null);
Check((await savingMutationDatabase.GetPendingOutboxAsync(1)).Count == 0,
    "successful saving delete clears its local tombstone and outbox");

var savingProcessorPath = Path.Combine(Path.GetTempPath(), $"family-budget-saving-processor-{Guid.NewGuid():N}.db3");
var savingProcessorDatabase = new LocalDatabase(savingProcessorPath);
await savingProcessorDatabase.ReplaceLedgerSnapshotAsync(1, [wallet], [period], []);
await savingProcessorDatabase.ReplaceDomainSnapshotAsync(1, new([saving], [], [], []));
var savingProcessorNetwork = new TestNetwork { IsInternetAvailable = false };
CreateSavingDepositRequest? capturedSavingCreate = null;
CreateSavingDepositRequest? capturedSavingUpdate = null;
var savingApi = DispatchProxy.Create<IApiClient, ApiProxy>();
((ApiProxy)(object)savingApi).Handler = (method, args) => method.Name switch
{
    nameof(IApiClient.CreateSavingTransactionAsync) => CaptureSavingCreate(args),
    nameof(IApiClient.UpdateSavingTransactionAsync) => CaptureSavingUpdate(args),
    _ => throw new NotImplementedException(method.Name),
};
Task<SavingTransactionDto> CaptureSavingCreate(object?[]? args)
{
    capturedSavingCreate = (CreateSavingDepositRequest)args![1]!;
    return Task.FromResult(new SavingTransactionDto(Id: 90, SavingId: 7, Type: "deposit", Amount: capturedSavingCreate.Amount,
        SourceTransactionId: 91, TransferGroupId: null, SourceCategoryName: null,
        FromWalletId: capturedSavingCreate.FromWalletId, FromWalletName: "Cash", ToWalletId: null, ToWalletName: null,
        RelatedSavingId: null, RelatedTransactionId: null, RelatedSavingName: null, Note: capturedSavingCreate.Note,
        UserId: 1, OccurredAt: capturedSavingCreate.OccurredAt, CreatedAt: date,
        ClientMutationId: capturedSavingCreate.ClientMutationId, Version: 1));
}
Task<SavingTransactionDto> CaptureSavingUpdate(object?[]? args)
{
    capturedSavingUpdate = (CreateSavingDepositRequest)args![1]!;
    return Task.FromResult(new SavingTransactionDto(Id: 90, SavingId: 7, Type: "deposit", Amount: capturedSavingUpdate.Amount,
        SourceTransactionId: 91, TransferGroupId: null, SourceCategoryName: null,
        FromWalletId: capturedSavingUpdate.FromWalletId, FromWalletName: "Cash", ToWalletId: null, ToWalletName: null,
        RelatedSavingId: null, RelatedTransactionId: null, RelatedSavingName: null, Note: capturedSavingUpdate.Note,
        UserId: 1, OccurredAt: capturedSavingUpdate.OccurredAt, CreatedAt: date, Version: 2));
}
var savingProcessor = new OutboxSyncService(savingProcessorDatabase, savingApi, auth, savingProcessorNetwork);
await savingProcessor.EnqueueSavingAsync(new(7, "deposit", 200, 1, "Cash", null, null, null, null, null, date));
savingProcessorNetwork.IsInternetAvailable = true;
await savingProcessor.ProcessPendingAsync();
var syncedSavingDeposit = (await savingProcessorDatabase.GetSavingTransactionAsync(1, 90))!;
Check(capturedSavingCreate?.ClientMutationId is not null && syncedSavingDeposit.Version == 1,
    "saving outbox processor sends stable idempotency id for create");
savingProcessorNetwork.IsInternetAvailable = false;
await savingProcessor.EnqueueSavingUpdateAsync(syncedSavingDeposit,
    new(7, "deposit", 250, 1, "Cash", null, null, null, null, null, date));
savingProcessorNetwork.IsInternetAvailable = true;
await savingProcessor.ProcessPendingAsync();
Check(capturedSavingUpdate is { ExpectedVersion: 1, ClientMutationId: not null }
    && (await savingProcessorDatabase.GetSavingTransactionAsync(1, 90))?.Version == 2,
    "saving outbox processor sends expectedVersion for edit");

Check(await database.GetSyncCursorAsync(1) is null, "new account has no incremental cursor");
await database.SetSyncCursorAsync(1, 1234);
Check(await database.GetSyncCursorAsync(1) == 1234
    && await database.GetSyncCursorAsync(2) is null,
    "sync cursor persists independently per account");
var coordinatorPath = Path.Combine(Path.GetTempPath(), $"family-budget-sync-test-{Guid.NewGuid():N}.db3");
var coordinatorDatabase = new LocalDatabase(coordinatorPath);
var coordinatorApi = DispatchProxy.Create<IApiClient, ApiProxy>();
var coordinatorProxy = (ApiProxy)(object)coordinatorApi;
var feedCursor = 10L;
coordinatorProxy.Handler = (method, args) => method.Name switch
{
    nameof(IApiClient.GetSyncBootstrapAsync) => Task.FromResult(new SyncBootstrapDto(10)),
    nameof(IApiClient.GetSyncChangesAsync) => Task.FromResult(feedCursor == 10
        ? new SyncChangesDto([], 10, false)
        : new SyncChangesDto([new(11, "transactions", 1, "upsert", date)], 11, false)),
    _ => throw new NotImplementedException(method.Name),
};
var coordinatorAuth = new TestAuth();
var fakeReferences = new FakeReferences();
var fakeLedger = new FakeLedger();
var fakeDomain = new FakeDomain { Fail = true };
var coordinator = new SyncCoordinator(coordinatorApi, coordinatorDatabase, coordinatorAuth,
    fakeReferences, fakeLedger, fakeDomain, new FakeOutbox(), new TestNetwork { IsInternetAvailable = true });
try { await coordinator.SynchronizeAsync(); }
catch (Exception exception) when (exception.Message == "refresh failed") { }
Check(await coordinatorDatabase.GetSyncCursorAsync(1) is null,
    "failed bootstrap refresh never advances the sync cursor");
fakeDomain.Fail = false;
await coordinator.SynchronizeAsync();
Check(await coordinatorDatabase.GetSyncCursorAsync(1) == 10,
    "successful bootstrap commits captured cursor after all projections");
await coordinatorDatabase.ReplaceLedgerSnapshotAsync(1, [wallet], [period], [Entry(1, "expense", 1, null, 1)]);
feedCursor = 11;
await coordinator.SynchronizeAsync();
Check(await coordinatorDatabase.GetSyncCursorAsync(1) == 11 && fakeLedger.RefreshCount >= 2
    && fakeDomain.RefreshedBudgetPeriods.SequenceEqual([1]) && fakeDomain.RefreshCount == 2,
    "transaction change refreshes only its affected budget period, not the full domain snapshot");
var fullResyncPending = await coordinatorDatabase.EnqueueTransactionAsync(1, new PendingTransactionInput(
    1, "expense", 1, "Cash", null, null, 1, "Food", 90, null, date));
feedCursor = 10;
await coordinator.ForceFullResyncAsync();
Check(await coordinatorDatabase.GetSyncCursorAsync(1) == 10
    && (await coordinatorDatabase.GetPendingOutboxAsync(1)).Any(item => item.OperationId == fullResyncPending.LocalId),
    "full resync replaces the cursor without deleting pending local mutations");
var diagnostics = await coordinator.GetStatusAsync();
Check(diagnostics is { Status: "synced", LastSuccessAt: not null, QueueDepth: 1, LastErrorCode: null },
    "sync diagnostics persist last success and queue depth without payload data");

var legacyPath = Path.Combine(Path.GetTempPath(), $"family-budget-legacy-test-{Guid.NewGuid():N}.db3");
using (var legacy = new SQLite.SQLiteConnection(legacyPath))
{
    legacy.Execute("CREATE TABLE outbox (OperationId varchar PRIMARY KEY, UserId integer, EntityLocalId varchar, TransactionType varchar, PayloadJson varchar, Status varchar, AttemptCount integer, CreatedAtUnixMilliseconds integer, LastAttemptAtUnixMilliseconds integer)");
    legacy.Execute("CREATE TABLE transactions (Key varchar PRIMARY KEY, UserId integer, ServerId integer, PeriodId integer, FromWalletId integer, ToWalletId integer, Type varchar, OccurredAtUnixMilliseconds integer, CreatedAtUnixMilliseconds integer, PayloadJson varchar)");
    legacy.Execute("PRAGMA user_version=1");
}
var migrated = new LocalDatabase(legacyPath);
var migratedPending = await migrated.EnqueueTransactionAsync(1, new PendingTransactionInput(
    1, "income", null, null, 1, "Cash", null, null, 25, null, date));
using (var upgraded = new SQLite.SQLiteConnection(legacyPath))
{
    Check(upgraded.ExecuteScalar<int>("PRAGMA user_version") == LocalDatabase.CurrentSchemaVersion
        && upgraded.ExecuteScalar<int>("SELECT COUNT(*) FROM pragma_table_info('outbox') WHERE name='NextAttemptAtUnixMilliseconds'") == 1
        && upgraded.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='outbox_user_status_created_idx'") == 1
        && migratedPending.LocalId is not null,
        "legacy database migrates columns, schema version and hot-query indexes without data reset");
}
Console.WriteLine("All local database/repository/outbox regression checks passed.");
