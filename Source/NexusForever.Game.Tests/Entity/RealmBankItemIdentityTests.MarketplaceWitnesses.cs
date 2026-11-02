using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Fact]
    public async Task MarketplaceStoredAuctionLegacyCsvAndRepeatedBidsAdvanceTheirPreimages()
    {
        var f = await MailFixture(); var manager = await StoredMarketplaceAuction(f, 200);
        using (var edit = new CharacterContext(Configuration))
        {
            var row = await edit.MarketplaceAuction.SingleAsync(); row.MicrochipIds = "81, 82";
            await edit.SaveChangesAsync();
        }
        manager.Initialise();
        Assert.Equal(GenericError.Ok, manager.BuyAuction(f.Player, StoredMarketplaceBid(10), out var first));
        Assert.Equal(new uint[] { 81, 82 }, first.MicrochipIds);
        Assert.Equal(GenericError.Ok, manager.BuyAuction(f.Player, StoredMarketplaceBid(11), out var second));
        Assert.Equal(new uint[] { 81, 82 }, second.MicrochipIds);
        Assert.Equal(89ul, Assert.Single(f.Currency).Amount);
        using var read = new CharacterContext(Configuration); var stored = Assert.Single(read.MarketplaceAuction);
        Assert.Equal(11ul, stored.CurrentBid); Assert.Equal(100ul, stored.TopBidderCharacterId);
        Assert.Equal("81,82", stored.MicrochipIds);
    }

    [Fact]
    public async Task MarketplaceStoredAuctionRejectsAReentrantBidBeforeTouchingItsWallet()
    {
        var f = await MailFixture(); var manager = await StoredMarketplaceAuction(f, 200);
        var competing = RecordingDispatchProxy<IPlayer>.Create(out var cp);
        var competitorCurrency = RecordingDispatchProxy<ICurrencyManager>.Create(out var competitorWallet);
        competitorWallet.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
        cp.SetProperty(nameof(IPlayer.CharacterId), 300ul); cp.SetProperty(nameof(IPlayer.CurrencyManager), competitorCurrency);
        cp.SetProperty(nameof(IPlayer.Inventory), RecordingDispatchProxy<IInventory>.Create(out _));
        var currency = RecordingDispatchProxy<ICurrencyManager>.Create(out var wallet);
        wallet.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
        GenericError nested = GenericError.Ok; ulong balance = 100;
        wallet.SetMethodHandler(nameof(ICurrencyManager.CurrencySubtractAmount), args =>
        {
            balance -= (ulong)args[1];
            nested = manager.BuyAuction(competing, StoredMarketplaceBid(20), out _); return null;
        });
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.CurrencyManager), currency);
        Assert.Equal(GenericError.Ok, manager.BuyAuction(f.Player, StoredMarketplaceBid(10), out var accepted));
        Assert.Equal(GenericError.DbFailure, nested); Assert.Equal(90ul, balance); Assert.Equal(10ul, accepted.CurrentBid);
        Assert.Empty(competitorWallet.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        using var read = new CharacterContext(Configuration); var stored = Assert.Single(read.MarketplaceAuction);
        Assert.Equal(10ul, stored.CurrentBid); Assert.Equal(100ul, stored.TopBidderCharacterId);
    }

    [Theory]
    [InlineData("unchanged", null)]
    [InlineData(nameof(MarketplaceAuctionModel.OwnerCharacterId), 100ul)]
    [InlineData(nameof(MarketplaceAuctionModel.ItemId), 901ul)]
    [InlineData(nameof(MarketplaceAuctionModel.MinimumBid), 11ul)]
    [InlineData(nameof(MarketplaceAuctionModel.BuyoutPrice), 90ul)]
    [InlineData(nameof(MarketplaceAuctionModel.CurrentBid), 12ul)]
    [InlineData(nameof(MarketplaceAuctionModel.TopBidderCharacterId), 200ul)]
    [InlineData(nameof(MarketplaceAuctionModel.ExpirationTime), 1ul)]
    [InlineData(nameof(MarketplaceAuctionModel.Item2Id), 7002u)]
    [InlineData(nameof(MarketplaceAuctionModel.Quantity), 2u)]
    [InlineData(nameof(MarketplaceAuctionModel.WorldRequirementItem2Id), 7003u)]
    [InlineData(nameof(MarketplaceAuctionModel.CircuitData), 3ul)]
    [InlineData(nameof(MarketplaceAuctionModel.GlyphData), 4u)]
    [InlineData(nameof(MarketplaceAuctionModel.ThresholdData), 5ul)]
    [InlineData(nameof(MarketplaceAuctionModel.Unknown2), 6u)]
    [InlineData(nameof(MarketplaceAuctionModel.MicrochipIds), "81,82")]
    public async Task MarketplaceStoredAuctionBidRequiresTheCompleteLoadedRow(string field, object changed)
    {
        var f = await MailFixture(); var manager = await StoredMarketplaceAuction(f, 200);
        using (var edit = new CharacterContext(Configuration))
        {
            if (field == nameof(MarketplaceAuctionModel.ItemId))
            {
                var alternate = CharacterRow(); alternate.Id = 901; alternate.OwnerId = null;
                alternate.Location = (ushort)InventoryLocation.None; alternate.Soulbound = false;
                edit.Item.Add(alternate); await edit.SaveChangesAsync();
            }
            if (field != "unchanged")
            {
                var row = await edit.MarketplaceAuction.SingleAsync(); edit.Entry(row).Property(field).CurrentValue = changed;
                await edit.SaveChangesAsync();
            }
        }
        Assert.Equal(field == "unchanged" ? GenericError.Ok : GenericError.DbFailure,
            manager.BuyAuction(f.Player, StoredMarketplaceBid(10), out var auction));
        Assert.Equal(field == "unchanged" ? 90ul : 100ul, Assert.Single(f.Currency).Amount);
        Assert.Equal(field == "unchanged" ? 10ul : 0ul, auction.CurrentBid);
        using var read = new CharacterContext(Configuration); var stored = Assert.Single(read.MarketplaceAuction);
        if (field != "unchanged") Assert.Equal(changed, read.Entry(stored).Property(field).CurrentValue);
        Assert.Empty(read.CharacterMail); Assert.Empty(read.CharacterMailAttachment);
    }

    [Theory]
    [InlineData("unchanged", null)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.OwnerCharacterId), 200ul)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.Item2Id), 7002u)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.Quantity), 3u)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.PricePerUnit), 11ul)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.Price), 30ul)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.IsBuyOrder), false)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.ForceImmediate), true)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.ListTime), 1ul)]
    [InlineData(nameof(MarketplaceCommodityOrderModel.ExpirationTime), 2ul)]
    public async Task MarketplaceStoredCommodityRefundRequiresTheCompleteLoadedRow(string field, object changed)
    {
        var f = await MailFixture();
        using (var seed = new CharacterContext(Configuration))
        {
            seed.MarketplaceCommodityOrder.Add(new MarketplaceCommodityOrderModel {
                Id = 1, OwnerCharacterId = 100, Item2Id = 7001, Quantity = 2, PricePerUnit = 10, Price = 20, IsBuyOrder = true });
            await seed.SaveChangesAsync();
        }
        var manager = StoredMarketplace(f); manager.Initialise();
        if (field != "unchanged")
        {
            using var edit = new CharacterContext(Configuration); var row = await edit.MarketplaceCommodityOrder.SingleAsync();
            edit.Entry(row).Property(field).CurrentValue = changed; await edit.SaveChangesAsync();
        }
        Assert.Equal(field == "unchanged" ? GenericError.Ok : GenericError.DbFailure,
            manager.CancelCommodityOrder(f.Player, 1, 7001, true, out _));
        Assert.Equal(field == "unchanged" ? 120ul : 100ul, Assert.Single(f.Currency).Amount);
        Assert.Equal(field == "unchanged" ? 0 : 1, manager.GetOwnedCommodityOrders(f.Player).Count);
        using var read = new CharacterContext(Configuration);
        if (field == "unchanged") Assert.Empty(read.MarketplaceCommodityOrder);
        else Assert.Equal(changed, read.Entry(Assert.Single(read.MarketplaceCommodityOrder)).Property(field).CurrentValue);
        Assert.Empty(read.CharacterMail); Assert.Empty(read.CharacterMailAttachment);
    }
}
