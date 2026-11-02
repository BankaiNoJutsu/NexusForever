using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData("none", null)]
    [InlineData(nameof(MarketplaceAuctionModel.Id), 2ul)]
    [InlineData(nameof(MarketplaceAuctionModel.OwnerCharacterId), 100ul)]
    [InlineData(nameof(MarketplaceAuctionModel.ItemId), 901ul)]
    [InlineData(nameof(MarketplaceAuctionModel.MinimumBid), 11ul)]
    [InlineData(nameof(MarketplaceAuctionModel.BuyoutPrice), 90ul)]
    [InlineData(nameof(MarketplaceAuctionModel.CurrentBid), 12ul)]
    [InlineData(nameof(MarketplaceAuctionModel.TopBidderCharacterId), 100ul)]
    [InlineData(nameof(MarketplaceAuctionModel.ExpirationTime), 1ul)]
    [InlineData(nameof(MarketplaceAuctionModel.Item2Id), 7002u)]
    [InlineData(nameof(MarketplaceAuctionModel.Quantity), 2u)]
    [InlineData(nameof(MarketplaceAuctionModel.WorldRequirementItem2Id), 7003u)]
    [InlineData(nameof(MarketplaceAuctionModel.CircuitData), 3ul)]
    [InlineData(nameof(MarketplaceAuctionModel.GlyphData), 4u)]
    [InlineData(nameof(MarketplaceAuctionModel.ThresholdData), 5ul)]
    [InlineData(nameof(MarketplaceAuctionModel.Unknown2), 6u)]
    [InlineData(nameof(MarketplaceAuctionModel.MicrochipIds), "81,82")]
    [InlineData(nameof(MarketplaceAuctionModel.MicrochipIds), " ")]
    [InlineData(nameof(MarketplaceAuctionModel.MicrochipIds), null)]
    public async Task MarketplaceAuctionDeleteRetainsACompetingCommittedRow(string field, object changed)
    {
        var f = await MailFixture(); var manager = await StoredMarketplaceAuction(f, 200);
        if (field == nameof(MarketplaceAuctionModel.ItemId))
        {
            using var seed = new CharacterContext(Configuration); var item = CharacterRow();
            item.Id = 901; item.OwnerId = null; item.Location = (ushort)InventoryLocation.None; seed.Item.Add(item); await seed.SaveChangesAsync();
        }
        int writes = 0;
        var race = new BeforeListingTransaction(() =>
        {
            if (field != "none")
            {
                using var edit = new CharacterContext(Configuration);
                if (field == nameof(MarketplaceAuctionModel.Id))
                    edit.Database.ExecuteSqlRaw("UPDATE marketplace_auction SET id=2 WHERE id=1;");
                else
                {
                    var row = edit.MarketplaceAuction.Single();
                    edit.Entry(row).Property(field).CurrentValue = changed; edit.SaveChanges();
                }
            }
            writes++;
        });
        using (var save = ListingRaceContext(race))
        {
            var row = await save.MarketplaceAuction.SingleAsync(); save.Remove(row);
            ObserveMarketplaceAuctionWrite(save, manager, null);
            var error = await Record.ExceptionAsync(() => save.SaveChangesAsync()); Assert.Equal(1, writes);
            if (field == "none") Assert.Null(error); else Assert.IsType<DbUpdateConcurrencyException>(error);
        }
        using var read = new CharacterContext(Configuration);
        if (field == "none") Assert.Empty(read.MarketplaceAuction);
        else Assert.Equal(changed, read.Entry(Assert.Single(read.MarketplaceAuction)).Property(field).CurrentValue);
    }

    [Theory]
    [InlineData("none", null)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.Id), 2ul)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.OwnerCharacterId), 200ul)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.Item2Id), 7002u)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.Quantity), 3u)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.PricePerUnit), 11ul)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.Price), 30ul)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.IsBuyOrder), false)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.ForceImmediate), true)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.ListTime), 1ul)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.ExpirationTime), 2ul)]
    public async Task MarketplaceCommodityDeleteRetainsACompetingCommittedRow(string field, object changed)
    {
        var f = await MailFixture();
        using (var seed = new CharacterContext(Configuration))
        {
            seed.MarketplaceCommodityOrder.Add(new MarketplaceCommodityOrderModel {
                Id = 1, OwnerCharacterId = 100, Item2Id = 7001, Quantity = 2, PricePerUnit = 10, Price = 20, IsBuyOrder = true });
            await seed.SaveChangesAsync();
        }
        var manager = StoredMarketplace(f); manager.Initialise(); int writes = 0;
        var race = new BeforeListingTransaction(() =>
        {
            if (field != "none")
            {
                using var edit = new CharacterContext(Configuration);
                if (field == nameof(MarketplaceCommodityOrderModel.Id))
                    edit.Database.ExecuteSqlRaw("UPDATE marketplace_commodity_order SET id=2 WHERE id=1;");
                else
                {
                    var row = edit.MarketplaceCommodityOrder.Single();
                    edit.Entry(row).Property(field).CurrentValue = changed; edit.SaveChanges();
                }
            }
            writes++;
        });
        using (var save = ListingRaceContext(race))
        {
            var row = await save.MarketplaceCommodityOrder.SingleAsync(); save.Remove(row);
            ObserveMarketplaceCommodityWrite(save, manager, null);
            var error = await Record.ExceptionAsync(() => save.SaveChangesAsync()); Assert.Equal(1, writes);
            if (field == "none") Assert.Null(error); else Assert.IsType<DbUpdateConcurrencyException>(error);
        }
        using var read = new CharacterContext(Configuration);
        if (field == "none") Assert.Empty(read.MarketplaceCommodityOrder);
        else Assert.Equal(changed, read.Entry(Assert.Single(read.MarketplaceCommodityOrder)).Property(field).CurrentValue);
    }

    [Theory]
    [InlineData("none", null)]
    [InlineData(nameof(MarketplaceAuctionModel.CurrentBid), 99ul)]
    [InlineData(nameof(MarketplaceAuctionModel.TopBidderCharacterId), 300ul)]
    [InlineData(nameof(MarketplaceAuctionModel.MicrochipIds), " ")]
    public async Task MarketplaceAuctionUpdateCannotOverwriteACompetingBid(string field, object changed)
    {
        var f = await MailFixture(); var manager = await StoredMarketplaceAuction(f, 200); int writes = 0;
        var race = new BeforeListingTransaction(() =>
        {
            if (field != "none")
            {
                using var edit = new CharacterContext(Configuration); var row = edit.MarketplaceAuction.Single();
                edit.Entry(row).Property(field).CurrentValue = changed; edit.SaveChanges();
            }
            writes++;
        });
        using (var save = ListingRaceContext(race))
        {
            var row = await save.MarketplaceAuction.SingleAsync(); row.CurrentBid = 10; row.TopBidderCharacterId = 100;
            ObserveMarketplaceAuctionWrite(save, manager, row);
            var error = await Record.ExceptionAsync(() => save.SaveChangesAsync()); Assert.Equal(1, writes);
            if (field == "none") Assert.Null(error); else Assert.IsType<DbUpdateConcurrencyException>(error);
        }
        using var read = new CharacterContext(Configuration); var stored = Assert.Single(read.MarketplaceAuction);
        if (field == "none") Assert.Equal(10ul, stored.CurrentBid);
        else Assert.Equal(changed, read.Entry(stored).Property(field).CurrentValue);
    }

    [Theory]
    [InlineData("none", null)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.Quantity), 3u)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.Price), 30ul)]
    public async Task MarketplaceCommodityUpdateCannotOverwriteACompetingFill(string field, object changed)
    {
        var f = await MailFixture(); var (manager, _) = await StoredMarketplaceSellOrder(f); int writes = 0;
        var race = new BeforeListingTransaction(() =>
        {
            if (field != "none")
            {
                using var edit = new CharacterContext(Configuration); var row = edit.MarketplaceCommodityOrder.Single();
                edit.Entry(row).Property(field).CurrentValue = changed; edit.SaveChanges();
            }
            writes++;
        });
        using (var save = ListingRaceContext(race))
        {
            var row = await save.MarketplaceCommodityOrder.SingleAsync(); row.Quantity = 1; row.Price = 10;
            ObserveMarketplaceCommodityWrite(save, manager, row);
            var error = await Record.ExceptionAsync(() => save.SaveChangesAsync()); Assert.Equal(1, writes);
            if (field == "none") Assert.Null(error); else Assert.IsType<DbUpdateConcurrencyException>(error);
        }
        using var read = new CharacterContext(Configuration); var stored = Assert.Single(read.MarketplaceCommodityOrder);
        if (field == "none") Assert.Equal(1u, stored.Quantity);
        else Assert.Equal(changed, read.Entry(stored).Property(field).CurrentValue);
    }

    private CharacterContext ListingRaceContext(BeforeListingTransaction interceptor)
    {
        var options = new DbContextOptionsBuilder<CharacterContext>();
        options.UseConfiguration(Configuration).AddInterceptors(interceptor);
        return new(options.Options);
    }

    [Fact]
    public async Task MarketplaceAuctionDeleteProtectsANullOriginalCsvFromACompetingWrite()
    {
        var f = await MailFixture(); var manager = await StoredMarketplaceAuction(f, 200);
        using (var seed = new CharacterContext(Configuration))
            await seed.Database.ExecuteSqlRawAsync("UPDATE marketplace_auction SET unknownArray=NULL WHERE id=1;");
        manager.Initialise(); int writes = 0;
        var race = new BeforeListingTransaction(() =>
        {
            using var edit = new CharacterContext(Configuration); var row = edit.MarketplaceAuction.Single();
            Assert.Null(row.MicrochipIds); row.MicrochipIds = "81,82"; edit.SaveChanges(); writes++;
        });
        using (var save = ListingRaceContext(race))
        {
            var row = await save.MarketplaceAuction.SingleAsync(); Assert.Null(row.MicrochipIds); save.Remove(row);
            ObserveMarketplaceAuctionWrite(save, manager, null);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => save.SaveChangesAsync()); Assert.Equal(1, writes);
        }
        using var read = new CharacterContext(Configuration); Assert.Equal("81,82", Assert.Single(read.MarketplaceAuction).MicrochipIds);
    }

    private static void ObserveMarketplaceCommodityWrite(CharacterContext context, GlobalMarketplaceManager manager, MarketplaceCommodityOrderModel target)
    {
        var records = (System.Collections.IEnumerable)typeof(GlobalMarketplaceManager)
            .GetField("commodityOrders", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(manager)!;
        object record = Assert.Single(records.Cast<object>());
        typeof(GlobalMarketplaceManager).GetMethod("ObserveCommodityRow", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [context, record, target]);
    }

    private sealed class BeforeListingTransaction(Action write) : DbTransactionInterceptor
    {
        private int entered;
        public override InterceptionResult<DbTransaction> TransactionStarting(DbConnection connection,
            TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result)
        {
            if (Interlocked.Exchange(ref entered, 1) == 0) write();
            return result;
        }
        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection,
            TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
            => new(TransactionStarting(connection, eventData, result));
    }
}
