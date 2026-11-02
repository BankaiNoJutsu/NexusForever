using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Mail;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using static NexusForever.Game.Marketplace.GlobalMarketplaceManager;

namespace NexusForever.Game.Tests.Marketplace;

public sealed class NormalAuctionBidCommandTests
{
    [Theory]
    [InlineData(true, true, false, false, false)] [InlineData(true, false, true, false, false)]
    [InlineData(true, true, false, false, true)] [InlineData(false, true, false, false, false)]
    [InlineData(false, false, true, false, false)] [InlineData(false, true, false, true, false)]
    [InlineData(false, true, false, true, true)]
    public async Task CommodityReturnsCommitRefundOrCompleteItemMaterialDelivery(bool buy, bool online, bool mailed, bool satchel, bool lost)
    {
        using var f = new Fixture("none", lost); var command = f.CommodityReturn(buy, online, mailed, satchel);
        Assert.True((await command.ExecuteAsync()).Committed); Assert.True((await command.ResolveAsync()).Committed);
        using var read = f.Context(); Assert.Empty(read.MarketplaceCommodityOrder); Assert.Single(read.MarketplaceBidReceipt);
        Assert.Equal(buy && online ? 30ul : 0ul, read.CharacterCurrency.SingleOrDefault(row => row.Id == 1)?.Amount ?? 0);
        if (!buy)
        {
            var delivered = read.Item.Single(row => row.Id == 2000);
            Assert.Equal(satchel ? 1u : 3u, delivered.StackCount); Assert.Equal(1ul, delivered.OwnerId);
            Assert.Equal((ushort)(mailed ? InventoryLocation.None : InventoryLocation.Inventory), delivered.Location);
            if (satchel) Assert.Equal(100, read.CharacterTradeskillMaterial.Single().Amount);
        }
        if (mailed) Assert.Equal(buy ? 30ul : 0ul, Assert.Single(read.CharacterMail).CurrencyAmount);
        else Assert.Empty(read.CharacterMail);
        Assert.Equal(!buy && mailed ? 1 : 0, read.CharacterMailAttachment.Count());
    }

    [Theory]
    [InlineData("refund")] [InlineData("item-credit")] [InlineData("mail-owner")]
    [InlineData("sell-credit")] [InlineData("quantity")] [InlineData("owner")] [InlineData("merge-metadata")]
    public void CommodityReturnRejectsChangedFinancialAndFungibleRoles(string fault)
    {
        using var f = new Fixture("none"); bool buy = fault is "refund" or "item-credit" or "mail-owner";
        Assert.Throws<InvalidOperationException>(() => f.CommodityReturn(buy, fault != "mail-owner", fault == "mail-owner", false, fault));
    }

    [Fact]
    public async Task CommodityExpiryCannotRemoveAFutureOrder()
    {
        using var f = new Fixture("none"); var command = f.CommodityReturn(true, true, false, false, "future-expiry");
        Assert.False((await command.ExecuteAsync()).Committed);
        using var read = f.Context(); Assert.Single(read.MarketplaceCommodityOrder); Assert.Empty(read.MarketplaceBidReceipt);
    }

