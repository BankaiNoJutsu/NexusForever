using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Abstract.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MarketplaceStoredAuctionBidRequiresItsSqlRow(bool missing)
    {
        var f = await MailFixture(); var manager = await StoredMarketplaceAuction(f, 200);
        if (missing)
        {
            using var edit = new CharacterContext(Configuration);
            Assert.Equal(1, await edit.MarketplaceAuction.Where(row => row.Id == 1).ExecuteDeleteAsync());
        }
        var result = manager.BuyAuction(f.Player, StoredMarketplaceBid(10), out var auction);
        Assert.Equal(missing ? GenericError.DbFailure : GenericError.Ok, result);
        Assert.Equal(missing ? 100ul : 90ul, Assert.Single(f.Currency).Amount);
        Assert.Equal(missing ? 0ul : 10ul, auction.CurrentBid);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMail); Assert.Empty(read.CharacterMailAttachment);
        Assert.Null(Assert.Single(read.Item).OwnerId);
        if (missing) Assert.Empty(read.MarketplaceAuction);
        else Assert.Equal(10ul, Assert.Single(read.MarketplaceAuction).CurrentBid);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MarketplaceStoredCommodityCancellationRequiresItsSqlRowBeforeRefund(bool missing)
    {
        var f = await MailFixture();
        using (var seed = new CharacterContext(Configuration))
        {
            seed.MarketplaceCommodityOrder.Add(new MarketplaceCommodityOrderModel {
                Id = 1, OwnerCharacterId = 100, Item2Id = 7001, Quantity = 2, PricePerUnit = 10, Price = 20, IsBuyOrder = true });
            await seed.SaveChangesAsync();
        }
        var manager = StoredMarketplace(f); manager.Initialise();
        Assert.Single(manager.GetOwnedCommodityOrders(f.Player));
        if (missing)
        {
            using var edit = new CharacterContext(Configuration);
            Assert.Equal(1, await edit.MarketplaceCommodityOrder.Where(row => row.Id == 1).ExecuteDeleteAsync());
        }
        var result = manager.CancelCommodityOrder(f.Player, 1, 7001, true, out _);
        Assert.Equal(missing ? GenericError.DbFailure : GenericError.Ok, result);
        Assert.Equal(missing ? 100ul : 120ul, Assert.Single(f.Currency).Amount);
        Assert.Equal(missing ? 1 : 0, manager.GetOwnedCommodityOrders(f.Player).Count);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.MarketplaceCommodityOrder); Assert.Empty(read.CharacterMail);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task MarketplaceStoredAuctionCancellationRequiresItsSqlRowBeforeInventoryOrMailReturn(bool missing, bool byMail)
    {
        var f = await MailFixture();
        var inventory = RecordingDispatchProxy<IInventory>.Create(out var ip);
        ip.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), byMail ? 0u : 1u);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.Inventory), inventory);
        var manager = await StoredMarketplaceAuction(f, 100);
        Assert.Single(manager.GetOwnedItemAuctions(f.Player));
        if (missing)
        {
            using var edit = new CharacterContext(Configuration);
            Assert.Equal(1, await edit.MarketplaceAuction.Where(row => row.Id == 1).ExecuteDeleteAsync());
        }
        var result = manager.CancelAuction(f.Player, 1, 7001, out _);
        Assert.Equal(missing ? (byMail ? GenericError.ItemInventoryFull : GenericError.DbFailure) : GenericError.Ok, result);
        Assert.Equal(missing ? 1 : 0, manager.GetOwnedItemAuctions(f.Player).Count);
        Assert.Equal(!missing && !byMail ? 1 : 0, ip.GetInvocations(nameof(IInventory.AddItem)).Count);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.MarketplaceAuction);
        Assert.Equal(!missing && byMail ? 1 : 0, read.CharacterMail.Count());
        Assert.Equal(!missing && byMail ? 1 : 0, read.CharacterMailAttachment.Count());
        Assert.Equal(missing ? null : 100ul, Assert.Single(read.Item).OwnerId);
    }

    [Theory]
    [InlineData(false, 1u)] [InlineData(true, 1u)]
    [InlineData(false, 2u)] [InlineData(true, 2u)]
    public async Task MarketplaceStoredCommodityFillRequiresTheRestingSqlRow(bool missing, uint quantity)
    {
        var f = await MailFixture(); var (manager, inventory) = await StoredMarketplaceSellOrder(f);
        if (missing)
        {
            using var edit = new CharacterContext(Configuration);
            Assert.Equal(1, await edit.MarketplaceCommodityOrder.Where(row => row.Id == 1).ExecuteDeleteAsync());
        }
        var result = manager.PostCommodityOrder(f.Player, new CommodityOrder {
            Item2Id = 7001, IsBuyOrder = true, ForceImmediate = true,
            Quantity = quantity, PricePerUnit = 10, Price = 10ul * quantity }, out var posted, f.Items);
        Assert.Equal(GenericError.Ok, result); Assert.Equal(0u, posted.Quantity);
        Assert.Equal(missing ? 100ul : 100ul - 10ul * quantity, Assert.Single(f.Currency).Amount);
        Assert.Equal(missing ? 0 : 1, inventory.GetInvocations(nameof(IInventory.ItemCreate)).Count);
        var remaining = manager.BuildCommodityInfo(7001).Orders;
        if (missing) Assert.Equal(2u, Assert.Single(remaining).Quantity);
        else if (quantity == 1) Assert.Equal(1u, Assert.Single(remaining).Quantity);
        else Assert.Empty(remaining);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(missing ? 0 : 1, read.CharacterMail.Count());
        if (missing || quantity == 2) Assert.Empty(read.MarketplaceCommodityOrder);
        else Assert.Equal(1u, Assert.Single(read.MarketplaceCommodityOrder).Quantity);
    }

    [Theory]
    [InlineData(2u)] [InlineData(3u)]
    public async Task MarketplaceStoredCommodityNewIncomingMatchPersistsOnlyRemainingOrder(uint quantity)
    {
        var f = await MailFixture(); var (manager, inventory) = await StoredMarketplaceSellOrder(f);
        Assert.Equal(GenericError.Ok, manager.PostCommodityOrder(f.Player, new CommodityOrder {
            Item2Id = 7001, IsBuyOrder = true, Quantity = quantity,
            PricePerUnit = 10, Price = 10ul * quantity }, out _, f.Items));
        Assert.Single(inventory.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(100ul - 10ul * quantity, Assert.Single(f.Currency).Amount);
        using var read = new CharacterContext(Configuration);
        Assert.Single(read.CharacterMail); // Offline seller proceeds share the fill write.
        if (quantity == 2) { Assert.Empty(read.MarketplaceCommodityOrder); Assert.Empty(manager.BuildCommodityInfo(7001).Orders); }
        else
        {
            var stored = Assert.Single(read.MarketplaceCommodityOrder);
            Assert.Equal(100ul, stored.OwnerCharacterId); Assert.Equal(1u, stored.Quantity); Assert.Equal(10ul, stored.Price);
            Assert.Equal(1u, Assert.Single(manager.GetOwnedCommodityOrders(f.Player)).Quantity);
        }
    }

    private async Task<(GlobalMarketplaceManager Manager, RecordingDispatchProxy<IInventory> Inventory)> StoredMarketplaceSellOrder(MailState f)
    {
        using (var seed = new CharacterContext(Configuration))
        {
            seed.MarketplaceCommodityOrder.Add(new MarketplaceCommodityOrderModel {
                Id = 1, OwnerCharacterId = 200, Item2Id = 7001, Quantity = 2, PricePerUnit = 10, Price = 20, IsBuyOrder = false });
            await seed.SaveChangesAsync();
        }
        var inventory = RecordingDispatchProxy<IInventory>.Create(out var ip);
        ip.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 2u);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.Inventory), inventory);
        ((RecordingDispatchProxy<IPlayerManager>)(object)f.Players).SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] == 100 ? f.Player : null);
        var manager = StoredMarketplace(f); manager.Initialise();
        Assert.Equal(2u, Assert.Single(manager.BuildCommodityInfo(7001).Orders).Quantity);
        return (manager, ip);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MarketplaceStoredIncomingRemainderRequiresItsInsertedRowOnTheNextFill(bool missing)
    {
        var f = await MailFixture(); var (manager, buyerInventory) = await StoredMarketplaceSellOrder(f);
        Assert.Equal(GenericError.Ok, manager.PostCommodityOrder(f.Player, new CommodityOrder {
            Item2Id = 7001, IsBuyOrder = true, Quantity = 3, PricePerUnit = 10, Price = 30 }, out _, f.Items));
        var remaining = Assert.Single(manager.GetOwnedCommodityOrders(f.Player)); Assert.Equal(1u, remaining.Quantity);
        Assert.Single(buyerInventory.GetInvocations(nameof(IInventory.ItemCreate)));
        if (missing)
        {
            using var edit = new CharacterContext(Configuration);
            Assert.Equal(1, await edit.MarketplaceCommodityOrder.Where(row => row.Id == remaining.CommodityOrderId).ExecuteDeleteAsync());
        }
        var seller = RecordingDispatchProxy<IPlayer>.Create(out var sp);
        var inventory = RecordingDispatchProxy<IInventory>.Create(out var ip);
        ip.SetMethodReturn(nameof(IInventory.HasItemCount), true); ip.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 2u);
        sp.SetProperty(nameof(IPlayer.CharacterId), 200ul); sp.SetProperty(nameof(IPlayer.Inventory), inventory);
        Assert.Equal(GenericError.Ok, manager.PostCommodityOrder(seller, new CommodityOrder {
            Item2Id = 7001, IsBuyOrder = false, ForceImmediate = true,
            Quantity = 1, PricePerUnit = 10, Price = 10 }, out var completed, f.Items));
        Assert.Equal(0u, completed.Quantity);
        Assert.Equal(missing ? 1 : 2, buyerInventory.GetInvocations(nameof(IInventory.ItemCreate)).Count);
        Assert.Equal(missing ? 1 : 0, ip.GetInvocations(nameof(IInventory.ItemCreate)).Count);
        Assert.Equal(missing ? 1 : 0, manager.GetOwnedCommodityOrders(f.Player).Count);
        Assert.Equal(70ul, Assert.Single(f.Currency).Amount);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.MarketplaceCommodityOrder);
        Assert.Equal(missing ? 1 : 2, read.CharacterMail.Count());
    }

    private GlobalMarketplaceManager StoredMarketplace(MailState f)
    {
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dp);
        dp.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), Database());
        MarketplaceMailAssets(f); ulong nextMail = 1000;
        var assets = RecordingDispatchProxy<IAssetManager>.Create(out var ap);
        ap.SetMethodReturnFactory("get_NextMailId", () => nextMail++);
        return new GlobalMarketplaceManager(databases, f.Tables, assets, f.Players, f.Items);
    }
    [Theory]
    [InlineData("none", false)] [InlineData("missing", false)] [InlineData("delete", false)]
    [InlineData("none", true)] [InlineData("missing", true)] [InlineData("delete", true)]
    public async Task MarketplaceStoredUnbidExpiryRequiresItsSqlDeletionBeforeReturnOrRemoval(string fault, bool byMail)
    {
        var f = await MailFixture(); PublicationRecipient(f);
        var inventory = RecordingDispatchProxy<IInventory>.Create(out var ip);
        ip.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), byMail ? 0u : 1u);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.Inventory), inventory);
        var manager = await StoredMarketplaceAuction(f, 100, expired: true);
        Assert.Single(manager.GetOwnedItemAuctions(f.Player));
        using (var edit = new CharacterContext(Configuration))
        {
            if (fault == "missing") Assert.Equal(1, await edit.MarketplaceAuction.Where(row => row.Id == 1).ExecuteDeleteAsync());
            if (fault == "delete") await edit.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_expired_auction BEFORE DELETE ON marketplace_auction BEGIN SELECT RAISE(ABORT, 'Rejected expiry deletion'); END;");
        }
        manager.Update(1);
        bool failed = fault != "none";
        Assert.Equal(failed ? 1 : 0, manager.GetOwnedItemAuctions(f.Player).Count);
        Assert.Equal(!failed && !byMail ? 1 : 0, ip.GetInvocations(nameof(IInventory.AddItem)).Count);
        Assert.Equal(failed ? 0 : 1, BankMessages(f.Player.Session).OfType<ServerAuctionExpired>().Count());
        using var read = new CharacterContext(Configuration);
        Assert.Equal(fault == "delete" ? 1 : 0, read.MarketplaceAuction.Count());
        Assert.Equal(!failed && byMail ? 1 : 0, read.CharacterMail.Count());
        Assert.Equal(!failed && byMail ? 1 : 0, read.CharacterMailAttachment.Count());
        Assert.Equal(failed ? null : 100ul, Assert.Single(read.Item).OwnerId);
    }

    [Theory]
    [InlineData("none", false)] [InlineData("missing", false)] [InlineData("delete", false)]
    [InlineData("none", true)] [InlineData("missing", true)] [InlineData("delete", true)]
    public async Task MarketplaceStoredFallbackSaleRequiresSqlBeforeASlotBecomesAvailable(string fault, bool expiry)
    {
        var f = await MailFixture();
        var inventory = RecordingDispatchProxy<IInventory>.Create(out var ip); int slotReads = 0;
        ip.SetMethodReturnFactory(nameof(IInventory.GetInventorySlotsRemaining), () => ++slotReads <= (expiry ? 1 : 2) ? 0u : 1u);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.Inventory), inventory);
        ((RecordingDispatchProxy<IPlayerManager>)(object)f.Players).SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] == 100 ? f.Player : null);
        var manager = await StoredMarketplaceAuction(f, 200, expired: expiry, bid: expiry, buyout: !expiry);
        using (var edit = new CharacterContext(Configuration))
        {
            if (fault == "missing") Assert.Equal(1, await edit.MarketplaceAuction.Where(row => row.Id == 1).ExecuteDeleteAsync());
            if (fault == "delete") await edit.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_fallback_auction BEFORE DELETE ON marketplace_auction BEGIN SELECT RAISE(ABORT, 'Rejected fallback deletion'); END;");
        }
        bool failed = fault != "none";
        if (expiry) manager.Update(1);
        else Assert.Equal(failed ? GenericError.ItemInventoryFull : GenericError.Ok, manager.BuyAuction(f.Player, StoredMarketplaceBid(20), out _));
        Assert.Equal(failed ? 0 : 1, ip.GetInvocations(nameof(IInventory.AddItem)).Count);
        Assert.Equal(failed ? 0 : 1, BankMessages(f.Player.Session).OfType<ServerAuctionWon>().Count());
        Assert.Equal(failed || expiry ? 100ul : 80ul, Assert.Single(f.Currency).Amount);
        var seller = RecordingDispatchProxy<IPlayer>.Create(out var sellerProxy); sellerProxy.SetProperty(nameof(IPlayer.CharacterId), 200ul);
        Assert.Equal(failed ? 1 : 0, manager.GetOwnedItemAuctions(seller).Count);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(fault == "delete" ? 1 : 0, read.MarketplaceAuction.Count());
        Assert.Equal(failed ? 0 : 1, read.CharacterMail.Count());
        Assert.Equal(failed ? null : 100ul, Assert.Single(read.Item).OwnerId);
    }

    private static ClientAuctionBuyOrderSubmit StoredMarketplaceBid(ulong amount)
    {
        using var body = new MemoryStream();
        using (var writer = new GamePacketWriter(body)) { writer.Write(7001u, 18u); writer.Write(1ul); writer.Write(amount); writer.FlushBits(); }
        using var reader = new GamePacketReader(new MemoryStream(body.ToArray()));
        var request = new ClientAuctionBuyOrderSubmit(); request.Read(reader); return request;
    }

    [Theory]
    [InlineData("none", 1u)] [InlineData("missing", 1u)] [InlineData("write", 1u)]
    [InlineData("none", 2u)] [InlineData("missing", 2u)] [InlineData("write", 2u)]
    public async Task MarketplaceStoredCommodityFallbackKeepsItsFillWriteWhenSpaceBecomesAvailable(string fault, uint quantity)
    {
        var f = await MailFixture(); var (manager, inventory) = await StoredMarketplaceSellOrder(f); int reads = 0;
        inventory.SetMethodReturnFactory(nameof(IInventory.GetInventorySlotsRemaining), () => ++reads == 1 ? 0u : 2u);
        using (var edit = new CharacterContext(Configuration))
        {
            if (fault == "missing") Assert.Equal(1, await edit.MarketplaceCommodityOrder.Where(row => row.Id == 1).ExecuteDeleteAsync());
            if (fault == "write")
                await edit.Database.ExecuteSqlRawAsync(quantity == 1
                    ? "CREATE TRIGGER reject_fallback_order BEFORE UPDATE ON marketplace_commodity_order BEGIN SELECT RAISE(ABORT, 'Rejected fallback fill'); END;"
                    : "CREATE TRIGGER reject_fallback_order BEFORE DELETE ON marketplace_commodity_order BEGIN SELECT RAISE(ABORT, 'Rejected fallback fill'); END;");
        }
        Assert.Equal(GenericError.Ok, manager.PostCommodityOrder(f.Player, new CommodityOrder {
            Item2Id = 7001, IsBuyOrder = true, ForceImmediate = true,
            Quantity = quantity, PricePerUnit = 10, Price = 10ul * quantity }, out var posted, f.Items));
        bool failed = fault != "none";
        Assert.Equal(0u, posted.Quantity);
        Assert.Equal(failed ? 100ul : 100ul - 10ul * quantity, Assert.Single(f.Currency).Amount);
        using var read = new CharacterContext(Configuration);
        int itemMail = read.CharacterMail.Count(row => row.RecipientId == 100);
        Assert.Equal(failed ? 0 : 1, itemMail + inventory.GetInvocations(nameof(IInventory.ItemCreate)).Count);
        Assert.Equal(failed ? 0 : 1, read.CharacterMail.Count(row => row.RecipientId == 200));
        Assert.Equal(itemMail, read.CharacterMailAttachment.Count());
        var book = manager.BuildCommodityInfo(7001).Orders;
        if (failed) Assert.Equal(2u, Assert.Single(book).Quantity);
        else if (quantity == 1) Assert.Equal(1u, Assert.Single(book).Quantity);
        else Assert.Empty(book);
        if (fault == "write") Assert.Equal(2u, Assert.Single(read.MarketplaceCommodityOrder).Quantity);
        else if (fault == "missing" || quantity == 2) Assert.Empty(read.MarketplaceCommodityOrder);
        else Assert.Equal(1u, Assert.Single(read.MarketplaceCommodityOrder).Quantity);
    }

    private async Task<GlobalMarketplaceManager> StoredMarketplaceAuction(MailState f, ulong owner, bool expired = false, bool bid = false, bool buyout = false)
    {
        var item = CharacterRow(); item.Id = 900; item.OwnerId = null; item.Location = (ushort)InventoryLocation.None; item.Soulbound = false; item.StackCount = 1;
        using (var seed = new CharacterContext(Configuration))
        {
            seed.Item.Add(item);
            seed.MarketplaceAuction.Add(new MarketplaceAuctionModel {
                Id = 1, OwnerCharacterId = owner, ItemId = item.Id, MinimumBid = 10,
                CurrentBid = bid ? 10ul : 0ul, TopBidderCharacterId = bid ? 100ul : 0ul, BuyoutPrice = buyout ? 20ul : 0ul,
                ExpirationTime = (ulong)DateTimeOffset.UtcNow.AddSeconds(expired ? -60 : 3600).ToUnixTimeSeconds(),
                Item2Id = 7001, Quantity = 1, MicrochipIds = string.Empty });
            await seed.SaveChangesAsync();
        }
        var manager = StoredMarketplace(f); manager.Initialise(); return manager;
    }
}
