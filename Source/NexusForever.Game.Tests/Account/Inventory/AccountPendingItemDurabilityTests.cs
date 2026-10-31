using System.Reflection;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Tests.Account.Inventory;

public sealed class AccountPendingItemDurabilityTests : IDisposable
{
    private const uint AccountId = 1;
    private const uint ItemId = 77;
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"NexusPendingDurability_{Guid.NewGuid():N}");
    private readonly DatabaseConnectionString connection;

    public AccountPendingItemDurabilityTests()
    {
        Directory.CreateDirectory(directory);
        connection = Config("pending.sqlite");
        using var context = Context();
        context.Database.EnsureCreated();
        context.Account.Add(Account());
        context.SaveChanges();
    }

    [Fact]
    public void StaleSnapshotPreservesExternalAppendWhileSavingNewLocalGrant()
    {
        Seed(7, "original");
        AccountInventoryManager manager = Manager();
        Repository().AppendPendingGroup(AccountId, Insert("external"));
        manager.AddPendingItemGroup([ItemId], group: "local", notify: false);
        Save(manager);
        Assert.Equal(new[] { "external", "local", "original" }, Rows().Select(row => row.GroupName).Order().ToArray());
        Assert.Equal(3, Rows().Select(row => row.PendingItemId).Distinct().Count());
    }

    [Fact]
    public void CommittedAllocationCollisionReplacesPreviouslyAnnouncedClientCacheId()
    {
        AccountInventoryManager manager = Manager(out var session);
        manager.AddPendingItemGroup([ItemId], group: "local", notify: true);
        ulong provisional = session.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<ServerAccountPendingItemAdd>().Single().PendingGroup.Id;
        Repository().AppendPendingGroup(AccountId, Insert("external"));
        Save(manager);
        object[] messages = session.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).ToArray();
        ServerAccountPendingItemAdd[] adds = messages.OfType<ServerAccountPendingItemAdd>().ToArray();
        Assert.Equal(2, adds.Length);
        Assert.NotEqual(provisional, adds[1].PendingGroup.Id);
        Assert.Equal(Rows().Single(row => row.GroupName == "local").PendingItemId, adds[1].PendingGroup.Id);
        ServerAccountPendingItemGroupDelete deleted = Assert.Single(messages.OfType<ServerAccountPendingItemGroupDelete>());
        Assert.Equal("local", deleted.Value);
        Assert.True(Array.IndexOf(messages, deleted) < Array.IndexOf(messages, adds[1]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InFlightAdditionCannotShareRebasedCommittedClientCacheId(bool stageBeforeAddition)
    {
        AccountInventoryManager manager = Manager(out var session);
        manager.AddPendingItemGroup([ItemId], group: "A", notify: true);
        Repository().AppendPendingGroup(AccountId, Insert("external"));
        await using (var context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            manager.Save(context);
            if (stageBeforeAddition) await context.SaveChangesAsync();
            manager.AddPendingItemGroup([ItemId], group: "B", notify: true);
            if (!stageBeforeAddition) await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        Assert.Equal(new[] { "A", "B" }, ClientPendingCache(session).Values.Select(row => row.Group).Order().ToArray());
        Save(manager);
        Dictionary<ulong, ServerAccountItemsPending.PendingAccountItemGroup> cache = ClientPendingCache(session);
        foreach (string group in new[] { "A", "B" })
            Assert.Equal(Rows().Single(row => row.GroupName == group).PendingItemId, cache.Values.Single(row => row.Group == group).Id);
    }

    [Fact]
    public void SnapshotAdvertisementOfInitiallySilentAdditionIsReconciledOnRebase()
    {
        AccountInventoryManager manager = Manager(out var session);
        manager.AddPendingItemGroup([ItemId], group: "local", notify: false);
        manager.SendPendingItems();
        Repository().AppendPendingGroup(AccountId, Insert("external"));
        Save(manager);
        ServerAccountItemsPending.PendingAccountItemGroup item = Assert.Single(ClientPendingCache(session).Values);
        Assert.Equal("local", item.Group);
        Assert.Equal(Rows().Single(row => row.GroupName == "local").PendingItemId, item.Id);
    }

    private static Dictionary<ulong, ServerAccountItemsPending.PendingAccountItemGroup> ClientPendingCache(RecordingDispatchProxy<IGameSession> session)
    {
        var cache = new Dictionary<ulong, ServerAccountItemsPending.PendingAccountItemGroup>();
        foreach (object message in session.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]))
            switch (message)
            {
                case ServerAccountPendingItemAdd add: cache[add.PendingGroup.Id] = add.PendingGroup; break;
                case ServerAccountItemsPending snapshot:
                    foreach (var row in snapshot.PendingGroups) cache[row.Id] = row;
                    break;
                case ServerAccountPendingItemGroupDelete delete when delete.Flag:
                    foreach (ulong key in cache.Where(pair => pair.Value.Group.Equals(delete.Value, StringComparison.OrdinalIgnoreCase)).Select(pair => pair.Key).ToArray()) cache.Remove(key);
                    break;
                case ServerAccountPendingItemsClear: cache.Clear(); break;
            }
        return cache;
    }

    [Fact]
    public void MySqlSnapshotContainsSequenceAndReceiptsWithoutPendingModelChanges()
    {
        var options = new DbContextOptionsBuilder<AuthContext>().UseMySql("Server=127.0.0.1;Database=not-opened;User=test", new MySqlServerVersion(new Version(8, 0, 36))).Options;
        using var context = new AuthContext(options);
        Assert.False(context.Database.HasPendingModelChanges());
        string sql = context.GetService<IMigrator>().GenerateScript("20260610114005_StorePurchaseHistoryAccountPurchasedUtcIndex");
        Assert.Contains("nextPendingItemId", sql);
        Assert.Contains("account_pending_item_delivery", sql);
    }

    [Fact]
    public void StaleSnapshotDoesNotResurrectExternallyConsumedRows()
    {
        Seed(7, "consumed");
        AccountInventoryManager manager = Manager();
        Repository().RemovePendingGroup(AccountId, "consumed");
        manager.AddPendingItemGroup([ItemId], group: "local", notify: false);
        Save(manager);
        Save(manager);
        Assert.Equal("local", Assert.Single(Rows()).GroupName);
        Assert.False(manager.RemovePendingItemGroup("consumed", notify: false));
    }

    [Fact]
    public void OwnedDeleteDoesNotRemoveExternallyAppendedRowsWithSameGroup()
    {
        Seed(7, "shared");
        AccountInventoryManager manager = Manager();
        Repository().AppendPendingGroup(AccountId, Insert("shared"));
        Assert.True(manager.RemovePendingItemGroup("shared", notify: false));
        Save(manager);
        Assert.True(Assert.Single(Rows()).PendingItemId > 7);
    }

    [Fact]
    public void OwnedUpdatePreservesUnrelatedExternalColumns()
    {
        Seed(7, "target", target: 10);
        AccountInventoryManager manager = Manager();
        Retarget(manager, "target", 99);
        using (var external = Context())
        {
            AccountPendingItemModel row = external.AccountPendingItem.Single();
            row.ClaimState = 3;
            external.SaveChanges();
        }
        Save(manager);
        AccountPendingItemModel saved = Assert.Single(Rows());
        Assert.Equal(99ul, saved.TargetCharacterId);
        Assert.Equal((byte)3, saved.ClaimState);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OwnedUpdateOrDeleteOfConsumedRowIsIdempotent(bool remove)
    {
        Seed(7, "consumed", target: 10);
        AccountInventoryManager manager = Manager();
        if (remove) manager.RemovePendingItemGroup("consumed", notify: false);
        else Retarget(manager, "consumed", 99);
        Repository().RemovePendingGroup(AccountId, "consumed");
        Save(manager);
        Assert.Empty(Rows());
    }

    [Fact]
    public void ConsumedHighestIdIsNeverReallocatedToUnrelatedGrant()
    {
        AccountPendingItemRepository repository = Repository();
        repository.AppendPendingGroup(AccountId, Insert("old"));
        ulong oldId = Assert.Single(Rows()).PendingItemId;
        AccountInventoryManager stale = Manager();
        repository.RemovePendingGroup(AccountId, "old");
        repository.AppendPendingGroup(AccountId, Insert("new"));
        Assert.True(Assert.Single(Rows()).PendingItemId > oldId);
        stale.RemovePendingItemGroup("old", notify: false);
        Save(stale);
        Assert.Equal("new", Assert.Single(Rows()).GroupName);
    }

    [Fact]
    public async Task IndependentManagersAndRepositoryAllocateWithoutCollisions()
    {
        AccountInventoryManager first = Manager();
        AccountInventoryManager second = Manager();
        first.AddPendingItemGroup([ItemId, ItemId], group: "first", notify: false);
        second.AddPendingItemGroup([ItemId, ItemId], group: "second", notify: false);
        using var ready = new Barrier(3);
        await Task.WhenAll(
            Task.Run(() => { ready.SignalAndWait(); Save(first); }),
            Task.Run(() => { ready.SignalAndWait(); Save(second); }),
            Task.Run(() => { ready.SignalAndWait(); Repository().AppendPendingGroup(AccountId, Insert("external")); }));
        Assert.Equal(5, Rows().Count);
        Assert.Equal(5, Rows().Select(row => row.PendingItemId).Distinct().Count());
    }

    [Fact]
    public void FailedFlushRetainsNewGrantAndDeleteIntentForFreshContextRetry()
    {
        Seed(7, "old");
        AccountInventoryManager manager = Manager();
        manager.RemovePendingItemGroup("old", notify: false);
        manager.AddPendingItemGroup([ItemId], group: "new", notify: false);
        using (var failed = Context())
        {
            failed.Database.ExecuteSqlRaw("CREATE TRIGGER reject_pending BEFORE INSERT ON account_pending_item BEGIN SELECT RAISE(ABORT, 'owned failure'); END");
            manager.Save(failed);
            Assert.Throws<DbUpdateException>(() => failed.SaveChanges());
        }
        Assert.Equal("old", Assert.Single(Rows()).GroupName);
        using (var cleanup = Context()) cleanup.Database.ExecuteSqlRaw("DROP TRIGGER reject_pending");
        Save(manager);
        Assert.Equal("new", Assert.Single(Rows()).GroupName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveAcknowledgesOnlyActualOuterTransactionCompletion(bool commit)
    {
        AccountInventoryManager manager = Manager();
        manager.AddPendingItemGroup([ItemId], group: "owned", notify: false);
        await using (var context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            manager.Save(context);
            await context.SaveChangesAsync();
            if (commit) await transaction.CommitAsync(); else await transaction.RollbackAsync();
        }
        Assert.Equal(commit ? 1 : 0, Rows().Count);
        Save(manager);
        Assert.Single(Rows());
    }

    [Fact]
    public async Task SavepointRollbackRetainsIntentUntilTerminalCommitThenRetry()
    {
        AccountInventoryManager manager = Manager();
        manager.AddPendingItemGroup([ItemId], group: "owned", notify: false);
        await using (var context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            await transaction.CreateSavepointAsync("before_pending");
            manager.Save(context);
            await context.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("before_pending");
            await transaction.CommitAsync();
        }
        Assert.Empty(Rows());
        Save(manager);
        Assert.Single(Rows());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UncertainSavepointCompletionDoesNotDuplicateOrResurrectCommittedAppend(bool consumedBeforeRetry)
    {
        AccountInventoryManager manager = Manager();
        manager.AddPendingItemGroup([ItemId], group: "owned", notify: false);
        await using (var context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            manager.Save(context);
            await context.SaveChangesAsync();
            await transaction.CreateSavepointAsync("after_pending");
            await transaction.RollbackToSavepointAsync("after_pending");
            await transaction.CommitAsync();
        }
        Assert.Single(Rows());
        if (consumedBeforeRetry) Repository().RemovePendingGroup(AccountId, "owned");
        Save(manager);
        Assert.Equal(consumedBeforeRetry ? 0 : 1, Rows().Count);
        using var verify = Context();
        Assert.Single(verify.AccountPendingItemDelivery.Where(row => row.DeliveryKey.StartsWith("pending:")));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ProviderRetryCoversWholeUnitAndVerifiesAmbiguousCommitWithoutReplayingOtherWrites(bool afterCommit, bool asynchronous)
    {
        AccountInventoryManager manager = Manager();
        manager.AddPendingItemGroup([ItemId], group: "owned", notify: false);
        var failure = new FailCommitOnce(afterCommit);
        var options = new DbContextOptionsBuilder<AuthContext>();
        options.UseConfiguration(connection);
        options.ReplaceService<IExecutionStrategyFactory, RetryingStrategyFactory>();
        options.AddInterceptors(failure);
        await using (var context = new AuthContext(options.Options))
        {
            context.AccountStorePurchaseHistory.Add(new AccountStorePurchaseHistoryModel { AccountId = AccountId, OfferId = 12, PurchasedUtc = DateTime.UtcNow });
            manager.Save(context);
            if (asynchronous) await context.SaveChangesAsync(); else context.SaveChanges();
        }
        Assert.Equal(1, failure.Failures);
        Assert.Single(Rows());
        using var verify = Context();
        Assert.Single(verify.AccountStorePurchaseHistory);
        Assert.Single(verify.AccountPendingItemDelivery.Where(row => row.DeliveryKey.StartsWith("pending:")));
        Save(manager);
        Assert.Single(Rows());
    }

    [Fact]
    public async Task DisposedOuterTransactionCannotBeAcknowledgedByLaterEmptySave()
    {
        AccountInventoryManager manager = Manager();
        manager.AddPendingItemGroup([ItemId], group: "owned", notify: false);
        await using (var context = Context())
        {
            var transaction = await context.Database.BeginTransactionAsync();
            manager.Save(context);
            await context.SaveChangesAsync();
            await transaction.DisposeAsync();
            await context.SaveChangesAsync();
        }
        Assert.Empty(Rows());
        Save(manager);
        Assert.Single(Rows());
    }

    [Fact]
    public void DisposingUnstagedContextReleasesIntentWithoutLosingIt()
    {
        AccountInventoryManager manager = Manager();
        manager.AddPendingItemGroup([ItemId], group: "owned", notify: false);
        using (var abandoned = Context()) manager.Save(abandoned);
        Save(manager);
        Assert.Single(Rows());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NewMutationsDuringInFlightSaveSurviveCompletion(bool commit)
    {
        Seed(7, "target", target: 10);
        AccountInventoryManager manager = Manager();
        Retarget(manager, "target", 99);
        manager.AddPendingItemGroup([ItemId], group: "removed_while_saving", notify: false);
        await using (var context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            manager.Save(context);
            await context.SaveChangesAsync();
            manager.RemovePendingItemGroup("removed_while_saving", notify: false);
            Retarget(manager, "target", 100);
            manager.AddPendingItemGroup([ItemId], group: "added_while_saving", notify: false);
            // A competing account save must not stage the same owner's pending unit.
            using (var competing = Context()) manager.Save(competing);
            if (commit) await transaction.CommitAsync(); else await transaction.RollbackAsync();
        }
        Save(manager);
        AccountPendingItemModel target = Rows().Single(row => row.GroupName == "target");
        Assert.Equal(100ul, target.TargetCharacterId);
        Assert.Equal(new[] { "added_while_saving", "target" }, Rows().Select(row => row.GroupName).Order().ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SequenceMigrationSupportsFreshAndPopulatedSqlite(bool populated)
    {
        await using var context = new AuthContext(Config("migration.sqlite"));
            string previous = context.Database.GetMigrations().TakeWhile(id => !id.EndsWith("_AccountPendingItemSequence", StringComparison.Ordinal)).Last();
        if (populated)
        {
            await context.GetService<IMigrator>().MigrateAsync(previous);
            await context.Database.ExecuteSqlRawAsync("INSERT INTO account(id,email,s,v,gameToken,sessionKey) VALUES(1,'migration@example.test','','','','')");
            await context.Database.ExecuteSqlRawAsync("INSERT INTO account_pending_item(id,pendingItemId,groupName,accountItemId) VALUES(1,41,'existing',77)");
        }
        await context.Database.MigrateAsync();
        if (!populated)
        {
            context.Account.Add(Account());
            await context.SaveChangesAsync();
        }
        ulong allocated = 0;
        context.EnlistSaveWork(unit => allocated = unit.ReservePendingItemIds(AccountId, 2), _ => { });
        await context.SaveChangesAsync();
        Assert.Equal(populated ? 42ul : 1ul, allocated);
        Assert.Equal(allocated + 2, await context.Account.Select(row => row.NextPendingItemId).SingleAsync());
        Assert.Equal(populated ? 1 : 0, await context.AccountPendingItem.CountAsync());
    }

    [Fact]
    public void ExhaustedSequenceDoesNotCommitOtherTrackedChanges()
    {
        using (var seed = Context())
        {
            seed.Account.Single().NextPendingItemId = ulong.MaxValue;
            seed.SaveChanges();
        }
        AccountInventoryManager manager = Manager();
        manager.AddPendingItemGroup([ItemId], group: "overflow", notify: false);
        using var context = Context();
        context.Account.Single().Email = "must-rollback@example.test";
        manager.Save(context);
        Assert.Throws<OverflowException>(() => context.SaveChanges());
        Assert.Empty(Rows());
        using var verify = Context();
        Assert.Equal("pending@example.test", verify.Account.Single().Email);
    }

    private DatabaseConnectionString Config(string name) => new() { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={Path.Combine(directory, name)};Default Timeout=15" };
    private AuthContext Context() => new(connection);
    private static AccountModel Account() => new() { Id = AccountId, Email = "pending@example.test", S = "", V = "", GameToken = "", SessionKey = "" };
    private static AccountPendingItemInsert Insert(string group) => new() { GroupName = group, AccountItemIds = [ItemId] };
    private List<AccountPendingItemModel> Rows() { using var context = Context(); return context.AccountPendingItem.AsNoTracking().ToList(); }
    private void Save(AccountInventoryManager manager) { using var context = Context(); manager.Save(context); context.SaveChanges(); }
    private void Seed(ulong id, string group, ulong target = 0)
    {
        using var context = Context();
        context.AccountPendingItem.Add(new AccountPendingItemModel { Id = AccountId, PendingItemId = id, GroupName = group, AccountItemId = ItemId, TargetRealmId = 1, TargetCharacterId = target });
        context.SaveChanges();
    }

    private AccountPendingItemRepository Repository()
    {
        var database = new AuthDatabase();
        database.Initialise(connection);
        IDatabaseManager manager = RecordingDispatchProxy<IDatabaseManager>.Create(out var proxy);
        proxy.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        return new AccountPendingItemRepository(manager);
    }

    private AccountInventoryManager Manager() => Manager(out _);

    private AccountInventoryManager Manager(out RecordingDispatchProxy<IGameSession> session)
    {
        using var context = Context();
        AccountModel model = context.Account.AsNoTracking().Include(row => row.AccountPendingItem).Single();
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Id), AccountId);
        accountProxy.SetProperty(nameof(IAccount.Session), RecordingDispatchProxy<IGameSession>.Create(out session));
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        var table = (GameTable<AccountItemEntry>)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GameTable<AccountItemEntry>));
        typeof(GameTable<AccountItemEntry>).GetField("<Entries>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(table, new[] { new AccountItemEntry { Id = ItemId } });
        typeof(GameTable<AccountItemEntry>).GetField("header", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(table, new GameTableHeader { MaxId = ItemId + 1 });
        int[] lookup = Enumerable.Repeat(-1, (int)ItemId + 1).ToArray(); lookup[ItemId] = 0;
        typeof(GameTable<AccountItemEntry>).GetField("lookup", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(table, lookup);
        tableProxy.SetProperty(nameof(IGameTableManager.AccountItem), table);
        ICharacterManager characters = RecordingDispatchProxy<ICharacterManager>.Create(out var characterProxy);
        characterProxy.SetMethodHandler(nameof(ICharacterManager.GetCharacter), args =>
        {
            ICharacter character = RecordingDispatchProxy<ICharacter>.Create(out var proxy);
            proxy.SetProperty(nameof(ICharacter.AccountId), AccountId);
            proxy.SetProperty(nameof(ICharacter.CharacterId), (ulong)args[0]);
            return character;
        });
        return new AccountInventoryManager(account, model, null, characterManager: characters, gameTableManager: tables);
    }

    private static void Retarget(AccountInventoryManager manager, string group, ulong target)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { RealmId = 1, Id = 50 });
        Assert.Equal(AccountOperationResult.Ok, manager.GiftPendingItemGroupToCharacter(player, group, new NetworkIdentity { RealmId = 1, Id = target }));
    }

    public void Dispose()
    {
        foreach (string name in new[] { "pending.sqlite", "migration.sqlite" })
        {
            using var pool = new Microsoft.Data.Sqlite.SqliteConnection(Config(name).ConnectionString);
            Microsoft.Data.Sqlite.SqliteConnection.ClearPool(pool);
        }
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    private sealed class TransientCommitException : Exception { }
    private sealed class RetryingStrategyFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new RetryingStrategy(dependencies);
    }
    private sealed class RetryingStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 2, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is TransientCommitException;
    }
    private sealed class FailCommitOnce(bool afterCommit) : DbTransactionInterceptor
    {
        public int Failures { get; private set; }
        private void Fail()
        {
            if (Failures == 0) { Failures++; throw new TransientCommitException(); }
        }
        public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result)
        {
            if (!afterCommit) Fail();
            return result;
        }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(TransactionCommitting(transaction, eventData, result));
        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        {
            if (afterCommit) Fail();
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            TransactionCommitted(transaction, eventData);
            return Task.CompletedTask;
        }
    }
}