    [Theory]
    [InlineData("order")] [InlineData("wallet")] [InlineData("slot")]
    public async Task CommodityReturnRejectsStoredGraphChangesWithoutPartialDeletion(string fault)
    {
        using var f = new Fixture("none"); var command = f.CommodityReturn(fault == "wallet", true, false, false);
        using (var changed = f.Context())
        {
            if (fault == "order") changed.MarketplaceCommodityOrder.Single().Quantity = 2;
            else if (fault == "wallet") changed.CharacterCurrency.Add(new() { Id = 1, CurrencyId = 1, Amount = 25 });
            else changed.Item.Add(new() { Id = 3000, OwnerId = 1, ItemId = 7001, Location = (ushort)InventoryLocation.Inventory, BagIndex = 7, StackCount = 1 });
            await changed.SaveChangesAsync();
        }
        Assert.False((await command.ExecuteAsync()).Committed);
        using var read = f.Context(); Assert.Single(read.MarketplaceCommodityOrder); Assert.Empty(read.MarketplaceBidReceipt);
        Assert.DoesNotContain(read.Item, row => row.Id == 2000);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(true, false, false)]
    [InlineData(true, true, false)] [InlineData(true, false, true)]
    [InlineData(true, true, true)] [InlineData(false, false, true)]
    public async Task CancellationReturnsOwnerItemWithoutDebitAndRefundsTheFullHeldBid(bool heldBid, bool onlineBidder, bool mailed)
    {
        using var f = new Fixture(heldBid ? "online" : "none");
        var command = f.Cancellation(heldBid, onlineBidder, mailed);
        Assert.True((await command.ExecuteAsync()).Committed);
        Assert.True((await command.ResolveAsync()).Committed);
        using var read = f.Context(); Assert.Empty(read.MarketplaceAuction);
        var delivered = Assert.Single(read.Item); Assert.Equal(1ul, delivered.OwnerId);
        Assert.Equal((ushort)(mailed ? InventoryLocation.None : InventoryLocation.Inventory), delivered.Location);
        Assert.DoesNotContain(read.CharacterCurrency, row => row.Id == 1);
        Assert.Equal(heldBid && onlineBidder ? 150ul : 100ul, read.CharacterCurrency.Single(row => row.Id == 3).Amount);
        Assert.Equal(heldBid && !onlineBidder ? 50ul : 0ul, read.CharacterMail.Where(row => row.RecipientId == 3).Select(row => row.CurrencyAmount).SingleOrDefault());
        Assert.Equal(mailed ? 1 : 0, read.CharacterMailAttachment.Count());
        Assert.Single(read.MarketplaceBidReceipt);
    }

    [Theory]
    [InlineData("debit")] [InlineData("proceeds")] [InlineData("receiver")]
    [InlineData("refund")] [InlineData("amount")] [InlineData("self-bid")]
    [InlineData("bidder-without-bid")] [InlineData("bid-without-bidder")]
    public void CancellationRejectsChangedMoneyAndListingRoles(string fault)
    {
        using var f = new Fixture("online");
        Assert.Throws<InvalidOperationException>(() => f.Cancellation(true, true, false, fault));
    }

