using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Static;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task MarketplaceLoadedRowsRejectAnIdenticalRowInAnotherDatabase(bool otherRoute, bool commodity)
    {
        var f = await MailFixture(); await StoredMarketplaceAuction(f, 200);
        using (var seed = new CharacterContext(Configuration))
        {
            seed.MarketplaceCommodityOrder.Add(new MarketplaceCommodityOrderModel {
                Id = 1, OwnerCharacterId = 100, Item2Id = 7001, Quantity = 2, PricePerUnit = 10, Price = 20, IsBuyOrder = true });
            await seed.SaveChangesAsync();
        }
        using var other = new RealmBankItemIdentityTests(); await other.MailFixture();
        using (var read = new CharacterContext(Configuration))
        using (var copy = new CharacterContext(other.Configuration))
        {
            var item = await read.Item.SingleAsync(); var auction = await read.MarketplaceAuction.SingleAsync();
            var order = await read.MarketplaceCommodityOrder.SingleAsync();
            copy.Item.Add((ItemModel)read.Entry(item).CurrentValues.ToObject());
            copy.MarketplaceAuction.Add((MarketplaceAuctionModel)read.Entry(auction).CurrentValues.ToObject());
            copy.MarketplaceCommodityOrder.Add((MarketplaceCommodityOrderModel)read.Entry(order).CurrentValues.ToObject());
            await copy.SaveChangesAsync();
        }
        CharacterDatabase selected = Database();
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dp);
        dp.SetMethodReturnFactory(nameof(IDatabaseManager.GetDatabase), () => selected);
        var manager = new GlobalMarketplaceManager(databases, f.Tables, MarketplaceMailAssets(f), f.Players, f.Items);
        manager.Initialise(); if (otherRoute) selected = other.Database();
        var result = commodity
            ? manager.CancelCommodityOrder(f.Player, 1, 7001, true, out _)
            : manager.BuyAuction(f.Player, StoredMarketplaceBid(10), out _);
        Assert.Equal(otherRoute ? GenericError.DbFailure : GenericError.Ok, result);
        Assert.Equal(otherRoute ? 100ul : commodity ? 120ul : 90ul, Assert.Single(f.Currency).Amount);
        using var primary = new CharacterContext(Configuration); using var secondary = new CharacterContext(other.Configuration);
        Assert.Equal(!otherRoute && !commodity ? 10ul : 0ul, Assert.Single(primary.MarketplaceAuction).CurrentBid);
        Assert.Equal(0ul, Assert.Single(secondary.MarketplaceAuction).CurrentBid);
        Assert.Equal(!otherRoute && commodity ? 0 : 1, primary.MarketplaceCommodityOrder.Count());
        Assert.Single(secondary.MarketplaceCommodityOrder);
        Assert.Empty(primary.CharacterMail); Assert.Empty(secondary.CharacterMail);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MarketplaceLoadedRowsAcceptEquivalentConnectionKeyOrdering(bool commodity)
    {
        var f = await MailFixture(); await StoredMarketplaceAuction(f, 200);
        using (var seed = new CharacterContext(Configuration))
        {
            seed.MarketplaceCommodityOrder.Add(new MarketplaceCommodityOrderModel {
                Id = 1, OwnerCharacterId = 100, Item2Id = 7001, Quantity = 2, PricePerUnit = 10, Price = 20, IsBuyOrder = true });
            await seed.SaveChangesAsync();
        }
        var database = Database(); var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dp);
        dp.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var manager = new GlobalMarketplaceManager(databases, f.Tables, MarketplaceMailAssets(f), f.Players, f.Items); manager.Initialise();
        database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Pooling=False;Data Source={path}" });
        Assert.Equal(GenericError.Ok, commodity ? manager.CancelCommodityOrder(f.Player, 1, 7001, true, out _)
            : manager.BuyAuction(f.Player, StoredMarketplaceBid(10), out _));
        Assert.Equal(commodity ? 120ul : 90ul, Assert.Single(f.Currency).Amount);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MarketplaceInsertedRemainderRetainsItsOriginalDatabase(bool otherRoute)
    {
        var f = await MailFixture(); await StoredMarketplaceSellOrder(f);
        CharacterDatabase selected = Database(); var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dp);
        dp.SetMethodReturnFactory(nameof(IDatabaseManager.GetDatabase), () => selected);
        var manager = new GlobalMarketplaceManager(databases, f.Tables, MarketplaceMailAssets(f), f.Players, f.Items); manager.Initialise();
        Assert.Equal(GenericError.Ok, manager.PostCommodityOrder(f.Player, new NexusForever.Network.World.Message.Model.Marketplace.CommodityOrder {
            Item2Id = 7001, IsBuyOrder = true, Quantity = 3, PricePerUnit = 10, Price = 30 }, out _, f.Items));
        var remainder = Assert.Single(manager.GetOwnedCommodityOrders(f.Player)); Assert.Equal(1u, remainder.Quantity);
        using var other = new RealmBankItemIdentityTests(); await other.MailFixture();
        using (var read = new CharacterContext(Configuration))
        using (var copy = new CharacterContext(other.Configuration))
        {
            var row = await read.MarketplaceCommodityOrder.SingleAsync();
            copy.MarketplaceCommodityOrder.Add((MarketplaceCommodityOrderModel)read.Entry(row).CurrentValues.ToObject());
            await copy.SaveChangesAsync();
        }
        if (otherRoute) selected = other.Database();
        Assert.Equal(otherRoute ? GenericError.DbFailure : GenericError.Ok,
            manager.CancelCommodityOrder(f.Player, remainder.CommodityOrderId, 7001, true, out _));
        Assert.Equal(otherRoute ? 70ul : 80ul, Assert.Single(f.Currency).Amount);
        using var primary = new CharacterContext(Configuration); using var secondary = new CharacterContext(other.Configuration);
        Assert.Equal(otherRoute ? 1 : 0, primary.MarketplaceCommodityOrder.Count()); Assert.Single(secondary.MarketplaceCommodityOrder);
        Assert.Single(primary.CharacterMail); Assert.Empty(secondary.CharacterMail);
    }

    [Fact]
    public async Task MarketplaceReloadPreservesTheIdHighWaterMarkOfAnOrphanedAuction()
    {
        var f = await MailFixture(); await StoredMarketplaceAuction(f, 200);
        var row = CharacterRow(); row.Id = 901; row.StackCount = 1; row.Soulbound = false;
        using (var seed = new CharacterContext(Configuration))
        {
            await seed.Database.OpenConnectionAsync(); await seed.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=OFF;");
            seed.Item.Add(row);
            seed.MarketplaceAuction.Add(new MarketplaceAuctionModel { Id = 50, ItemId = 99999, OwnerCharacterId = 200,
                Item2Id = 7001, Quantity = 1, MinimumBid = 10, MicrochipIds = string.Empty,
                ExpirationTime = (ulong)DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() });
            await seed.SaveChangesAsync();
        }
        var item = new GameItem(row, f.Items) { Location = InventoryLocation.Inventory };
        var inventory = RecordingDispatchProxy<IInventory>.Create(out var ip);
        ip.SetMethodHandler(nameof(IInventory.ItemRemove), _ => { item.CharacterId = null; item.Location = InventoryLocation.None; return null; });
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.Inventory), inventory);
        var manager = StoredMarketplace(f); manager.Initialise();
        Assert.Equal(GenericError.Ok, manager.PostAuction(f.Player, item, 10, 20, out var posted));
        Assert.Equal(51ul, posted.AuctionId);
        using var read = new CharacterContext(Configuration); Assert.Equal(3, read.MarketplaceAuction.Count());
    }
}

