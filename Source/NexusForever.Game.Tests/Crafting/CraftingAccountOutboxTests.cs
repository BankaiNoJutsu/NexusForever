using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Crafting;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Loot;

namespace NexusForever.Game.Tests.Crafting;

public sealed class CraftingAccountOutboxTests
{
    private readonly DbContextOptions<AuthContext> providerAuth;
    private readonly DbContextOptions<CharacterContext> providerCharacters;
    public CraftingAccountOutboxTests() { }
    internal CraftingAccountOutboxTests(DbContextOptions<AuthContext> auth, DbContextOptions<CharacterContext> characters)
    { providerAuth = auth; providerCharacters = characters; }

    [Fact]
    public async Task BoundedPassesSkipUnpaidAndMalformedRowsWithoutStarvingTheNextPaidAward()
    {
        using var f = new Fixture(providerAuth, providerCharacters);
        var unpaid = await f.Queue(1, paid: false);
        var broken = await f.Queue(2, paid: true);
        var paid = await f.Queue(3, paid: true);
        var foreignRealm = await f.Queue(4, paid: true, realm: 2);
        using (var corrupt = f.Auth())
        { corrupt.AccountCraftingReceipt.Single(row => row.Id == broken.Id).PayloadJson = "{"; await corrupt.SaveChangesAsync(); }
        var worker = new CraftingAccountOutbox(() => f.Character(), () => f.Auth(), 1, null);
        Assert.Equal(0, await worker.DrainAsync(1));
        Assert.Equal(0, await worker.DrainAsync(1));
        Assert.Equal(1, await worker.DrainAsync(1));
        Assert.Equal(0, await worker.DrainAsync(1));
        using var auth = f.Auth(); using var characters = f.Character();
        var untouched = auth.AccountCraftingReceipt.Single(row => row.Id == unpaid.Id);
        Assert.False(untouched.Cancelled); Assert.False(untouched.Delivered); Assert.False(untouched.Completed);
        Assert.DoesNotContain(characters.CharacterCraftingAccountOutbox, row => row.Id == unpaid.Id);
        Assert.True(auth.AccountCraftingReceipt.Single(row => row.Id == paid.Id).Completed);
        Assert.False(auth.AccountCraftingReceipt.Single(row => row.Id == foreignRealm.Id).Delivered);
        Assert.False(auth.AccountCraftingReceipt.Single(row => row.Id == broken.Id).Delivered);
        Assert.Equal(7ul, auth.AccountCurrency.Single().Amount); Assert.Single(auth.AccountInventory);
        Assert.Empty(auth.AccountEntitlement); Assert.Empty(characters.Character);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RestartCompletesInterruptedAcknowledgementWithoutRegrantingConsumedAwards(bool authAcknowledgement)
    {
        using var f = new Fixture(providerAuth, providerCharacters);
        var intent = await f.Queue(1, paid: true);
        var fault = new AcknowledgementFault(authAcknowledgement);
        var interrupted = new CraftingAccountOutbox(() => f.Character(fault), () => f.Auth(fault), 1, null);
        Assert.Equal(0, await interrupted.DrainAsync()); Assert.True(fault.Fired);
        using (var delivered = f.Auth())
        {
            var receipt = delivered.AccountCraftingReceipt.Single(); Assert.True(receipt.Delivered); Assert.False(receipt.Completed);
            Assert.Equal(7ul, delivered.AccountCurrency.Single().Amount); Assert.Single(delivered.AccountInventory);
            await delivered.AccountInventory.ExecuteDeleteAsync();
            await delivered.AccountCurrency.ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, 2ul));
        }
        using (var source = f.Character())
            Assert.Equal(authAcknowledgement, source.CharacterCraftingAccountOutbox.Single().Delivered);
        var restarted = new CraftingAccountOutbox(() => f.Character(), () => f.Auth(), 1, null);
        Assert.Equal(1, await restarted.DrainAsync());
        Assert.Equal(0, await restarted.DrainAsync());
        using var auth = f.Auth(); using var characters = f.Character();
        Assert.True(auth.AccountCraftingReceipt.Single(row => row.Id == intent.Id).Completed);
        Assert.True(characters.CharacterCraftingAccountOutbox.Single().Delivered);
        Assert.Equal(2ul, auth.AccountCurrency.Single().Amount); Assert.Empty(auth.AccountInventory);
        Assert.Single(auth.AccountCurrencyOperation); Assert.Single(auth.AccountInventoryReceipt); Assert.Empty(auth.AccountEntitlement);
    }

    [Fact]
    public async Task ExistingAdmissionFenceCanFinishItsAuthAcknowledgementWithoutCreatingAnAward()
    {
        using var f = new Fixture(providerAuth, providerCharacters);
        var intent = await f.Queue(1, paid: true, cancelled: true);
        Assert.Equal(1, await new CraftingAccountOutbox(() => f.Character(), () => f.Auth(), 1, null).DrainAsync());
        using var auth = f.Auth(); using var characters = f.Character();
        var receipt = auth.AccountCraftingReceipt.Single(row => row.Id == intent.Id);
        Assert.True(receipt.Cancelled); Assert.True(receipt.Completed); Assert.False(receipt.Delivered);
        Assert.True(characters.CharacterCraftingAccountOutbox.Single().Cancelled);
        Assert.Empty(auth.AccountCurrency); Assert.Empty(auth.AccountInventory); Assert.Empty(auth.AccountEntitlement);
    }

    [Fact]
    public async Task IndependentWorkersDeliverOnePaidBundleOnce()
    {
        using var f = new Fixture(providerAuth, providerCharacters);
        await f.Queue(1, paid: true);
        var first = new CraftingAccountOutbox(() => f.Character(), () => f.Auth(), 1, null);
        var second = new CraftingAccountOutbox(() => f.Character(), () => f.Auth(), 1, null);
        await Task.WhenAll(Task.Run(() => first.DrainAsync()), Task.Run(() => second.DrainAsync()));
        using var auth = f.Auth(); using var characters = f.Character();
        Assert.Equal(7ul, auth.AccountCurrency.Single().Amount); Assert.Single(auth.AccountInventory);
        Assert.Single(auth.AccountCurrencyOperation); Assert.Single(auth.AccountInventoryReceipt);
        Assert.True(auth.AccountCraftingReceipt.Single().Completed);
        Assert.True(characters.CharacterCraftingAccountOutbox.Single().Delivered);
        Assert.Empty(auth.AccountEntitlement);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReconfiguredOwnedFactoriesCannotDeliverToAnotherDatabase(bool authRoute)
    {
        using var original = new Fixture(providerAuth, providerCharacters);
        using var foreign = new Fixture(null, null);
        var intent = await original.Queue(1, paid: false);
        bool replaced = false;
        var worker = new CraftingAccountOutbox(
            () => replaced && !authRoute ? foreign.Character() : original.Character(),
            () => replaced && authRoute ? foreign.Auth() : original.Auth(), 1, null);
        Assert.Equal(0, await worker.DrainAsync());
        using (var character = original.Character())
        { character.CharacterCraftingAccountOutbox.Add(intent.Model()); await character.SaveChangesAsync(); }
        if (authRoute)
        {
            using var auth = foreign.Auth(); auth.AccountCraftingReceipt.Add(intent.Reservation()); await auth.SaveChangesAsync();
        }
        else
        {
            using var character = foreign.Character(); character.CharacterCraftingAccountOutbox.Add(intent.Model()); await character.SaveChangesAsync();
        }
        replaced = true;
        if (authRoute) await Assert.ThrowsAsync<InvalidOperationException>(() => worker.DrainAsync());
        else Assert.Equal(0, await worker.DrainAsync());
        using var originalAuth = original.Auth(); using var foreignAuth = foreign.Auth();
        Assert.False(originalAuth.AccountCraftingReceipt.Single().Delivered);
        Assert.Empty(originalAuth.AccountCurrency); Assert.Empty(originalAuth.AccountInventory);
        Assert.Empty(foreignAuth.AccountCurrency); Assert.Empty(foreignAuth.AccountInventory);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), $"nf-crafting-outbox-{Guid.NewGuid():N}");
        private readonly DbContextOptions<AuthContext> auth;
        private readonly DbContextOptions<CharacterContext> characters;
        internal Fixture(DbContextOptions<AuthContext> auth, DbContextOptions<CharacterContext> characters)
        {
            Directory.CreateDirectory(directory);
            this.auth = auth ?? new DbContextOptionsBuilder<AuthContext>()
                .UseSqlite($"Data Source={Path.Combine(directory, "auth.sqlite")};Pooling=False")
                .ReplaceService<IExecutionStrategyFactory, RetryFactory>().Options;
            this.characters = characters ?? new DbContextOptionsBuilder<CharacterContext>()
                .UseSqlite($"Data Source={Path.Combine(directory, "character.sqlite")};Pooling=False")
                .ReplaceService<IExecutionStrategyFactory, RetryFactory>().Options;
            using var source = Character(); source.Database.EnsureCreated();
            using var account = Auth(); account.Database.EnsureCreated();
            account.Account.Add(new() { Id = 12, Email = "outbox@example.test" });
            account.AccountWorldSession.Add(new() { AccountId = 12, Epoch = 1 }); account.SaveChanges();
        }
        internal AuthContext Auth(params IInterceptor[] interceptors)
            => new(new DbContextOptionsBuilder<AuthContext>(auth).AddInterceptors(interceptors)
                .ConfigureWarnings(warnings => warnings.Log(CoreEventId.ManyServiceProvidersCreatedWarning)).Options);
        internal CharacterContext Character(params IInterceptor[] interceptors)
            => new(new DbContextOptionsBuilder<CharacterContext>(characters).AddInterceptors(interceptors)
                .ConfigureWarnings(warnings => warnings.Log(CoreEventId.ManyServiceProvidersCreatedWarning)).Options);
        internal async Task<CraftingAccountIntent> Queue(int ordinal, bool paid, bool cancelled = false, ushort realm = 1)
        {
            using var authContext = Auth(); using var character = Character();
            var intent = new CraftingAccountIntent(1, ordinal.ToString("x32"), 12, 42, realm,
                NexusForever.Game.Entity.Item.GetRewardDatabaseKey(character), AccountCurrencyOperationRepository.DatabaseKey(authContext),
                [new(LootItemType.AccountCurrency, 14, 7), new(LootItemType.AccountItem, 1101, 1)], 0, 1, null, []);
            intent = intent.Reserve(new FixtureAuth(this));
            if (paid)
            {
                var row = intent.Model(); row.Cancelled = cancelled;
                character.CharacterCraftingAccountOutbox.Add(row); await character.SaveChangesAsync();
            }
            return intent;
        }
        private sealed class FixtureAuth(Fixture fixture) : AuthDatabase
        { public override AuthContext CreateContext() => fixture.Auth(); }
        public void Dispose() => Directory.Delete(directory, true);
    }
    private sealed class RetryFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    { public IExecutionStrategy Create() => new Retry(dependencies); }
    private sealed class Retry(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 2, TimeSpan.Zero)
    { protected override bool ShouldRetryOn(Exception error) => error is TimeoutException; }
    private sealed class AcknowledgementFault(bool auth) : DbTransactionInterceptor
    {
        internal bool Fired;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            bool acknowledgement = auth
                ? data.Context is AuthContext a && a.ChangeTracker.Entries<AccountCraftingReceiptModel>().Any(row => row.Entity.Completed)
                : data.Context is CharacterContext c && c.ChangeTracker.Entries<NexusForever.Database.Character.Model.CharacterCraftingAccountOutboxModel>().Any(row => row.Entity.Delivered);
            if (!Fired && acknowledgement) { Fired = true; throw new IOException("Owned outbox acknowledgement rejection."); }
            return new(result);
        }
    }
}
