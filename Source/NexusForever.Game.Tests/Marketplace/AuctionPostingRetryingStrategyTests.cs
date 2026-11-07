using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Entity;
using static NexusForever.Game.Marketplace.GlobalMarketplaceManager;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Marketplace;

/// <summary>
/// <c>AuctionPostingCommand.ExecuteAsync</c> runs its <c>Attempt</c> through the provider execution strategy, so a transient fault replays the whole attempt. The existing posting tests have no
/// strategy and resolve a lost commit through <c>ResolveAsync</c> instead. These cases install a strategy that really replays once (SQLite, Source/SQLite evidence only; the production MySQL
/// strategy classifies transience itself) and pin that a replay yields exactly one listing, one escrowed item and one receipt, whether the fault happened before the commit or after it.
/// </summary>
public sealed class AuctionPostingRetryingStrategyTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "nf-auction-retrying-" + Guid.NewGuid().ToString("N"));
    private static int replays;
    private readonly DbContextOptions<CharacterContext> options;

    public AuctionPostingRetryingStrategyTests()
    {
        replays = 0;
        Directory.CreateDirectory(directory);
        options = new DbContextOptionsBuilder<CharacterContext>()
            .UseSqlite($"Data Source={Path.Combine(directory, "auction.sqlite")};Pooling=False;Default Timeout=10")
            .ReplaceService<IExecutionStrategyFactory, ReplayingStrategyFactory>()
            .Options;
    }

    public void Dispose()
    {
        try { Directory.Delete(directory, true); } catch (IOException) { }
    }

    [Theory]
    [InlineData(false)]    // the transaction fails before it commits: the replay commits it
    [InlineData(true)]     // the commit succeeds but its acknowledgement is lost: the replay finds the receipt
    public async Task AReplayedPostingCommitsExactlyOneListingEscrowAndReceipt(bool acknowledgementLost)
    {
        using var f = new Fixture(options); f.Seed();
        var before = new BidItem(900, 42, 7001, (ushort)InventoryLocation.Inventory, 0, 3, 2, .75f, 1200, false, "", "");
        string route;
        using (var seed = f.Context())
        {
            seed.Character.Single().AccountId = 12;
            seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
            seed.Item.Add(before.Model()); await seed.SaveChangesAsync();
            before = BidItem.Capture(await seed.Item.AsNoTracking().SingleAsync());
            route = CharacterDatabaseIdentity.Get(seed);
        }
        var listing = AuctionRow.Capture(new MarketplaceAuctionModel
        {
            Id = 1, OwnerCharacterId = 42, ItemId = before.Id, Item2Id = before.Definition, Quantity = before.Quantity,
            MinimumBid = 10, BuyoutPrice = 25, MicrochipIds = "",
            ExpirationTime = (ulong)DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        });
        var wallet = new BidWallet(42, 12, true, 100, 100, 1000);
        var fault = new CommitFault(acknowledgementLost);
        var command = new AuctionPostingCommand(() => f.Context(fault), route, Guid.NewGuid().ToString("N"), DateTime.UtcNow, listing, before, wallet);

        var result = await command.ExecuteAsync();

        Assert.True(result.Committed);
        Assert.Equal(1, fault.Fired);
        Assert.Equal(1, Volatile.Read(ref replays));     // the strategy replayed the attempt; the ResolveAsync fallback was not what committed it
        using var read = f.Context();
        Assert.Equal(before with { Owner = null, Location = (ushort)InventoryLocation.None, Bag = 0 }, BidItem.Capture(read.Item.Single()));
        Assert.Equal(listing, AuctionRow.Capture(read.MarketplaceAuction.Single()));
        Assert.Equal(1, read.MarketplaceBidReceipt.Count());
        Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
    }

    private sealed class TransientPostingException() : IOException("Injected transient posting fault.");

    private sealed class CommitFault(bool after) : DbTransactionInterceptor
    {
        private int fired;
        public int Fired => fired;

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!after && Interlocked.Exchange(ref fired, 1) == 0) throw new TransientPostingException();
            return new(result);
        }

        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => after && Interlocked.Exchange(ref fired, 1) == 0 ? Task.FromException(new TransientPostingException()) : Task.CompletedTask;
    }

    private sealed class ReplayingStrategyFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new ReplayingStrategy(dependencies);
    }

    private sealed class ReplayingStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 1, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception)
        {
            bool retry = exception is TransientPostingException;
            if (retry) Interlocked.Increment(ref replays);
            return retry;
        }
    }
}