    [Theory]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, false, true)]
    [InlineData(false, false, true, true)]
    [InlineData(false, true, true, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, false, true)]
    public async Task ExpiryModesPreserveHeldBidAndReturnCustodyWithoutNewDebit(bool returning, bool online, bool sellerOnline, bool mail)
    {
        var kind = returning ? AuctionSettlementKind.UnsoldReturn : AuctionSettlementKind.WinningExpiry;
        using var f = new Fixture("none"); var command = f.Expiry(kind, online, sellerOnline, mail);
        Assert.Equal(new BidOutcome(true, true), await command.ExecuteAsync());
        Assert.Equal(new BidOutcome(true, true), await command.ResolveAsync());
        using var read = f.Context(); Assert.Empty(read.MarketplaceAuction); Assert.Single(read.MarketplaceBidReceipt);
        var receiver = kind == AuctionSettlementKind.UnsoldReturn ? 1ul : 2ul;
        var item = Assert.Single(read.Item); Assert.Equal(receiver, item.OwnerId);
        Assert.Equal((ushort)(mail ? InventoryLocation.None : InventoryLocation.Inventory), item.Location);
        Assert.Equal(500ul, read.CharacterCurrency.Single(row => row.Id == 2).Amount);
        if (kind == AuctionSettlementKind.UnsoldReturn) Assert.DoesNotContain(read.CharacterCurrency, row => row.Id == 1);
        else if (sellerOnline) Assert.Equal(147ul, read.CharacterCurrency.Single(row => row.Id == 1).Amount);
        else Assert.Equal(47ul, Assert.Single(read.CharacterMail.Where(row => row.RecipientId == 1)).CurrencyAmount);
        Assert.Equal(mail ? 1 : 0, read.CharacterMailAttachment.Count());
    }

    [Theory]
    [InlineData("double-debit")] [InlineData("amount")] [InlineData("recipient")] [InlineData("return-proceeds")] [InlineData("unreserved-slot")]
    public void ExpiryCommandRejectsChangedFinancialOrReceiverRoles(string fault)
    {
        using var f = new Fixture("none");
        Assert.Throws<InvalidOperationException>(() => f.Expiry(fault == "return-proceeds" ? AuctionSettlementKind.UnsoldReturn : AuctionSettlementKind.WinningExpiry,
            true, false, false, fault));
    }

    [Fact]
    public async Task ExpiryCannotSettleAFutureListing()
    {
        using var f = new Fixture("none"); var command = f.Expiry(AuctionSettlementKind.WinningExpiry, true, false, false, "future");
        Assert.Equal(new BidOutcome(false, true), await command.ExecuteAsync());
        using var read = f.Context(); Assert.Single(read.MarketplaceAuction); Assert.Empty(read.MarketplaceBidReceipt);
        Assert.Null(read.Item.Single().OwnerId); Assert.Empty(read.CharacterMail);
    }

    [Theory]
    [InlineData(499ul, 474ul)] [InlineData(500ul, 0ul)] [InlineData(1000ul, 500ul)]
    public async Task BuyoutUsesTheExistingMinimumFeeBoundaryBeforeCapturingProceeds(ulong price, ulong net)
    {
        using var f = new Fixture("none", buyout: true, buyoutPrice: price);
        Assert.True((await f.Buyout(false).ExecuteAsync()).Committed);
        using var read = f.Context(); Assert.Empty(read.MarketplaceAuction);
        Assert.Equal(Math.Max(500ul, price * 2) - price, read.CharacterCurrency.Single(row => row.Id == 2).Amount);
        if (net == 0) Assert.Empty(read.CharacterMail);
        else Assert.Equal(net, Assert.Single(read.CharacterMail).CurrencyAmount);
        Assert.Equal(2ul, Assert.Single(read.Item).OwnerId);
    }

    [Theory]
    [InlineData("none", false, false)] [InlineData("self", false, false)]
    [InlineData("online", false, false)] [InlineData("offline", false, false)]
    [InlineData("new-wallet", false, false)] [InlineData("none", true, false)]
    [InlineData("offline", true, true)] [InlineData("online", false, true)]
    public async Task BuyoutDeletesListingAndTransfersWholeItemWithDebitProceedsAndRefundInOneCommit(string prior, bool mail, bool lose)
    {
        using var f = new Fixture(prior, lose, buyout: true);
        var command = f.Buyout(mail); Assert.Equal(new BidOutcome(true, true), await command.ExecuteAsync());
        Assert.Equal(new BidOutcome(true, true), await command.ResolveAsync());
        using var read = f.Context(); Assert.Empty(read.MarketplaceAuction);
        Assert.Equal(prior == "self" ? 470ul : 420ul, read.CharacterCurrency.Single(row => row.Id == 2).Amount);
        Assert.Equal(76ul, Assert.Single(read.CharacterMail.Where(row => row.RecipientId == 1)).CurrencyAmount);
        var delivered = Assert.Single(read.Item); Assert.Equal(1000ul, delivered.Id); Assert.Equal(2ul, delivered.OwnerId);
        Assert.Equal((ushort)(mail ? InventoryLocation.None : InventoryLocation.Inventory), delivered.Location);
        Assert.Equal(mail ? 0u : 7u, delivered.BagIndex); Assert.Equal(0u, delivered.Charges); Assert.False(delivered.Soulbound);
        if (prior == "offline") Assert.Equal(50ul, Assert.Single(read.CharacterMail.Where(row => row.RecipientId == 3)).CurrencyAmount);
        if (prior is "online" or "new-wallet") Assert.Equal(prior == "online" ? 150ul : 50ul, read.CharacterCurrency.Single(row => row.Id == 3).Amount);
        if (mail)
        {
            var attachment = Assert.Single(read.CharacterMailAttachment);
            Assert.Equal(97ul, attachment.Id); Assert.Equal(0u, attachment.Index); Assert.Equal(delivered.Id, attachment.ItemGuid);
        }
        else Assert.Empty(read.CharacterMailAttachment);
        Assert.Single(read.MarketplaceBidReceipt);
        Assert.Equal(lose ? 1 : 0, f.AcknowledgementsLost);
    }

    [Theory]
    [InlineData("delete")] [InlineData("item")] [InlineData("seller")]
    public async Task RejectedOrRewrittenBuyoutRollsBackEveryFinancialAndCustodyChange(string fault)
    {
        using var f = new Fixture("offline", buyout: true);
        using (var setup = f.Context()) setup.Database.ExecuteSqlRaw(fault switch
        {
            "delete" => "CREATE TRIGGER reject_sale BEFORE DELETE ON marketplace_auction BEGIN SELECT RAISE(ABORT,'sale fault'); END;",
            "item" => "CREATE TRIGGER rewrite_sale AFTER UPDATE ON item BEGIN UPDATE item SET stackCount=7 WHERE id=NEW.id; END;",
            _ => "CREATE TRIGGER rewrite_sale AFTER INSERT ON character_mail BEGIN UPDATE character_mail SET currencyAmount=1 WHERE id=98; END;"
        });
        Assert.Equal(new BidOutcome(false, true), await f.Buyout(false).ExecuteAsync());
        using var read = f.Context(); Assert.Single(read.MarketplaceAuction); Assert.Empty(read.MarketplaceBidReceipt);
        Assert.Equal(500ul, read.CharacterCurrency.Single(row => row.Id == 2).Amount);
        Assert.Null(Assert.Single(read.Item).OwnerId); Assert.Equal(1u, Assert.Single(read.Item).StackCount);
        Assert.Empty(read.CharacterMail); Assert.Empty(read.CharacterMailAttachment);
    }

    [Fact]
    public async Task HistoricBuyoutReceiptDoesNotAuthorizeChangedCurrentItemCustody()
    {
        using var f = new Fixture("none", buyout: true); var command = f.Buyout(false);
        Assert.True((await command.ExecuteAsync()).Committed);
        using (var moved = f.Context()) { moved.Item.Single().OwnerId = 3; await moved.SaveChangesAsync(); }
        var observed = await command.ReadReconciliationAsync();
        Assert.True(observed.Outcome.Committed); Assert.False(observed.Outcome.CurrentGraphMatches);
        Assert.False(observed.DeliveryMatches); Assert.Equal(3ul, observed.Item.Owner);
        using var read = f.Context(); Assert.Equal(3ul, read.Item.Single().OwnerId);
    }

    [Theory]
    [InlineData("none", false)] [InlineData("none", true)]
    [InlineData("self", false)] [InlineData("self", true)]
    [InlineData("online", false)] [InlineData("online", true)]
    [InlineData("offline", false)] [InlineData("offline", true)]
    [InlineData("new-wallet", false)] [InlineData("new-wallet", true)]
    public async Task ListingDebitAndPriorRefundShareOneRecoverableCommit(string prior, bool lostAcknowledgement)
    {
        using var f = new Fixture(prior, lostAcknowledgement);
        var command = f.Command(); var result = await command.ExecuteAsync();
        Assert.Equal(new BidOutcome(true, true), result);
        Assert.Equal(result, await command.ResolveAsync());
        using var read = f.Context(); var auction = Assert.Single(read.MarketplaceAuction);
        Assert.Equal(80ul, auction.CurrentBid); Assert.Equal(2ul, auction.TopBidderCharacterId);
        Assert.Equal(prior == "self" ? 470ul : 420ul, read.CharacterCurrency.Single(row => row.Id == 2).Amount);
        Assert.Equal(prior is "online" or "new-wallet" ? prior == "online" ? 150ul : 50ul : 100ul,
            read.CharacterCurrency.Single(row => row.Id == 3).Amount);
        if (prior == "offline") Assert.Equal(50ul, Assert.Single(read.CharacterMail).CurrencyAmount);
        else Assert.Empty(read.CharacterMail);
        Assert.Single(read.MarketplaceBidReceipt); Assert.Single(read.Item);
        Assert.True(!lostAcknowledgement || f.AcknowledgementsLost == 1);
    }

    [Theory]
    [InlineData("listing")] [InlineData("wallet-postimage")] [InlineData("mail-postimage")] [InlineData("receipt-postimage")]
    public async Task SqlRejectionOrRewrittenPostimageRollsBackEveryParticipant(string fault)
    {
        using var f = new Fixture("offline");
        using (var setup = f.Context())
            setup.Database.ExecuteSqlRaw(fault switch
            {
                "listing" => "CREATE TRIGGER reject_bid BEFORE UPDATE ON marketplace_auction BEGIN SELECT RAISE(ABORT,'bid fault'); END;",
                "wallet-postimage" => "CREATE TRIGGER rewrite_bid AFTER UPDATE ON character_currency BEGIN UPDATE character_currency SET amount=7 WHERE id=2; END;",
                "mail-postimage" => "CREATE TRIGGER rewrite_bid AFTER INSERT ON character_mail BEGIN UPDATE character_mail SET currencyAmount=7 WHERE id=99; END;",
                _ => "CREATE TRIGGER rewrite_bid AFTER INSERT ON marketplace_bid_receipt BEGIN UPDATE marketplace_bid_receipt SET payload='other' WHERE id=NEW.id; END;"
            });
        Assert.Equal(new BidOutcome(false, true), await f.Command().ExecuteAsync());
        using var read = f.Context(); Assert.Equal(50ul, Assert.Single(read.MarketplaceAuction).CurrentBid);
        Assert.Equal(500ul, read.CharacterCurrency.Single(row => row.Id == 2).Amount);
        Assert.Equal(100ul, read.CharacterCurrency.Single(row => row.Id == 3).Amount);
        Assert.Empty(read.CharacterMail); Assert.Empty(read.MarketplaceBidReceipt);
    }

    [Theory]
    [InlineData("account")] [InlineData("deleted")] [InlineData("wallet")] [InlineData("listing")] [InlineData("item")]
    public async Task ChangedCapturedOwnerOrGraphCannotExecuteTheBid(string fault)
    {
        using var f = new Fixture("online"); var command = f.Command();
        using (var change = f.Context())
        {
            if (fault == "account") change.Character.Single(row => row.Id == 3).AccountId = 999;
            if (fault == "deleted") change.Character.Single(row => row.Id == 1).DeleteTime = DateTime.UtcNow;
            if (fault == "wallet") change.CharacterCurrency.Single(row => row.Id == 2).Amount = 777;
            if (fault == "listing") change.MarketplaceAuction.Single().MicrochipIds = "7";
            if (fault == "item") change.Item.Single().OwnerId = 3;
            await change.SaveChangesAsync();
        }
        Assert.Equal(new BidOutcome(false, false), await command.ExecuteAsync());
        using var read = f.Context(); Assert.Equal(50ul, Assert.Single(read.MarketplaceAuction).CurrentBid);
        Assert.Empty(read.MarketplaceBidReceipt); Assert.Empty(read.CharacterMail);
        Assert.Equal(fault == "wallet" ? 777ul : 500ul, read.CharacterCurrency.Single(row => row.Id == 2).Amount);
    }

    [Fact]
    public async Task ReceiptCannotAcknowledgeAnotherPayloadWithTheSameCommandId()
    {
        using var f = new Fixture("online"); Assert.True((await f.Command().ExecuteAsync()).Committed);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Command(amount: 81).ExecuteAsync());
        using var read = f.Context(); Assert.Equal(80ul, Assert.Single(read.MarketplaceAuction).CurrentBid);
        Assert.Equal(420ul, read.CharacterCurrency.Single(row => row.Id == 2).Amount); Assert.Single(read.MarketplaceBidReceipt);
    }

    [Fact]
    public async Task HistoricalReceiptDoesNotAuthorizeRewindingALaterBalanceOrListing()
    {
        using var f = new Fixture("online"); var command = f.Command(); Assert.True((await command.ExecuteAsync()).Committed);
        using (var later = f.Context())
        {
            later.CharacterCurrency.Single(row => row.Id == 2).Amount = 900;
            later.MarketplaceAuction.Single().CurrentBid = 100;
            await later.SaveChangesAsync();
        }
        Assert.Equal(new BidOutcome(true, false), await command.ResolveAsync());
        using var read = f.Context(); Assert.Equal(900ul, read.CharacterCurrency.Single(row => row.Id == 2).Amount);
        Assert.Equal(100ul, Assert.Single(read.MarketplaceAuction).CurrentBid); Assert.Single(read.MarketplaceBidReceipt);
    }

    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        internal int Lost;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref Lost) == 1) return Task.FromException(new IOException("Committed normal bid lost its acknowledgement."));
            return Task.CompletedTask;
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "NexusForever-NormalBid-" + Guid.NewGuid().ToString("N"));
        private readonly string connection;
        private readonly string prior;
        private readonly ulong startingWallet, buyoutPrice;
        private readonly LostAcknowledgement lost;
        private readonly AuctionRow before;
        private readonly BidItem item;
        private readonly string database, id = Guid.NewGuid().ToString("N");
        private readonly DateTime created = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
        internal int AcknowledgementsLost => lost?.Lost ?? 0;

        internal Fixture(string prior, bool lose = false, bool buyout = false, ulong buyoutPrice = 80)
        {
            this.prior = prior; this.buyoutPrice = buyoutPrice; startingWallet = buyout ? Math.Max(500ul, buyoutPrice * 2) : 500ul; Directory.CreateDirectory(directory);
            connection = $"Data Source={Path.Combine(directory, "bid.sqlite")};Pooling=False;Foreign Keys=True";
            using var seed = Context(); seed.Database.EnsureCreated();
            for (ulong owner = 1; owner <= 3; owner++)
                seed.Character.Add(new() { Id = owner, AccountId = (uint)owner + 100, Name = "Bid" + owner, OriginalName = "Bid" + owner });
            seed.CharacterCurrency.Add(new() { Id = 2, CurrencyId = 1, Amount = startingWallet });
            if (prior != "new-wallet") seed.CharacterCurrency.Add(new() { Id = 3, CurrencyId = 1, Amount = 100 });
            var storedItem = new ItemModel { Id = 1000, ItemId = 7001, StackCount = 1, Location = (ushort)InventoryLocation.None };
            seed.Item.Add(storedItem);
            var auction = new MarketplaceAuctionModel { Id = 1, OwnerCharacterId = 1, ItemId = 1000, Item2Id = 7001, Quantity = 1,
                MinimumBid = 10, BuyoutPrice = buyout ? buyoutPrice : 1000ul, CurrentBid = prior == "none" ? 0ul : 50ul,
                TopBidderCharacterId = prior == "none" ? 0ul : prior == "self" ? 2ul : 3ul,
                ExpirationTime = (ulong)DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds() };
            seed.MarketplaceAuction.Add(auction); seed.SaveChanges();
            database = CharacterDatabaseIdentity.Get(seed); before = AuctionRow.Capture(auction); item = BidItem.Capture(storedItem);
            if (lose) lost = new();
        }

        internal CharacterContext Context()
        {
            var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection);
            if (lost != null) options.AddInterceptors(lost);
            return new(options.Options);
        }

        internal AuctionSettlementCommand Command(ulong amount = 80)
        {
            List<BidCharacter> characters = [new(1, 101), new(2, 102)];
            List<BidWallet> wallets = [new(2, 102, true, 500, 500 - (prior == "self" ? amount - 50 : amount), 0)];
            MailHeaderState mail = null;
            if (prior is "online" or "offline" or "new-wallet") characters.Add(new(3, 103));
            if (prior is "online" or "new-wallet") wallets.Add(new(3, 103, prior != "new-wallet", prior == "online" ? 100ul : 0ul, prior == "online" ? 150ul : 50ul, 0));
            if (prior == "offline") mail = new(99, 3, 3, 3, 0, "Marketplace", "Refund", 0, 0, 0, 1, 50, 0, 0, 0, 0, created);
            return new(Context, database, id, created, before, 2, amount, item, characters, wallets, mail);
        }

        internal AuctionSettlementCommand Buyout(bool byMail)
        {
            List<BidCharacter> characters = [new(1, 101), new(2, 102)];
            List<BidWallet> wallets = [new(2, 102, true, startingWallet, startingWallet - (prior == "self" ? buyoutPrice - 50 : buyoutPrice), 0)];
            ulong proceeds = MarketplaceTransactionFee.CalculateItemAuctionSellerProceeds(buyoutPrice);
            if (prior is "online" or "offline" or "new-wallet") characters.Add(new(3, 103));
            if (prior is "online" or "new-wallet") wallets.Add(new(3, 103, prior != "new-wallet", prior == "online" ? 100ul : 0ul, prior == "online" ? 150ul : 50ul, 0));
            MailHeaderState Credit(ulong id, ulong recipient, ulong amount) => new(id, recipient, 3, 3, 0, "Marketplace", "Credits", 0, 0, 0, 1, amount, 0, 0, 0, 0, created);
            var sale = new AuctionDeliverySettlement(proceeds, item with { Owner = 2, Location = (ushort)(byMail ? InventoryLocation.None : InventoryLocation.Inventory), Bag = byMail ? 0u : 7u },
                proceeds != 0 ? Credit(98, 1, proceeds) : null, byMail ? Credit(97, 2, 0) : null);
            return new(Context, database, id, created, before, 2, buyoutPrice, item, characters, wallets, prior == "offline" ? Credit(99, 3, 50) : null, sale);
        }

        internal CommodityReturnCommand CommodityReturn(bool buy, bool online, bool mailed, bool satchel, string fault = null)
        {
            var order = new CommodityRow(10, 1, 7001, 3, 10, 30, buy, false,
                (ulong)DateTime.UtcNow.AddHours(-1).ToFileTimeUtc(), (ulong)DateTime.UtcNow.AddHours(1).ToFileTimeUtc());
            var delivered = item with { Id = 2000, Owner = fault == "owner" ? 2ul : 1ul, Quantity = fault == "quantity" ? 4u : satchel ? 1u : 3u,
                Location = (ushort)(mailed ? InventoryLocation.None : InventoryLocation.Inventory), Bag = mailed ? 0u : 7u, Durability = 1 };
            BidItem existing = null;
            if (fault == "merge-metadata") { existing = delivered with { Quantity = 2 }; delivered = delivered with { Quantity = 5, Bound = true }; }
            // Seeding is not the command's fault-injected transaction.
            using (var seed = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).Options))
            {
                seed.MarketplaceCommodityOrder.Add(order.Model());
                if (satchel) seed.CharacterTradeskillMaterial.Add(new() { Id = 1, MaterialId = 1, Amount = 98 });
                if (existing != null) seed.Item.Add(existing.Model()); seed.SaveChanges();
            }
            BidWallet money = online ? new(1, 101, false, 0, buy ? fault == "refund" ? 29ul : 30ul : fault == "sell-credit" ? 1ul : 0ul, 1000) : null;
            CommodityItemWrite[] returned = !buy || fault == "item-credit" ? [new(existing, delivered)] : [];
            MailHeaderState mail = mailed ? new(97, fault == "mail-owner" ? 2ul : 1ul, 3, 3, 0, "Marketplace", "Commodity", 0, 0, 0, 1, buy ? 30ul : 0ul, 0, 0, 0, 0, created) : null;
            return new(Context, database, id, created, order, new(1, 101), fault == "future-expiry", money, returned,
                satchel ? new(1, true, 98, 100, 100) : null, mail);
        }

        internal AuctionSettlementCommand Cancellation(bool heldBid, bool onlineBidder, bool mailed, string fault = null)
        {
            var original = before with { Bid = fault == "bidder-without-bid" ? 0ul : heldBid ? 50ul : 0ul,
                Bidder = fault == "bid-without-bidder" ? 0ul : fault == "self-bid" ? 1ul : heldBid ? 3ul : 0ul };
            List<BidCharacter> owners = heldBid ? [new(1, 101), new(3, 103)] : [new(1, 101)];
            List<BidWallet> money = [new(1, 101, false, 0, fault == "debit" ? 1ul : 0ul, 1000)];
            if (heldBid && onlineBidder) money.Add(new(3, 103, true, 100, fault == "refund" ? 149ul : 150ul, 1000));
            MailHeaderState Credit(ulong key, ulong recipient, ulong value) => new(key, recipient, 3, 3, 0, "Marketplace", "Cancellation", 0, 0, 0, 1, value, 0, 0, 0, 0, created);
            var sale = new AuctionDeliverySettlement(fault == "proceeds" ? 1ul : 0ul,
                item with { Owner = 1, Location = (ushort)(mailed ? InventoryLocation.None : InventoryLocation.Inventory), Bag = mailed ? 0u : 7u },
                DeliveryMail: mailed ? Credit(97, 1, 0) : null, Kind: AuctionSettlementKind.CancelledReturn);
            return new(Context, database, id, created, original, fault == "receiver" ? 3ul : 1ul,
                fault == "amount" ? 1ul : 0ul, item, owners, money,
                heldBid && !onlineBidder ? Credit(99, 3, 50) : null, sale);
        }

        internal AuctionSettlementCommand Expiry(AuctionSettlementKind kind, bool online, bool sellerOnline, bool mailed, string fault = null)
        {
            bool returning = kind == AuctionSettlementKind.UnsoldReturn;
            ulong receiver = returning ? 1ul : 2ul;
            AuctionRow original;
            using (var setup = Context())
            {
                var row = setup.MarketplaceAuction.Single(); row.CurrentBid = returning ? 0ul : 50ul;
                row.TopBidderCharacterId = returning ? 0ul : 2ul;
                row.ExpirationTime = (ulong)DateTimeOffset.UtcNow.AddMinutes(fault == "future" ? 1 : -1).ToUnixTimeSeconds();
                if (sellerOnline) setup.CharacterCurrency.Add(new() { Id = 1, CurrencyId = 1, Amount = 100 });
                setup.SaveChanges(); original = AuctionRow.Capture(row);
            }
            List<BidCharacter> owners = returning ? [new(1, 101)] : [new(1, 101), new(2, 102)];
            List<BidWallet> money = [];
            if (online && fault != "unreserved-slot") money.Add(new(receiver, (uint)receiver + 100, !returning, returning ? 0ul : 500ul,
                fault == "double-debit" ? 450ul : returning ? 0ul : 500ul, 1000));
            if (sellerOnline) money.Add(new(1, 101, true, 100, 147, 1000));
            MailHeaderState Credit(ulong key, ulong recipient, ulong value) => new(key, recipient, 3, 3, 0, "Marketplace", "Expiry", 0, 0, 0, 1, value, 0, 0, 0, 0, created);
            var sale = new AuctionDeliverySettlement(returning ? fault == "return-proceeds" ? 1ul : 0ul : 47ul,
                item with { Owner = fault == "recipient" ? 3ul : receiver, Location = (ushort)(mailed ? InventoryLocation.None : InventoryLocation.Inventory), Bag = mailed ? 0u : 7u },
                !returning && !sellerOnline ? Credit(98, 1, 47) : null, mailed ? Credit(97, receiver, 0) : null, kind);
            return new(Context, database, id, created, original, receiver, fault == "amount" ? 51ul : returning ? 0ul : 50ul, item, owners, money, sale: sale);
        }

        public void Dispose()
        {
            var path = Path.GetFullPath(directory);
            if (!path.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(path).StartsWith("NexusForever-NormalBid-", StringComparison.Ordinal)) throw new InvalidOperationException("Bid fixture escaped temp root.");
            TemporaryDirectoryCleanup.Delete(path);
        }
    }
}
