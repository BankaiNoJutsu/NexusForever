using System.Data;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Costume;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Entitlement;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Account.Option;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Option;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Tests.Transport;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Option;
using AccountOwner = NexusForever.Game.Account.Account;
using NetworkBinding = NexusForever.Network.World.Message.Model.Shared.Binding;

namespace NexusForever.Game.Tests.Option;

public sealed class AccountKeybindingPersistenceTests
{
    private readonly DbContextOptions<AuthContext> options;
    public AccountKeybindingPersistenceTests() { }
    internal AccountKeybindingPersistenceTests(DbContextOptions<AuthContext> options) => this.options = options;
    private const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    [Theory]
    [InlineData("new")] [InlineData("edit")] [InlineData("delete")]
    public async Task AcceptedAccountBindingsSurviveFailedFlushAndFreshRetry(string action)
    {
        using var f = new Fixture(options, action != "new"); var manager = f.Manager();
        manager.Update(action == "delete" ? Set() : Set(Binding(1, 20)));
        await Assert.ThrowsAsync<IOException>(() => f.Save(manager, fail: true)); await f.Save(manager);
        using var stored = f.Context(); if (action == "delete") Assert.Empty(stored.AccountKeybinding); else Assert.Equal(20u, Assert.Single(stored.AccountKeybinding).Code00);
        Assert.Empty(stored.AccountEntitlement);
    }

    [Theory]
    [InlineData("dispose")] [InlineData("rollback")] [InlineData("suppressed")] [InlineData("lost-ack")] [InlineData("before-commit")]
    public async Task WholeSetRecoversAfterAnUncertainTransaction(string fault)
    {
        using var f = new Fixture(options, two: true); var manager = f.Manager(); manager.Update(Set(Binding(1, 20), Binding(3, 30)));
        if (fault is "lost-ack" or "before-commit")
        {
            await using var context = f.Context(new CommitFailure(fault == "lost-ack")); manager.Save(context);
            await Assert.ThrowsAsync<IOException>(() => context.SaveChangesAsync());
        }
        else if (fault == "suppressed")
        {
            await using var context = f.Context(new SuppressedFlush()); manager.Save(context);
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }
        else
        {
            await using var context = f.Context(); await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(context); if (fault == "rollback") { await context.SaveChangesAsync(); await transaction.RollbackAsync(); }
        }
        await f.Save(manager);
        using var stored = f.Context(); Assert.Equal(new uint[] { 20, 30 }, stored.AccountKeybinding.OrderBy(row => row.InputActionId).Select(row => row.Code00));
        Assert.Equal(new ushort[] { 1, 3 }, f.Manager().Build().Bindings.OrderBy(row => row.InputActionId).Select(row => row.InputActionId));
        await f.Save(manager); Assert.Empty(stored.AccountEntitlement);
    }

    [Theory]
    [InlineData("edit")] [InlineData("delete")] [InlineData("restore")]
    public async Task CommitAcknowledgesOnlyTheFrozenSetAndRetainsLaterActorIntent(string action)
    {
        using var f = new Fixture(options); var manager = f.Manager(); manager.Update(action == "restore" ? Set() : Set(Binding(1, 20)));
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            manager.Save(context); await context.SaveChangesAsync(false); context.ChangeTracker.AcceptAllChanges();
            manager.Update(action == "delete" ? Set() : Set(Binding(1, 30))); await transaction.CommitAsync();
        }
        using (var first = f.Context()) { if (action == "restore") Assert.Empty(first.AccountKeybinding); else Assert.Equal(20u, Assert.Single(first.AccountKeybinding).Code00); }
        await f.Save(manager); using var stored = f.Context(); if (action == "delete") Assert.Empty(stored.AccountKeybinding); else Assert.Equal(30u, Assert.Single(stored.AccountKeybinding).Code00);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RegistrationFreezesTheSetBeforeAuthStaging(bool synchronous)
    {
        using var f = new Fixture(options); var manager = f.Manager(); manager.Update(Set(Binding(1, 20)));
        await using (var context = f.Context())
        {
            manager.Save(context); manager.Update(Set(Binding(2, 30)));
            if (synchronous) context.SaveChanges(); else await context.SaveChangesAsync();
        }
        using (var first = f.Context()) Assert.Equal(20u, Assert.Single(first.AccountKeybinding).Code00);
        await f.Save(manager); using var stored = f.Context(); Assert.Equal((ushort)2, Assert.Single(stored.AccountKeybinding).InputActionId);
    }

