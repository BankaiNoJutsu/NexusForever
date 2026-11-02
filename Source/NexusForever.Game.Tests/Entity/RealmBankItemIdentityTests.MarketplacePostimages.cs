using Microsoft.EntityFrameworkCore;
using System.Reflection;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MarketplaceAuctionBidRequiresItsActualSqlPostimage(bool rewrite)
    {
        var f = await MailFixture(); var manager = await StoredMarketplaceAuction(f, 200);
        if (rewrite)
        {
            using var edit = new CharacterContext(Configuration);
            await edit.Database.ExecuteSqlRawAsync("CREATE TRIGGER rewrite_bid AFTER UPDATE ON marketplace_auction BEGIN UPDATE marketplace_auction SET currentBid=99 WHERE id=NEW.id; END;");
        }
        Assert.Equal(rewrite ? GenericError.DbFailure : GenericError.Ok, manager.BuyAuction(f.Player, StoredMarketplaceBid(10), out var auction));
        Assert.Equal(rewrite ? 100ul : 90ul, Assert.Single(f.Currency).Amount);
        Assert.Equal(rewrite ? 0ul : 10ul, auction.CurrentBid);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(rewrite ? 0ul : 10ul, Assert.Single(read.MarketplaceAuction).CurrentBid);
        Assert.Empty(read.CharacterMail); Assert.Null(Assert.Single(read.Item).OwnerId);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MarketplaceCommodityRefundRequiresActualSqlAbsence(bool recreate)
    {
        var f = await MailFixture();
        using (var seed = new CharacterContext(Configuration))
        {
            seed.MarketplaceCommodityOrder.Add(new MarketplaceCommodityOrderModel {
                Id = 1, OwnerCharacterId = 100, Item2Id = 7001, Quantity = 2, PricePerUnit = 10, Price = 20, IsBuyOrder = true });
            await seed.SaveChangesAsync();
        }
        var manager = StoredMarketplace(f); manager.Initialise();
        if (recreate) await RecreateDeletedMarketplaceRow("marketplace_commodity_order");
        Assert.Equal(recreate ? GenericError.DbFailure : GenericError.Ok, manager.CancelCommodityOrder(f.Player, 1, 7001, true, out _));
        Assert.Equal(recreate ? 100ul : 120ul, Assert.Single(f.Currency).Amount);
        Assert.Equal(recreate ? 1 : 0, manager.GetOwnedCommodityOrders(f.Player).Count);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(recreate ? 1 : 0, read.MarketplaceCommodityOrder.Count()); Assert.Empty(read.CharacterMail);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task MarketplaceAuctionReturnRequiresActualSqlAbsence(bool recreate, bool byMail)
    {
        var f = await MailFixture(); var manager = await StoredMarketplaceAuction(f, 100);
        var inventory = RecordingDispatchProxy<IInventory>.Create(out var ip);
        ip.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), byMail ? 0u : 1u);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.Inventory), inventory);
        if (recreate) await RecreateDeletedMarketplaceRow("marketplace_auction");
        Assert.Equal(recreate ? byMail ? GenericError.ItemInventoryFull : GenericError.DbFailure : GenericError.Ok,
            manager.CancelAuction(f.Player, 1, 7001, out _));
        Assert.Equal(recreate ? 1 : 0, manager.GetOwnedItemAuctions(f.Player).Count);
        Assert.Equal(!recreate && !byMail ? 1 : 0, ip.GetInvocations(nameof(IInventory.AddItem)).Count);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(recreate ? 1 : 0, read.MarketplaceAuction.Count());
        Assert.Equal(!recreate && byMail ? 1 : 0, read.CharacterMail.Count());
        Assert.Equal(!recreate && byMail ? 1 : 0, read.CharacterMailAttachment.Count());
        Assert.Equal(recreate ? null : (ulong?)100, Assert.Single(read.Item).OwnerId);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task MarketplacePartialFillRequiresActualSqlPostimageBeforeDelivery(bool rewrite, bool byMail)
    {
        var f = await MailFixture(); var (manager, inventory) = await StoredMarketplaceSellOrder(f);
        inventory.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), byMail ? 0u : 2u);
        if (rewrite)
        {
            using var edit = new CharacterContext(Configuration);
            await edit.Database.ExecuteSqlRawAsync("CREATE TRIGGER rewrite_fill AFTER UPDATE ON marketplace_commodity_order BEGIN UPDATE marketplace_commodity_order SET quantity=2,price=20 WHERE id=NEW.id; END;");
        }
        Assert.Equal(GenericError.Ok, manager.PostCommodityOrder(f.Player, new CommodityOrder {
            Item2Id = 7001, IsBuyOrder = true, ForceImmediate = true, Quantity = 1, PricePerUnit = 10, Price = 10 }, out _, f.Items));
        Assert.Equal(rewrite ? 100ul : 90ul, Assert.Single(f.Currency).Amount);
        Assert.Equal(!rewrite && !byMail ? 1 : 0, inventory.GetInvocations(nameof(IInventory.ItemCreate)).Count);
        Assert.Equal(rewrite ? 2u : 1u, Assert.Single(manager.BuildCommodityInfo(7001).Orders).Quantity);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(rewrite ? 2u : 1u, Assert.Single(read.MarketplaceCommodityOrder).Quantity);
        Assert.Equal(!rewrite && byMail ? 1 : 0, read.CharacterMail.Count(row => row.RecipientId == 100));
        Assert.Equal(rewrite ? 0 : 1, read.CharacterMail.Count(row => row.RecipientId == 200));
    }

    private async Task RecreateDeletedMarketplaceRow(string table)
    {
        Assert.Contains(table, new[] { "marketplace_auction", "marketplace_commodity_order" });
        using var edit = new CharacterContext(Configuration);
        string snapshot = $"CREATE TABLE postimage_original AS SELECT * FROM {table};";
        string trigger = $"CREATE TRIGGER recreate_listing AFTER DELETE ON {table} BEGIN INSERT INTO {table} SELECT * FROM postimage_original WHERE id=OLD.id; END;";
        await edit.Database.ExecuteSqlRawAsync(snapshot);
        await edit.Database.ExecuteSqlRawAsync(trigger);
    }

    [Theory]
    [InlineData("suppress")] [InlineData("mask")] [InlineData("detach")]
    [InlineData("original")] [InlineData("scalar")] [InlineData("transaction")]
    public async Task MarketplaceListingWitnessRejectsChangedEnrollmentBeforeWriting(string fault)
    {
        var f = await MailFixture(); var manager = await StoredMarketplaceAuction(f, 200);
        using (var save = new CharacterContext(Configuration))
        {
            var row = await save.MarketplaceAuction.SingleAsync(); row.CurrentBid = 10;
            ObserveMarketplaceAuctionWrite(save, manager, row);
            var entry = save.Entry(row);
            if (fault == "suppress") entry.State = EntityState.Unchanged;
            if (fault == "mask") entry.Property(r => r.CurrentBid).IsModified = false;
            if (fault == "detach") entry.State = EntityState.Detached;
            if (fault == "original") entry.Property(r => r.CurrentBid).OriginalValue = 42;
            if (fault == "scalar") row.MinimumBid = 11;
            if (fault == "transaction") save.Database.AutoTransactionBehavior = AutoTransactionBehavior.WhenNeeded;
            await Assert.ThrowsAsync<InvalidOperationException>(() => save.SaveChangesAsync());
            Assert.True(save.IsSaveRejected);
        }
        using var read = new CharacterContext(Configuration); Assert.Equal(0ul, Assert.Single(read.MarketplaceAuction).CurrentBid);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MarketplaceListingWitnessSupportsRepeatedFlushAndTerminalSettlement(bool commit)
    {
        var f = await MailFixture(); var manager = await StoredMarketplaceAuction(f, 200);
        using (var save = new CharacterContext(Configuration))
        {
            using var transaction = save.Database.BeginTransaction();
            var row = await save.MarketplaceAuction.SingleAsync(); row.CurrentBid = 10;
            ObserveMarketplaceAuctionWrite(save, manager, row);
            await save.SaveChangesAsync(); await save.SaveChangesAsync();
            if (commit) await transaction.CommitAsync(); else await transaction.RollbackAsync();
        }
        using var read = new CharacterContext(Configuration); Assert.Equal(commit ? 10ul : 0ul, Assert.Single(read.MarketplaceAuction).CurrentBid);
    }

    [Fact]
    public async Task MarketplaceListingWitnessRejectsChangedSqlBeforeOuterCommit()
    {
        var f = await MailFixture(); var manager = await StoredMarketplaceAuction(f, 200);
        using (var save = new CharacterContext(Configuration))
        {
            await using var transaction = await save.Database.BeginTransactionAsync();
            var row = await save.MarketplaceAuction.SingleAsync(); row.CurrentBid = 10;
            ObserveMarketplaceAuctionWrite(save, manager, row); await save.SaveChangesAsync();
            await save.MarketplaceAuction.Where(r => r.Id == 1).ExecuteUpdateAsync(set => set.SetProperty(r => r.CurrentBid, 99ul));
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
        }
        using var read = new CharacterContext(Configuration); Assert.Equal(0ul, Assert.Single(read.MarketplaceAuction).CurrentBid);
    }

    [Fact]
    public async Task MarketplaceListingWitnessDoesNotReclassifyACommitFromALaterSqlWriter()
    {
        var f = await MailFixture(); var manager = await StoredMarketplaceAuction(f, 200);
        using (var save = new CharacterContext(Configuration))
        {
            // Register before the witness: an independent write occurs after the
            // implicit transaction has committed, before its SavedChanges observer.
            save.SavedChanges += (_, _) =>
            {
                Assert.Null(save.Database.CurrentTransaction);
                save.MarketplaceAuction.Where(r => r.Id == 1).ExecuteUpdate(set => set.SetProperty(r => r.CurrentBid, 11ul));
            };
            var row = await save.MarketplaceAuction.SingleAsync(); row.CurrentBid = 10;
            ObserveMarketplaceAuctionWrite(save, manager, row); await save.SaveChangesAsync();
        }
        using var read = new CharacterContext(Configuration); Assert.Equal(11ul, Assert.Single(read.MarketplaceAuction).CurrentBid);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MarketplaceCommodityInsertRequiresSqlPostimage(bool rewrite)
    {
        var f = await MailFixture(); var manager = StoredMarketplace(f); manager.Initialise();
        if (rewrite)
        {
            using var edit = new CharacterContext(Configuration);
            await edit.Database.ExecuteSqlRawAsync("CREATE TRIGGER rewrite_new_order AFTER INSERT ON marketplace_commodity_order BEGIN UPDATE marketplace_commodity_order SET price=99 WHERE id=NEW.id; END;");
        }
        Assert.Equal(rewrite ? GenericError.DbFailure : GenericError.Ok, manager.PostCommodityOrder(f.Player, new CommodityOrder {
            Item2Id = 7001, IsBuyOrder = true, Quantity = 2, PricePerUnit = 10, Price = 20 }, out _, f.Items));
        Assert.Equal(rewrite ? 100ul : 80ul, Assert.Single(f.Currency).Amount);
        Assert.Equal(rewrite ? 0 : 1, manager.GetOwnedCommodityOrders(f.Player).Count);
        using var read = new CharacterContext(Configuration); Assert.Equal(rewrite ? 0 : 1, read.MarketplaceCommodityOrder.Count());
        if (!rewrite) Assert.Equal(20ul, Assert.Single(read.MarketplaceCommodityOrder).Price);
        Assert.Empty(read.CharacterMail);
    }

    private static void ObserveMarketplaceAuctionWrite(CharacterContext context, GlobalMarketplaceManager manager, MarketplaceAuctionModel target)
    {
        // Exercise the real row owner in a caller-owned transaction to inject
        // boundaries that public manager methods do not expose as callbacks.
        var records = (System.Collections.IEnumerable)typeof(GlobalMarketplaceManager)
            .GetField("auctions", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(manager)!;
        object record = Assert.Single(records.Cast<object>());
        typeof(GlobalMarketplaceManager).GetMethod("ObserveAuctionRow", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [context, record, target]);
    }
}
