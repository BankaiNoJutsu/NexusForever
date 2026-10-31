using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;

namespace NexusForever.Game.Tests.Account.Currency;

public sealed partial class AccountCurrencyDurabilityTests
{
    private readonly DbContextOptions<AuthContext> providerOptions;
    public AccountCurrencyDurabilityTests() { }
    internal AccountCurrencyDurabilityTests(DbContextOptions<AuthContext> options) => providerOptions = options;

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task BothSaveEntryPointsAcknowledgeOnlyTheSettledOperation(bool synchronous)
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(3);
        await using (var context = f.Context())
        {
            f.Wallet.Save(context);
            if (synchronous) context.SaveChanges(); else await context.SaveChangesAsync();
        }
        await f.Save(); await using var read = f.Context(); Assert.Equal(103ul, read.AccountCurrency.Single().Amount); Assert.Single(read.AccountCurrencyOperation);
    }

    [Fact]
    public async Task DisposingAnUnflushedRegistrationRetainsItsOperationsForTheNextContext()
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(10);
        await using (var abandoned = f.Context()) f.Wallet.Save(abandoned);
        await using (var before = f.Context()) { Assert.Equal(100ul, before.AccountCurrency.Single().Amount); Assert.Empty(before.AccountCurrencyOperation); }
        await f.Save(); await using var read = f.Context(); Assert.Equal(110ul, read.AccountCurrency.Single().Amount); Assert.Single(read.AccountCurrencyOperation);
    }

    [Theory]
    [InlineData(1u)] [InlineData(2u)] [InlineData(3u)] [InlineData(5u)] [InlineData(6u)] [InlineData(7u)]
    [InlineData(8u)] [InlineData(9u)] [InlineData(11u)] [InlineData(12u)] [InlineData(13u)] [InlineData(14u)]
    [InlineData(15u)] [InlineData(16u)] [InlineData(17u)] [InlineData(18u)]
    public async Task EveryExistingCurrencyUsesRetainedOrderedOperationsWithoutChangingOtherOwners(uint currency)
    {
        using var f = new Fixture(providerOptions, (AccountCurrencyType)currency); f.Wallet.AddAmount(5); f.Wallet.SubtractAmount(2);
        await f.Save(); await f.Save();
        await using var read = f.Context(); Assert.Equal(103ul, read.AccountCurrency.Single().Amount); Assert.Equal(103ul, f.Wallet.Amount);
        var operations = read.AccountCurrencyOperation.ToArray(); Assert.Equal(2, operations.Length);
        Assert.All(operations, row => { Assert.Equal(77u, row.AccountId); Assert.Equal((byte)currency, row.CurrencyId); Assert.Empty(row.SourceJson); });
        Assert.Empty(read.AccountEntitlement); Assert.Empty(read.AccountInventory); Assert.Empty(read.AccountLoyalty);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CommitFailureRetainsTheSameOperationsAndFreshRetryNeverRegrantsAnUnknownCommit(bool after)
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(10);
        await using (var failed = f.Context(new CommitFailure(after)))
        {
            f.Wallet.Save(failed); await Assert.ThrowsAsync<InvalidOperationException>(() => failed.SaveChangesAsync());
        }
        await f.Save(); await f.Save();
        await using var read = f.Context(); Assert.Equal(110ul, read.AccountCurrency.Single().Amount); Assert.Single(read.AccountCurrencyOperation);
        Assert.Equal(110ul, f.Wallet.Amount);
    }

    [Fact]
    public async Task AConcurrentCompensationCreditCannotBeOverwrittenByTheActorsCachedAbsoluteBalance()
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(10);
        await using (var external = f.Context()) await external.AccountCurrency.Where(row => row.Id == 77).ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, 107ul));
        await f.Save(); Assert.Equal(117ul, f.Wallet.Amount);
        await using var read = f.Context(); Assert.Equal(117ul, read.AccountCurrency.Single().Amount);
        var operation = read.AccountCurrencyOperation.Single(); Assert.Equal(107ul, operation.BeforeAmount); Assert.Equal(117ul, operation.AfterAmount);
    }

    [Fact]
    public async Task AFailedAcknowledgementFollowedByAnotherCreditStillCannotReplayTheOriginalOperation()
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(10);
        await using (var first = f.Context(new CommitFailure(true)))
        {
            f.Wallet.Save(first); await Assert.ThrowsAsync<InvalidOperationException>(() => first.SaveChangesAsync());
        }
        await using (var external = f.Context()) await external.AccountCurrency.Where(row => row.Id == 77).ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, 117ul));
        await f.Save(); Assert.Equal(117ul, f.Wallet.Amount);
        await using var read = f.Context(); Assert.Single(read.AccountCurrencyOperation); Assert.Equal(117ul, read.AccountCurrency.Single().Amount);
    }

    [Fact]
    public async Task MutationsAfterCaptureRemainDirtyUntilTheNextSaveRatherThanBeingAcknowledgedByTheOlderUnit()
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(10);
        await using (var first = f.Context())
        {
            f.Wallet.Save(first); f.Wallet.AddAmount(5); await first.SaveChangesAsync();
        }
        await using (var middle = f.Context()) Assert.Equal(110ul, middle.AccountCurrency.Single().Amount);
        Assert.Equal(115ul, f.Wallet.Amount); await f.Save();
        await using var read = f.Context(); Assert.Equal(115ul, read.AccountCurrency.Single().Amount); Assert.Equal(2, read.AccountCurrencyOperation.Count());
    }

    [Fact]
    public async Task SameContextCanRefreshItsSnapshotBeforeStagingWithoutDuplicatingAnOperation()
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(10);
        await using var context = f.Context(); f.Wallet.Save(context); f.Wallet.AddAmount(5); f.Wallet.Save(context); await context.SaveChangesAsync();
        Assert.Equal(115ul, context.AccountCurrency.AsNoTracking().Single().Amount); Assert.Equal(2, context.AccountCurrencyOperation.Count());
    }

    [Fact]
    public async Task AnotherPendingContextCannotAcknowledgeOrPartiallySaveTheFirstOwnersOperations()
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(10);
        await using (var first = f.Context())
        {
            f.Wallet.Save(first);
            await using var foreign = f.Context(); Assert.Throws<InvalidOperationException>(() => f.Wallet.Save(foreign));
            await Assert.ThrowsAsync<InvalidOperationException>(() => foreign.SaveChangesAsync());
        }
        await f.Save(); await using var read = f.Context(); Assert.Equal(110ul, read.AccountCurrency.Single().Amount); Assert.Single(read.AccountCurrencyOperation);
    }

    [Fact]
    public async Task PostStageSnapshotReplacementRejectsTheWholeExplicitUnitAndRetainsLaterIntent()
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(10);
        await using (var context = f.Context())
        await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            f.Wallet.Save(context); await context.SaveChangesAsync(); f.Wallet.AddAmount(5);
            Assert.Throws<InvalidOperationException>(() => f.Wallet.Save(context)); await Assert.ThrowsAsync<InvalidOperationException>(() => tx.CommitAsync());
            await tx.RollbackAsync();
        }
        await f.Save(); await using var read = f.Context(); Assert.Equal(115ul, read.AccountCurrency.Single().Amount); Assert.Equal(2, read.AccountCurrencyOperation.Count());
    }

    [Theory]
    [InlineData("wallet")] [InlineData("receipt")] [InlineData("delete-wallet")] [InlineData("delete-receipt")] [InlineData("tracked")]
    [InlineData("original-wallet")] [InlineData("original-receipt")]
    public async Task SqlOrTrackedSuppressionCannotCommitAnOtherwiseAcknowledgedWalletOperation(string fault)
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(10);
        await using (var context = f.Context())
        await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            f.Wallet.Save(context); await context.SaveChangesAsync();
            if (fault == "wallet") await context.AccountCurrency.Where(row => row.Id == 77).ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, 100ul));
            if (fault == "receipt") await context.AccountCurrencyOperation.Where(row => row.AccountId == 77).ExecuteUpdateAsync(set => set.SetProperty(row => row.SourceJson, "changed"));
            if (fault == "delete-wallet") await context.AccountCurrency.Where(row => row.Id == 77).ExecuteDeleteAsync();
            if (fault == "delete-receipt") await context.AccountCurrencyOperation.Where(row => row.AccountId == 77).ExecuteDeleteAsync();
            if (fault == "tracked") context.AccountCurrencyOperation.Local.Single().Amount++;
            if (fault == "original-wallet") Assert.Throws<InvalidOperationException>(() =>
                context.Entry(context.AccountCurrency.Local.Single()).OriginalValues[nameof(AccountCurrencyModel.Id)] = 78u);
            if (fault == "original-receipt") Assert.Throws<InvalidOperationException>(() =>
                context.Entry(context.AccountCurrencyOperation.Local.Single()).OriginalValues[nameof(AccountCurrencyOperationModel.OperationKey)] = "changed-original");
            await Assert.ThrowsAsync<InvalidOperationException>(() => tx.CommitAsync()); await tx.RollbackAsync();
        }
        await f.Save(); await using var read = f.Context(); Assert.Equal(110ul, read.AccountCurrency.Single().Amount); Assert.Single(read.AccountCurrencyOperation);
    }

    [Fact]
    public async Task AnExternalSpendCannotWrapAnActorDebitOrEraseItsDirtyOperation()
    {
        using var f = new Fixture(providerOptions); f.Wallet.SubtractAmount(90);
        await using (var external = f.Context()) await external.AccountCurrency.Where(row => row.Id == 77).ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, 80ul));
        await Assert.ThrowsAsync<OverflowException>(() => f.Save());
        await using (var middle = f.Context()) { Assert.Equal(80ul, middle.AccountCurrency.Single().Amount); Assert.Empty(middle.AccountCurrencyOperation); }
        await using (var external = f.Context()) await external.AccountCurrency.Where(row => row.Id == 77).ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, 100ul));
        await f.Save(); await using var read = f.Context(); Assert.Equal(10ul, read.AccountCurrency.Single().Amount); Assert.Single(read.AccountCurrencyOperation);
    }

    [Fact]
    public async Task ASpendAfterTheFinalGuardCannotOverflowReconciliationOrPreventOtherOwnersSettling()
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(5);
        await using (var external = f.Context()) await external.AccountCurrency.Where(row => row.Id == 77).ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, 0ul));
        bool settled = false;
        await using (var context = f.Context(new AfterCommit(() => f.Wallet.SubtractAmount(50))))
        {
            f.Wallet.Save(context); context.EnlistSaveWork(_ => { }, committed => settled = committed); await context.SaveChangesAsync();
        }
        Assert.True(settled); Assert.Equal(5ul, f.Wallet.Amount);
        Assert.False(f.Wallet.CanAfford(1));
        Assert.Throws<InvalidOperationException>(() => f.Wallet.AddAmount(1));
        await using var blocked = f.Context(); Assert.Throws<InvalidOperationException>(() => f.Wallet.Save(blocked));
        Assert.Single(f.SessionProxy.GetInvocations(nameof(IGameSession.ForceDisconnect)));
    }

    [Fact]
    public async Task NewWalletCreationAndRollbackUseTheSameZeroAndCreditOperations()
    {
        using var f = new Fixture(providerOptions, existing: false); f.Wallet.AddAmount(7);
        await using (var failed = f.Context(new CommitFailure(false)))
        {
            f.Wallet.Save(failed); await Assert.ThrowsAsync<InvalidOperationException>(() => failed.SaveChangesAsync());
        }
        await f.Save(); await f.Save(); await using var read = f.Context();
        Assert.Equal(7ul, read.AccountCurrency.Single().Amount); Assert.Equal(2, read.AccountCurrencyOperation.Count());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TheSameGlobalOperationCannotBeReboundToAnotherAccountOrCurrency(bool otherAccount)
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(10); await f.Save();
        AccountCurrencyOperationModel prior;
        await using (var read = f.Context())
        {
            prior = read.AccountCurrencyOperation.Single();
            if (otherAccount) { read.Account.Add(new() { Id = 78, Email = "wallet-78@example.local" }); await read.SaveChangesAsync(); }
        }
        await using var context = f.Context();
        var operation = new AccountWalletOperation(prior.OperationKey, otherAccount ? 78u : 77u,
            otherAccount ? AccountCurrencyType.ProtostarPromissoryNote : AccountCurrencyType.ServiceToken, prior.Amount, prior.Subtract);
        AccountCurrencyOperationRepository repository = null;
        context.EnlistSaveWork(unit => { repository = new(unit, operation.AccountId, operation.Currency, [operation]); repository.Stage(); }, _ => { });
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        await using var verify = f.Context(); Assert.Equal(110ul, verify.AccountCurrency.Single().Amount); Assert.Single(verify.AccountCurrencyOperation);
    }

    [Fact]
    public async Task RetainedReceiptsSurviveAccountDeletionAndCannotCreditARecreatedOwnerAgain()
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(10); await f.Save(); AccountCurrencyOperationModel row;
        await using (var context = f.Context())
        {
            row = context.AccountCurrencyOperation.Single(); await context.Account.Where(owner => owner.Id == 77).ExecuteDeleteAsync();
            Assert.Single(context.AccountCurrencyOperation); context.Account.Add(new() { Id = 77, Email = "wallet-recreated@example.local" }); await context.SaveChangesAsync();
        }
        await using var retry = f.Context(); AccountCurrencyOperationRepository repository = null;
        var operation = new AccountWalletOperation(row.OperationKey, 77, (AccountCurrencyType)row.CurrencyId, row.Amount, row.Subtract, row.SourceJson);
        retry.EnlistSaveWork(unit => { repository = new(unit, 77, operation.Currency, [operation]); repository.Stage(); }, _ => { }, () => repository.CanCommit());
        await retry.SaveChangesAsync(); Assert.Equal(0ul, retry.AccountCurrency.AsNoTracking().Single().Amount); Assert.Single(retry.AccountCurrencyOperation);
    }

    [Fact]
    public void CosmicCurrencyRetainsTheExistingLoyaltyJournalInsteadOfEnlistingAnOrdinaryWalletSave()
    {
        using var f = new Fixture(providerOptions, AccountCurrencyType.CosmicReward);
        var loyalty = RecordingDispatchProxy<IAccountLoyaltyManager>.Create(out var proxy); f.AccountProxy.SetProperty(nameof(IAccount.LoyaltyManager), loyalty);
        using var context = f.Context(); f.Wallet.AddAmount(10); f.Wallet.Save(context);
        Assert.Single(proxy.GetInvocations(nameof(IAccountLoyaltyManager.Save))); Assert.Empty(context.ChangeTracker.Entries());
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "NexusForever-AccountWallet-" + Guid.NewGuid().ToString("N"));
        private readonly DbContextOptions<AuthContext> options;
        internal AccountCurrency Wallet { get; }
        internal RecordingDispatchProxy<IAccount> AccountProxy { get; }
        internal RecordingDispatchProxy<IGameSession> SessionProxy { get; }
        internal IAccount Account { get; }
        internal IGameTableManager Tables { get; }
        internal Fixture(DbContextOptions<AuthContext> provider, AccountCurrencyType currency = AccountCurrencyType.ProtostarPromissoryNote, bool existing = true)
        {
            Directory.CreateDirectory(directory);
            options = provider ?? new DbContextOptionsBuilder<AuthContext>().UseSqlite($"Data Source={Path.Combine(directory, "wallet.sqlite")};Pooling=False").Options;
            var account = RecordingDispatchProxy<IAccount>.Create(out var ap); AccountProxy = ap; ap.SetProperty(nameof(IAccount.Id), 77u);
            Account = account;
            var session = RecordingDispatchProxy<IGameSession>.Create(out var sp); SessionProxy = sp; ap.SetProperty(nameof(IAccount.Session), session);
            var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tablesProxy);
            Tables = tables;
            tablesProxy.SetProperty(nameof(IGameTableManager.AccountCurrencyType), PlayerBindPointTests.Table(new AccountCurrencyTypeEntry { Id = (uint)currency }));
            using var context = Context(); context.Database.EnsureCreated(); context.Account.Add(new() { Id = 77, Email = "wallet-77@example.local" });
            if (existing) context.AccountCurrency.Add(new() { Id = 77, CurrencyId = (byte)currency, Amount = 100 });
            context.SaveChanges(); Wallet = existing ? new(account, new AccountCurrencyModel { Id = 77, CurrencyId = (byte)currency, Amount = 100 }, tables)
                : new(account, currency, 0, tables);
        }
        internal AuthContext Context(IInterceptor interceptor = null) => interceptor == null ? new(options)
            : new(new DbContextOptionsBuilder<AuthContext>(options).AddInterceptors(interceptor).Options);
        internal async Task Save() { await using var context = Context(); Wallet.Save(context); await context.SaveChangesAsync(); }
        public void Dispose()
        {
            string path = Path.GetFullPath(directory);
            if (Path.GetDirectoryName(path) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
                || !Path.GetFileName(path).StartsWith("NexusForever-AccountWallet-", StringComparison.Ordinal)) throw new InvalidOperationException("Unsafe fixture cleanup path.");
            Directory.Delete(path, recursive: true);
        }
    }

    private sealed class CommitFailure(bool after) : DbTransactionInterceptor
    {
        public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result)
        { if (!after) throw new InvalidOperationException("Owned before-commit failure."); return result; }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default) => new(TransactionCommitting(transaction, eventData, result));
        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        { if (after) throw new InvalidOperationException("Owned lost acknowledgement."); }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        { TransactionCommitted(transaction, eventData); return Task.CompletedTask; }
    }
    private sealed class AfterCommit(Action action) : DbTransactionInterceptor
    {
        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData) => action();
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        { action(); return Task.CompletedTask; }
    }
}
