using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Entity;
using static NexusForever.Game.Marketplace.GlobalMarketplaceManager;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Marketplace;

public sealed class AuctionPostingCommandTests
{
    [Theory]
    [InlineData("commit")]
    [InlineData("lost")]
    [InlineData("rollback")]
    [InlineData("quantity")]
    [InlineData("existing-listing")]
    [InlineData("expired")]
    [InlineData("receipt")]
    [InlineData("context")]
    public async Task WholeEscrowAndNewListingHaveOneImmutableReceiptOutcome(string fault)
    {
        using var f = new Fixture(null); f.Seed();
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
        var listing = Listing(before, fault == "expired");
        var wallet = new BidWallet(42, 12, true, 100, 100, 1000);
        string id = Guid.NewGuid().ToString("N");
        var interceptor = new CommitFault(fault == "lost");
        using var reused = fault == "context" ? f.Context() : null;
        CharacterContext Context() => reused ?? (fault is "lost" or "rollback" ? f.Context(interceptor) : f.Context());
        var command = new AuctionPostingCommand(Context, route, id, DateTime.UtcNow, listing, before, wallet);
        using (var changed = f.Context())
        {
            if (fault == "quantity") changed.Item.Single().StackCount = 4;
            if (fault == "existing-listing") changed.MarketplaceAuction.Add((listing with { Id = 5 }).Model());
            if (fault == "receipt") changed.MarketplaceBidReceipt.Add(new() { Id = id, Payload = "another captured command", CreatedUtc = DateTime.UtcNow });
            await changed.SaveChangesAsync();
        }
        if (fault is "receipt" or "context")
        {
            await Assert.ThrowsAsync<InvalidOperationException>(command.ExecuteAsync);
            using var refused = f.Context(); Assert.Empty(refused.MarketplaceAuction);
            Assert.Equal(before, BidItem.Capture(refused.Item.Single())); return;
        }
        bool commit = fault is "commit" or "lost";
        var result = await command.ExecuteAsync();
        Assert.Equal(commit, result.Committed);
        Assert.Equal(commit, (await command.ResolveAsync()).Committed);
        using var read = f.Context();
        Assert.Equal(commit ? before with { Owner = null, Location = (ushort)InventoryLocation.None, Bag = 0 }
            : fault == "quantity" ? before with { Quantity = 4 } : before, BidItem.Capture(read.Item.Single()));
        Assert.Equal(commit ? listing : null, AuctionRow.Capture(read.MarketplaceAuction.SingleOrDefault(row => row.Id == 1)));
        Assert.Equal(commit ? 1 : 0, read.MarketplaceBidReceipt.Count());
        Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
        Assert.Empty(read.CharacterMail); Assert.Empty(read.CharacterMailAttachment);
        if (commit)
        {
            var view = await command.ReadReconciliationAsync(); Assert.True(view.CustodyMatches);
            Assert.Equal(listing, view.Auction); Assert.Equal(100ul, view.Wallet.Amount);
            await Assert.ThrowsAsync<InvalidOperationException>(command.ExecuteAsync);
        }
    }

    [Theory]
    [InlineData("debit")]
    [InlineData("owner")]
    [InlineData("quantity")]
    [InlineData("bound")]
    public void ConstructorRejectsChangedWholeItemAndNoDebitRoles(string fault)
    {
        var before = new BidItem(900, 42, 7001, (ushort)InventoryLocation.Inventory, 0, 3, 2, .75f, 1200, false, "", "");
        var listing = Listing(before, false); var wallet = new BidWallet(42, 12, true, 100, 100, 1000);
        if (fault == "debit") wallet = wallet with { After = 90 };
        if (fault == "owner") listing = listing with { Owner = 100 };
        if (fault == "quantity") listing = listing with { Quantity = 2 };
        if (fault == "bound") before = before with { Bound = true };
        Assert.Throws<InvalidOperationException>(() => new AuctionPostingCommand(() => throw new Exception("No SQL expected."),
            "captured route", Guid.NewGuid().ToString("N"), DateTime.UtcNow, listing, before, wallet));
    }

    private static AuctionRow Listing(BidItem item, bool expired) => AuctionRow.Capture(new MarketplaceAuctionModel
    {
        Id = 1, OwnerCharacterId = 42, ItemId = item.Id, Item2Id = item.Definition, Quantity = item.Quantity,
        MinimumBid = 10, BuyoutPrice = 25, MicrochipIds = "",
        ExpirationTime = (ulong)DateTimeOffset.UtcNow.AddHours(expired ? -1 : 1).ToUnixTimeSeconds()
    });

    private sealed class CommitFault(bool after) : DbTransactionInterceptor
    {
        private int fired;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!after && Interlocked.Exchange(ref fired, 1) == 0) throw new IOException("Whole auction posting rolled back.");
            return new(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => after && Interlocked.Exchange(ref fired, 1) == 0 ? Task.FromException(new IOException("Whole auction posting acknowledgement lost.")) : Task.CompletedTask;
    }
}