    [Theory]
    [InlineData("new")] [InlineData("edit")] [InlineData("delete")] [InlineData("restore")]
    public async Task SameOuterTransactionCanRestageAfterSaveChangesFalse(string action)
    {
        using var f = new Fixture(options, action != "new"); var manager = f.Manager();
        manager.Update(action == "restore" ? Set() : Set(Binding(1, 20)));
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            var account = await context.Account.SingleAsync(); account.GameToken = "unrelated-intent";
            manager.Save(context); await context.SaveChangesAsync(false);
            manager.Update(action == "delete" ? Set() : Set(Binding(1, 30)));
            manager.Save(context); await context.SaveChangesAsync(false); await transaction.CommitAsync();
        }
        using var stored = f.Context(); if (action == "delete") Assert.Empty(stored.AccountKeybinding); else Assert.Equal(30u, Assert.Single(stored.AccountKeybinding).Code00);
        Assert.Equal("unrelated-intent", stored.Account.Single().GameToken);
    }

    [Fact]
    public async Task UnchangedResavePreservesAVerifiedOuterTransactionFlush()
    {
        using var f = new Fixture(options); var manager = f.Manager(); manager.Update(Set(Binding(1, 20)));
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            manager.Save(context); await context.SaveChangesAsync(); manager.Save(context); await transaction.CommitAsync();
        }
        await f.Save(manager); using var stored = f.Context(); Assert.Equal(20u, Assert.Single(stored.AccountKeybinding).Code00);
    }

    [Fact]
    public async Task RecoveryRejectsACombinationOfSeparatelyMatchingRows()
    {
        using var f = new Fixture(options, two: true); var manager = f.Manager(); manager.Update(Set(Binding(1, 20), Binding(3, 30)));
        await Assert.ThrowsAsync<IOException>(() => f.Save(manager, fail: true));
        using (var foreign = f.Context()) await foreign.AccountKeybinding.Where(row => row.InputActionId == 1).ExecuteUpdateAsync(set => set.SetProperty(row => row.Code00, 20u));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Save(manager));
        using var stored = f.Context(); Assert.Equal(new uint[] { 20, 11 }, stored.AccountKeybinding.OrderBy(row => row.InputActionId).Select(row => row.Code00));
    }

    [Theory]
    [InlineData("tracked-before")] [InlineData("tracked-after")] [InlineData("sql-after")]
    public async Task ForeignIntentRejectsTheWholeAuthUnit(string fault)
    {
        using var f = new Fixture(options); var manager = f.Manager(); manager.Update(Set(Binding(1, 20)));
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            var account = await context.Account.SingleAsync(); account.GameToken = "must-rollback";
            if (fault == "tracked-before") (await context.AccountKeybinding.SingleAsync()).Code00 = 90;
            manager.Save(context);
            if (fault == "tracked-before") await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            else
            {
                await context.SaveChangesAsync();
                if (fault == "tracked-after") context.AccountKeybinding.Local.Single().Code00 = 90;
                else await context.AccountKeybinding.ExecuteUpdateAsync(set => set.SetProperty(row => row.Code00, 90u));
                await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
            }
            await transaction.RollbackAsync();
        }
        await f.Save(manager); using var stored = f.Context(); Assert.Equal(20u, Assert.Single(stored.AccountKeybinding).Code00); Assert.Equal("", stored.Account.Single().GameToken);
    }

    [Fact]
    public async Task AccountSetAndBoundMemberApisShareRecoveryAndRejectRemovedMembers()
    {
        using var f = new Fixture(options); var set = new KeybindingSet(f.Load()); var member = Assert.Single(set);
        member.Update(Binding(1, 20));
        await using (var context = f.Context()) { member.Save(context); await context.SaveChangesAsync(); }
        set.Update(Set()); await using (var context = f.Context()) { set.Save(context); await context.SaveChangesAsync(); }
        await using var rejected = f.Context(); Assert.Throws<InvalidOperationException>(() => member.Save(rejected));
        await Assert.ThrowsAsync<InvalidOperationException>(() => rejected.SaveChangesAsync()); using var stored = f.Context(); Assert.Empty(stored.AccountKeybinding);
    }

    [Fact]
    public async Task UnknownActionAndEveryRawSlotSurviveReloadWithoutGrantingEntitlements()
    {
        using var f = new Fixture(options); var manager = f.Manager(); var value = Binding(ushort.MaxValue, uint.MaxValue);
        value.DeviceEnum00 = 100; value.DeviceEnum01 = 101; value.DeviceEnum02 = 102;
        value.Code01 = 201; value.Code02 = 202; value.MetaKeys00 = 300; value.MetaKeys01 = 301; value.MetaKeys02 = 302;
        value.EventTypeEnum00 = 400; value.EventTypeEnum01 = 401; value.EventTypeEnum02 = 402;
        manager.Update(Set(value)); await f.Save(manager); var actual = Assert.Single(f.Manager().Build().Bindings);
        Assert.Equal(value.InputActionId, actual.InputActionId);
        Assert.Equal(new uint[] { 100, 101, 102, uint.MaxValue, 201, 202, 300, 301, 302, 400, 401, 402 },
            new[] { actual.DeviceEnum00, actual.DeviceEnum01, actual.DeviceEnum02, actual.Code00, actual.Code01, actual.Code02,
                actual.MetaKeys00, actual.MetaKeys01, actual.MetaKeys02, actual.EventTypeEnum00, actual.EventTypeEnum01, actual.EventTypeEnum02 });
        using var stored = f.Context(); Assert.Empty(stored.AccountEntitlement);
    }

    [Fact]
    public async Task CancelingANewBindingAndRestoringACommittedBindingPreservesReplacementSemantics()
    {
        using var f = new Fixture(options, existing: false); var manager = f.Manager(); manager.Update(Set(Binding(1, 20))); manager.Update(Set()); await f.Save(manager);
        using (var empty = f.Context()) Assert.Empty(empty.AccountKeybinding);
        manager.Update(Set(Binding(1, 30))); await f.Save(manager); manager.Update(Set()); manager.Update(Set(Binding(1, 40))); await f.Save(manager);
        using var stored = f.Context(); Assert.Equal(40u, Assert.Single(stored.AccountKeybinding).Code00);
    }

    [Fact]
    public async Task LoadedSetsCannotOverlapAndAStaleBaselineCannotOverwriteANewerOwner()
    {
        using var f = new Fixture(options); var first = f.Manager(); var second = f.Manager(); first.Update(Set(Binding(1, 20))); second.Update(Set(Binding(1, 30)));
        await using (var context = f.Context())
        {
            first.Save(context); await using var rejected = f.Context(); Assert.Throws<InvalidOperationException>(() => second.Save(rejected));
            await context.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Save(second)); using var stored = f.Context(); Assert.Equal(20u, stored.AccountKeybinding.Single().Code00);
    }

    [Fact]
    public async Task AManagerCannotSwitchItsDatabaseAfterAnUnfinishedAttempt()
    {
        using var f = new Fixture(options); var manager = f.Manager(); manager.Update(Set(Binding(1, 20)));
        await Assert.ThrowsAsync<IOException>(() => f.Save(manager, fail: true));
        using var other = new Fixture(null); await using var context = other.Context(); Assert.Throws<InvalidOperationException>(() => manager.Save(context));
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync()); await f.Save(manager);
        using var untouched = other.Context(); Assert.Equal(10u, untouched.AccountKeybinding.Single().Code00);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ReplacedManagerOrDurableAdmissionCannotCommitOldBindings(bool admission)
    {
        using var f = new Fixture(options, epoch: admission ? 5ul : 0ul); var model = f.Load(); var owner = RecordingDispatchProxy<IAccount>.Create(out var proxy);
        proxy.SetProperty(nameof(IAccount.Id), 42u); proxy.SetProperty(nameof(IAccount.WorldSessionEpoch), model.WorldSessionEpoch);
        var manager = new AccountKeybindingManager(model, owner); proxy.SetProperty(nameof(IAccount.KeybindingManager), manager); manager.Update(Set(Binding(1, 20)));
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            manager.Save(context); await context.SaveChangesAsync();
            if (admission) await context.AccountWorldSession.ExecuteUpdateAsync(set => set.SetProperty(row => row.Epoch, 6ul));
            else proxy.SetProperty(nameof(IAccount.KeybindingManager), f.Manager());
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync()); await transaction.RollbackAsync();
        }
        if (!admission) proxy.SetProperty(nameof(IAccount.KeybindingManager), manager);
        await f.Save(manager); using var stored = f.Context(); Assert.Equal(20u, stored.AccountKeybinding.Single().Code00);
    }

    [Fact]
    public async Task SavepointRollbackRejectsPartialOuterCommitAndRetainsIntentForFreshRetry()
    {
        using var f = new Fixture(options); var manager = f.Manager(); manager.Update(Set(Binding(1, 20)));
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            await transaction.CreateSavepointAsync("owned_input"); manager.Save(context); await context.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("owned_input"); await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
            await transaction.RollbackAsync();
        }
        await f.Save(manager); using var stored = f.Context(); Assert.Equal(20u, stored.AccountKeybinding.Single().Code00);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AuthDatabaseAccountSaveRecoversBindingsAndWalletTogetherWithoutChangingDefaults(bool blocking)
    {
        using var f = new Fixture(options);
        using (var seed = f.Context()) { var model = seed.Account.Single(); AccountDefaultEntitlements.EnsureBaseline(model); seed.SaveChanges(); }
        var loaded = f.Load(); using (var read = f.Context()) loaded.AccountEntitlement = read.AccountEntitlement.AsNoTracking().ToList();
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
        tp.SetProperty(nameof(IGameTableManager.AccountCurrencyType), PlayerBindPointTests.Table(new AccountCurrencyTypeEntry { Id = (uint)AccountCurrencyType.ProtostarPromissoryNote }));
        tp.SetProperty(nameof(IGameTableManager.Entitlement), PlayerBindPointTests.Table(new EntitlementEntry { Id = 15, MaxCount = 1, Flags = 3 }, new EntitlementEntry { Id = 17, MaxCount = 1, Flags = 3 }));
        var owner = new AccountOwner(); void Property(string name, object value) => typeof(AccountOwner).GetField($"<{name}>k__BackingField", Instance)!.SetValue(owner, value);
        Property(nameof(IAccount.Id), 42u);
        Property(nameof(IAccount.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        Property(nameof(IAccount.RbacManager), RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
        Property(nameof(IAccount.GenericUnlockManager), RecordingDispatchProxy<IGenericUnlockManager>.Create(out _));
        Property(nameof(IAccount.CostumeManager), RecordingDispatchProxy<IAccountCostumeManager>.Create(out _));
        Property(nameof(IAccount.RewardRotationGrantManager), new AccountRewardRotationGrantManager(owner, loaded));
        Property(nameof(IAccount.EntitlementManager), new AccountEntitlementManager(owner, loaded, null, tables));
        Property(nameof(IAccount.InventoryManager), new AccountInventoryManager(owner, loaded, null, gameTableManager: tables));
        var wallet = new AccountCurrencyManager(owner, loaded, tables); Property(nameof(IAccount.CurrencyManager), wallet);
        var manager = new AccountKeybindingManager(loaded, owner); Property(nameof(IAccount.KeybindingManager), manager);
        manager.Update(Set(Binding(1, 20))); wallet.CurrencyAddAmount(AccountCurrencyType.ProtostarPromissoryNote, 7);
        using var configuration = f.Context(); var database = new AuthDatabase();
        database.Initialise(new DatabaseConnectionString { Provider = configuration.Database.ProviderName.Contains("MySql", StringComparison.OrdinalIgnoreCase) ? DatabaseProvider.MySql : DatabaseProvider.Sqlite,
            ConnectionString = configuration.Database.GetConnectionString() });
        void Save(AuthContext context) { owner.Save(context); context.SavingChanges += (_, _) => throw new IOException("Owned combined Auth flush failure."); }
        if (blocking) Assert.Throws<IOException>(() => database.SaveBlocking(Save)); else await Assert.ThrowsAsync<IOException>(() => database.Save(Save));
        using (var failed = f.Context()) { Assert.Equal(10u, failed.AccountKeybinding.Single().Code00); Assert.Empty(failed.AccountCurrency); }
        if (blocking) database.SaveBlocking(owner.Save); else await database.Save(owner.Save);
        using var stored = f.Context(); Assert.Equal(20u, stored.AccountKeybinding.Single().Code00); Assert.Equal(7ul, stored.AccountCurrency.Single().Amount);
        Assert.Equal(new[] { (Id: 15u, Amount: 1u), (Id: 17u, Amount: 1u) }, stored.AccountEntitlement.OrderBy(row => row.EntitlementId).Select(row => new ValueTuple<uint, uint>(row.EntitlementId, row.Amount)));
        Assert.Equal(new[] { (Amount: 0ul, Before: 0ul, After: 0ul), (Amount: 7ul, Before: 0ul, After: 7ul) },
            stored.AccountCurrencyOperation.AsEnumerable().OrderBy(row => row.Amount).Select(row => (row.Amount, row.BeforeAmount, row.AfterAmount)));
        Assert.All(stored.AccountCurrencyOperation, row => { Assert.False(row.Subtract); Assert.Equal(42u, row.AccountId); Assert.Equal((byte)AccountCurrencyType.ProtostarPromissoryNote, row.CurrencyId); });
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ExecutionStrategyRetriesDoNotReplayAnAlreadyCommittedWholeUnit(bool afterCommit)
    {
        using var f = new Fixture(options); var manager = f.Manager(); manager.Update(Set(Binding(1, 20)));
        var fault = new RetryCommitFailure(afterCommit);
        // Keep this test-only replacement out of EF's process-wide provider
        // cache. The full suite also owns other distinct retry providers.
        var services = new ServiceCollection();
        using (var provider = f.Context())
        {
            if (provider.Database.ProviderName.Contains("MySql", StringComparison.OrdinalIgnoreCase)) services.AddEntityFrameworkMySql();
            else services.AddEntityFrameworkSqlite();
        }
        // Move configured provider replacements (including the production MySQL
        // concurrency SQL generator) into the owned container. EF forbids keeping
        // ReplaceService options alongside UseInternalServiceProvider.
        var replacements = f.Options.FindExtension<CoreOptionsExtension>()?.ReplacedServices;
        if (replacements != null)
            foreach (var descriptor in services.ToArray())
                foreach (var replacement in replacements)
                    if (descriptor.ServiceType == replacement.Key.Item1
                        && (replacement.Key.Item2 == null || descriptor.ImplementationType == replacement.Key.Item2))
                        services.Replace(new ServiceDescriptor(descriptor.ServiceType, replacement.Value, descriptor.Lifetime));
        services.Replace(ServiceDescriptor.Scoped<IExecutionStrategyFactory, InputRetryFactory>());
        using var internalServices = services.BuildServiceProvider();
        var retryBuilder = new DbContextOptionsBuilder<AuthContext>();
        foreach (var extension in f.Options.Extensions.Where(value => value is not CoreOptionsExtension))
            ((IDbContextOptionsBuilderInfrastructure)retryBuilder).AddOrUpdateExtension(extension);
        var retry = retryBuilder.UseInternalServiceProvider(internalServices).AddInterceptors(fault).Options;
        await using (var context = new AuthContext(retry))
        {
            var account = await context.Account.SingleAsync(); account.GameToken = "one-unit";
            manager.Save(context); await context.SaveChangesAsync();
        }
        Assert.Equal(afterCommit ? 1 : 2, fault.Attempts);
        await f.Save(manager); using var stored = f.Context(); Assert.Equal(20u, stored.AccountKeybinding.Single().Code00); Assert.Equal("one-unit", stored.Account.Single().GameToken);
        Assert.Single(stored.AccountPendingItemDelivery); Assert.Empty(stored.AccountEntitlement);
    }

    private static BiInputKeySet Set(params NetworkBinding[] values) => new() { Bindings = values.ToList(), CharacterId = 0 };
    private static NetworkBinding Binding(ushort action, uint code) => new() { InputActionId = action, Code00 = code };
    private sealed class Fixture : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "NexusForever-AccountBindings-" + Guid.NewGuid().ToString("N"));
        private readonly DbContextOptions<AuthContext> options;
        internal DbContextOptions<AuthContext> Options => options;
        private readonly ulong epoch;
        internal Fixture(DbContextOptions<AuthContext> provider, bool existing = true, bool two = false, ulong epoch = 0)
        {
            Directory.CreateDirectory(directory); this.epoch = epoch;
            options = provider ?? new DbContextOptionsBuilder<AuthContext>().UseSqlite($"Data Source={Path.Combine(directory, "bindings.sqlite")};Pooling=False").Options;
            using var context = Context(); context.Database.EnsureCreated();
            context.Account.Add(new() { Id = 42, Email = "bindings-42@example.invalid", S = "", V = "", GameToken = "", SessionKey = "", CreateTime = DateTime.UtcNow });
            if (existing) context.AccountKeybinding.Add(new() { Id = 42, InputActionId = 1, Code00 = 10 });
            if (two) context.AccountKeybinding.Add(new() { Id = 42, InputActionId = 2, Code00 = 11 });
            if (epoch != 0) context.AccountWorldSession.Add(new() { AccountId = 42, Epoch = epoch }); context.SaveChanges();
        }
        internal AccountModel Load() { using var context = Context(); var model = context.Account.AsNoTracking().Include(row => row.AccountKeybinding).Single(); model.WorldSessionEpoch = epoch; return model; }
        internal AccountKeybindingManager Manager() => new(Load());
        internal AuthContext Context(IInterceptor interceptor = null) => new(interceptor == null ? options : new DbContextOptionsBuilder<AuthContext>(options).AddInterceptors(interceptor).Options);
        internal async Task Save(AccountKeybindingManager manager, bool fail = false)
        {
            await using var context = Context(); if (fail) context.SavingChanges += (_, _) => throw new IOException("Owned Auth input flush failure.");
            manager.Save(context); await context.SaveChangesAsync();
        }
        public void Dispose()
        {
            string path = Path.GetFullPath(directory);
            if (Path.GetDirectoryName(path) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
                || !Path.GetFileName(path).StartsWith("NexusForever-AccountBindings-", StringComparison.Ordinal)) throw new InvalidOperationException("Unsafe fixture cleanup path.");
            Directory.Delete(path, recursive: true);
        }
    }
    private sealed class SuppressedFlush : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => new(InterceptionResult<int>.SuppressWithResult(0));
    }
    private sealed class CommitFailure(bool after) : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken cancellationToken = default)
        { if (!after) throw new IOException("Owned Auth input before-commit failure."); return new(result); }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => after ? Task.FromException(new IOException("Owned Auth input lost acknowledgement.")) : Task.CompletedTask;
    }
    private sealed class InputRetryFault : Exception;
    private sealed class InputRetryFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new InputRetryStrategy(dependencies);
    }
    private sealed class InputRetryStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 1, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception error) => error is InputRetryFault;
    }
    private sealed class RetryCommitFailure(bool after) : DbTransactionInterceptor
    {
        internal int Attempts;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken cancellationToken = default)
        { if (!after && ++Attempts == 1) throw new InputRetryFault(); return new(result); }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        { if (after && ++Attempts == 1) throw new InputRetryFault(); return Task.CompletedTask; }
    }
}
