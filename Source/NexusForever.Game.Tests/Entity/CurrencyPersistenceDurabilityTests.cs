using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using Fixture = NexusForever.Game.Tests.Challenges.CharacterChallengeRewardRepositoryTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class CurrencyPersistenceDurabilityTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public CurrencyPersistenceDurabilityTests() { }
    internal CurrencyPersistenceDurabilityTests(DbContextOptions<CharacterContext> options) => this.options = options;
    private static Currency Wallet(Fixture f, bool create = false)
    {
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        var entry = new CurrencyTypeEntry { Id = 1 };
        proxy.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(entry));
        if (create) return new(42, entry) { Amount = 125 };
        using var seed = f.Context(); seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 }); seed.SaveChanges();
        return new(new CharacterCurrencyModel { Id = 42, CurrencyId = 1, Amount = 100 }, tables) { Amount = 125 };
    }
    private static ulong Stored(Fixture f) { using var read = f.Context(); return read.CharacterCurrency.Single().Amount; }

    [Theory]
    [InlineData(false, "dispose")] [InlineData(false, "rollback")] [InlineData(false, "failure")] [InlineData(false, "suppressed")]
    [InlineData(true, "dispose")] [InlineData(true, "rollback")] [InlineData(true, "failure")] [InlineData(true, "suppressed")]
    public async Task ACreateOrUpdateRetainsItsCompleteIntentUntilTerminalCommit(bool create, string mode)
    {
        using var f = new Fixture(options); var wallet = Wallet(f, create);
        await using (var context = f.Intercepted(new FlushFault(mode)))
        {
            wallet.Save(context); Assert.True(wallet.NeedsPersistenceSave);
            if (mode == "rollback") { await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable); await context.SaveChangesAsync(); Assert.True(wallet.NeedsPersistenceSave); await tx.RollbackAsync(); }
            else if (mode == "failure") await Assert.ThrowsAsync<IOException>(() => context.SaveChangesAsync());
            else if (mode == "suppressed") Assert.Equal(0, await context.SaveChangesAsync());
        }
        Assert.True(wallet.NeedsPersistenceSave); using (var read = f.Context()) Assert.Equal(create ? 0 : 1, read.CharacterCurrency.Count());
        await CharacterAtomicSave.Execute(f.Context, 42, wallet.Save);
        Assert.False(wallet.NeedsPersistenceSave); Assert.Equal(125ul, Stored(f));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AFlushedWalletRemainsOwnedUntilCommitAndOnlyTheCapturedAmountIsAcknowledged(bool create)
    {
        using var f = new Fixture(options); var wallet = Wallet(f, create);
        await using (var context = f.Context()) await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            wallet.Save(context); await context.SaveChangesAsync(); Assert.True(wallet.NeedsPersistenceSave); wallet.Amount = 132;
            using var foreign = f.Context(); Assert.Throws<InvalidOperationException>(() => wallet.Save(foreign));
            await tx.CommitAsync(); Assert.True(wallet.NeedsPersistenceSave);
        }
        Assert.Equal(125ul, Stored(f)); await CharacterAtomicSave.Execute(f.Context, 42, wallet.Save);
        Assert.Equal(132ul, Stored(f)); Assert.False(wallet.NeedsPersistenceSave);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ABeforeOrAfterCommitFaultUsesFreshSqlCandidatesWithoutRepeatingOrDroppingTheWallet(bool after)
    {
        using var f = new Fixture(options); var wallet = Wallet(f);
        await Assert.ThrowsAsync<IOException>(() => CharacterAtomicSave.Execute(() => f.Intercepted(new CommitFault(after)), 42, wallet.Save));
        Assert.Equal(after ? 125ul : 100ul, Stored(f)); Assert.True(wallet.NeedsPersistenceSave);
        wallet.Amount = 132; await CharacterAtomicSave.Execute(f.Context, 42, wallet.Save);
        Assert.Equal(132ul, Stored(f)); Assert.False(wallet.NeedsPersistenceSave);
    }

    [Theory]
    [InlineData("amount")] [InlineData("detach")] [InlineData("unchanged")] [InlineData("original")]
    public async Task ARewrittenOrSuppressedTrackedWalletCannotCommit(string mode)
    {
        using var f = new Fixture(options); var wallet = Wallet(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(f.Context, 42, context =>
        {
            wallet.Save(context); var entry = context.Entry(context.CharacterCurrency.Local.Single());
            if (mode == "amount") entry.Entity.Amount = 900;
            if (mode == "detach") entry.State = EntityState.Detached;
            if (mode == "unchanged") entry.State = EntityState.Unchanged;
            if (mode == "original") entry.OriginalValues[nameof(CharacterCurrencyModel.Amount)] = 99ul;
        }));
        Assert.Equal(100ul, Stored(f)); Assert.True(wallet.NeedsPersistenceSave);
        await CharacterAtomicSave.Execute(f.Context, 42, wallet.Save); Assert.Equal(125ul, Stored(f));
    }

    [Fact]
    public async Task APostFlushSqlRewriteRollsBackTheWholeWalletUnit()
    {
        using var f = new Fixture(options); var wallet = Wallet(f);
        await using (var context = f.Context()) await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            wallet.Save(context); await context.SaveChangesAsync();
            await context.CharacterCurrency.ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, 900ul));
            await Assert.ThrowsAsync<InvalidOperationException>(() => tx.CommitAsync()); await tx.RollbackAsync();
        }
        Assert.Equal(100ul, Stored(f)); await CharacterAtomicSave.Execute(f.Context, 42, wallet.Save); Assert.Equal(125ul, Stored(f));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AFrozenWalletUsesItsCapturedValueAndRetainsLaterIntent(bool create)
    {
        using var f = new Fixture(options); var wallet = Wallet(f, create); var token = new object();
        using var identity = f.Context(); using var scope = QuestSaveScope.AcquireFrozen(wallet, token, NexusForever.Game.Entity.Item.GetRewardDatabaseKey(identity), 42);
        var save = wallet.CaptureNormalSave(token); wallet.Amount = 132;
        using (var escaped = f.Context()) { Assert.Throws<InvalidOperationException>(() => wallet.Save(escaped)); Assert.True(escaped.IsSaveRejected); }
        await CharacterAtomicSave.Execute(f.Context, 42, save); Assert.Equal(125ul, Stored(f)); Assert.True(wallet.NeedsPersistenceSave);
        scope.Dispose(); await CharacterAtomicSave.Execute(f.Context, 42, wallet.Save); Assert.Equal(132ul, Stored(f)); Assert.False(wallet.NeedsPersistenceSave);
    }

    [Fact]
    public async Task AForeignSqlBalanceCannotBeOverwrittenByTheLoadedWallet()
    {
        using var f = new Fixture(options); var wallet = Wallet(f);
        using (var external = f.Context()) await external.CharacterCurrency.ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, 107ul));
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(f.Context, 42, wallet.Save));
        Assert.Equal(107ul, Stored(f)); Assert.True(wallet.NeedsPersistenceSave);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LoadedIdentityChangesCannotRedirectTheCurrencyWrite(bool changeCurrency)
    {
        using var f = new Fixture(options); var wallet = Wallet(f);
        if (changeCurrency) wallet.Entry = new() { Id = 2 }; else wallet.CharacterId = 43;
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(f.Context, 42, wallet.Save));
        Assert.Equal(100ul, Stored(f));
    }

    private sealed class FlushFault(string mode) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => mode == "failure" ? throw new IOException("Owned currency flush fault.") : new(mode == "suppressed" ? InterceptionResult<int>.SuppressWithResult(0) : result);
    }
    private sealed class CommitFault(bool after) : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken cancellationToken = default)
            => after ? new(result) : throw new IOException("Owned currency pre-commit fault.");
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => after ? Task.FromException(new IOException("Owned currency acknowledgement loss.")) : Task.CompletedTask;
    }
}
