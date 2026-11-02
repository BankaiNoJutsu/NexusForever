using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MarketplaceMailedCommodityCanBeCollectedByTheConcreteInventory(bool returned)
    {
        var f = await MailFixture(); Assert.True(ProduceMarketplaceCommodityMail(f, returned));
        var (manager, mail) = LoadMarketplaceMailbox(f);
        manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        using var stored = new CharacterContext(Configuration);
        Assert.Empty(stored.CharacterMailAttachment); var item = Assert.Single(stored.Item);
        Assert.Equal(100ul, item.OwnerId); Assert.Equal((ushort)InventoryLocation.Inventory, item.Location);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MarketplaceMailedCommodityCommitsTheLegacyDetachedCustodyShape(bool returned)
    {
        var f = await MailFixture(); Assert.True(ProduceMarketplaceCommodityMail(f, returned));
        using var read = new CharacterContext(Configuration);
        var item = Assert.Single(read.Item); Assert.Equal(100ul, item.OwnerId);
        Assert.Equal((ushort)InventoryLocation.None, item.Location);
        Assert.Equal(item.Id, Assert.Single(read.CharacterMailAttachment).ItemGuid);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MarketplaceMailedCommodityCommittedControlPersistsOneHeaderItemAndLink(bool returned)
    {
        var f = await MailFixture(); Assert.True(ProduceMarketplaceCommodityMail(f, returned));
        using var read = new CharacterContext(Configuration);
        var mail = Assert.Single(read.CharacterMail); var item = Assert.Single(read.Item); var link = Assert.Single(read.CharacterMailAttachment);
        Assert.Equal(1000ul, mail.Id); Assert.Equal(100ul, mail.RecipientId); Assert.Equal((byte)SenderType.CommodityAuction, mail.SenderType);
        Assert.Equal((byte)(returned ? ContentType.AuctionExpired : ContentType.AuctionWon), mail.ContentType);
        Assert.Equal(900ul, item.Id); Assert.Equal(7001u, item.ItemId); Assert.Equal(1u, item.StackCount);
        Assert.Equal(mail.Id, link.Id); Assert.Equal(item.Id, link.ItemGuid);
    }

    private IAssetManager MarketplaceMailAssets(MailState f, ulong mailId = 1000, ulong itemId = 900)
    {
        ulong nextItem = itemId;
        ((RecordingDispatchProxy<IItemManager>)(object)f.Items).SetMethodReturnFactory("get_NextItemId", () => nextItem++);
        var assets = RecordingDispatchProxy<IAssetManager>.Create(out var ap); ap.SetProperty(nameof(IAssetManager.NextMailId), mailId);
        return assets;
    }
    private bool ProduceMarketplaceCommodityMail(MailState f, bool returned, ulong mailId = 1000, ulong itemId = 900)
    {
        var assets = MarketplaceMailAssets(f, mailId, itemId);
        return returned
            ? MarketplaceMailDelivery.TrySendCommodityAuctionReturnMail(Database(), f.Tables, assets, 100, 7001, 1, itemManager: f.Items)
            : MarketplaceMailDelivery.TrySendCommodityAuctionFillMail(Database(), f.Tables, assets, 100, 7001, 1, itemManager: f.Items);
    }

    private MailManager LoadMarketplaceMailManager(MailState f)
    {
        using var read = new CharacterContext(Configuration); var model = new CharacterModel();
        foreach (var row in read.CharacterMail.AsNoTracking().Include(mail => mail.Attachment).ThenInclude(link => link.Item).AsEnumerable().OrderBy(mail => mail.Id)) model.Mail.Add(row);
        var manager = new MailManager(f.Player, model, realmId: 2, itemManager: f.Items,
            gameTableManager: f.Tables, deliveryContexts: Database().CreateContext);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), manager);
        return manager;
    }
    private (MailManager Manager, MailItem Mail) LoadMarketplaceMailbox(MailState f)
    {
        var manager = LoadMarketplaceMailManager(f);
        return (manager, (MailItem)InboxMail(manager));
    }
}
