using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Shared.Game.Events;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData("account")] [InlineData("accountId")] [InlineData("accountSession")]
    [InlineData("inventory")] [InlineData("character")] [InlineData("realm")]
    [InlineData("events")] [InlineData("logout")]
    public async Task MarketplaceMailPublicationDropsAChangedRecipientIdentity(string changed)
    {
        var f = await MailFixture(); PublicationRecipient(f); var assets = MarketplaceMailAssets(f);
        Assert.True(await Task.Run(() => MarketplaceMailDelivery.TrySendCommodityAuctionFillMail(Database(), f.Tables, assets, 100, 7001, 1, f.Players, f.Items)));
        Assert.Empty(BankMessages(f.Player.Session)); var events = f.Player.Session.Events;
        var pp = (RecordingDispatchProxy<IPlayer>)(object)f.Player;
        var ap = (RecordingDispatchProxy<IAccount>)(object)f.Player.Account;
        switch (changed)
        {
            case "account":
                var account = RecordingDispatchProxy<IAccount>.Create(out var replacement);
                replacement.SetProperty(nameof(IAccount.Id), 12u); replacement.SetProperty(nameof(IAccount.Session), f.Player.Session);
                pp.SetProperty(nameof(IPlayer.Account), account); break;
            case "accountId": ap.SetProperty(nameof(IAccount.Id), 13u); break;
            case "accountSession": ap.SetProperty(nameof(IAccount.Session), RecordingDispatchProxy<IGameSession>.Create(out _)); break;
            case "inventory": pp.SetProperty(nameof(IPlayer.Inventory), RecordingDispatchProxy<IInventory>.Create(out _)); break;
            case "character": pp.SetProperty(nameof(IPlayer.CharacterId), 200ul); break;
            case "realm": pp.SetProperty(nameof(IPlayer.Identity), new Identity { RealmId = 3, Id = 100 }); break;
            case "events": ((RecordingDispatchProxy<IGameSession>)(object)f.Player.Session).SetProperty(nameof(IGameSession.Events), new EventQueue()); break;
            case "logout":
                var logout = RecordingDispatchProxy<ILogoutManager>.Create(out var lp); lp.SetProperty(nameof(ILogoutManager.State), LogoutState.Logout);
                pp.SetProperty(nameof(IPlayer.LogoutManager), logout); break;
        }
        events.Update(0); f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session));
        using var read = new CharacterContext(Configuration); Assert.Single(read.CharacterMail); Assert.Single(read.CharacterMailAttachment);
    }

    [Fact]
    public async Task MarketplaceMailPublicationRejectsASoftDeletedSqlOwner()
    {
        var f = await MailFixture(); PublicationRecipient(f); var assets = MarketplaceMailAssets(f);
        using (var edit = new CharacterContext(Configuration))
            await edit.Character.Where(row => row.Id == 100).ExecuteUpdateAsync(setters => setters.SetProperty(row => row.DeleteTime, DateTime.UtcNow));
        Assert.True(await Task.Run(() => MarketplaceMailDelivery.TrySendCommodityAuctionFillMail(Database(), f.Tables, assets, 100, 7001, 1, f.Players, f.Items)));
        f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session));
        using var read = new CharacterContext(Configuration); Assert.Single(read.CharacterMail);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MarketplaceMailPublicationReceiverRouteOrFactoryFailureLeavesSqlForLogin(bool factoryThrows)
    {
        var f = await MailFixture(); PublicationRecipient(f);
        string otherPath = path + ".unused";
        var other = new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={otherPath};Pooling=False" };
        var manager = new MailManager(f.Player, new CharacterModel(), realmId: 2, itemManager: f.Items, gameTableManager: f.Tables,
            deliveryContexts: () => factoryThrows ? throw new IOException("Receiver context unavailable.") : new CharacterContext(other));
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), manager);
        Assert.True(await Task.Run(() => MarketplaceMailDelivery.TrySendCommodityAuctionFillMail(Database(), f.Tables, MarketplaceMailAssets(f), 100, 7001, 1, f.Players, f.Items)));
        f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session)); Assert.False(File.Exists(otherPath));
        var recovered = LoadMarketplaceMailManager(f); recovered.SendInitialPackets();
        Assert.Single(Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailAvailable>()).MailList);
    }

    [Fact]
    public async Task MarketplaceMailPublicationSourceRouteChangeAfterCommitDoesNotReadOrPublishAnotherDatabase()
    {
        var f = await MailFixture(); PublicationRecipient(f); var database = Database(); string otherPath = path + ".unused";
        var other = new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={otherPath};Pooling=False" };
        Assert.True(await Task.Run(() => MarketplaceMailDelivery.TrySendCommodityAuctionFillMail(database, f.Tables, MarketplaceMailAssets(f), 100, 7001, 1,
            context => context.OnSaveSettled(committed => { if (committed) database.Initialise(other); }), f.Players, f.Items)));
        f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session)); Assert.False(File.Exists(otherPath));
        using var read = new CharacterContext(Configuration); Assert.Single(read.CharacterMail); Assert.Single(read.CharacterMailAttachment);
    }

    [Fact]
    public async Task MarketplaceMailPublicationPlayerLookupFailurePreservesConfirmedCreditMail()
    {
        var f = await MailFixture();
        ((RecordingDispatchProxy<IPlayerManager>)(object)f.Players).SetMethodHandler(nameof(IPlayerManager.GetPlayer), _ => throw new IOException("Player lookup unavailable."));
        Assert.True(await Task.Run(() => MarketplaceMailDelivery.TrySendMarketplaceCreditMail(Database(), f.Tables, MarketplaceMailAssets(f), 100, 75, "Proceeds", "Sold", f.Players)));
        Assert.Empty(BankMessages(f.Player.Session));
        using var read = new CharacterContext(Configuration); Assert.Equal(75ul, Assert.Single(read.CharacterMail).CurrencyAmount); Assert.Empty(read.CharacterMailAttachment);
    }

    [Fact]
    public async Task MarketplaceMailPublicationNonConcreteNotificationFailureDoesNotReverseConfirmedSql()
    {
        var f = await MailFixture(); PublicationRecipient(f);
        var adapter = RecordingDispatchProxy<IMailManager>.Create(out var mp);
        mp.SetMethodHandler(nameof(IMailManager.EnqueueMail), _ => throw new IOException("Adapter unavailable."));
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), adapter);
        Assert.True(await Task.Run(() => MarketplaceMailDelivery.TrySendMarketplaceCreditMail(Database(), f.Tables, MarketplaceMailAssets(f), 100, 75, "Proceeds", "Sold", f.Players)));
        Assert.Single(mp.GetInvocations(nameof(IMailManager.EnqueueMail)));
        using var read = new CharacterContext(Configuration); Assert.Equal(75ul, Assert.Single(read.CharacterMail).CurrencyAmount);
    }

    [Fact]
    public async Task MarketplaceMailPublicationCreditMailWaitsForTheActorAndPublishesOnce()
    {
        var f = await MailFixture(); PublicationRecipient(f);
        Assert.True(await Task.Run(() => MarketplaceMailDelivery.TrySendMarketplaceCreditMail(Database(), f.Tables, MarketplaceMailAssets(f), 100, 75, "Proceeds", "Sold", f.Players)));
        Assert.Empty(BankMessages(f.Player.Session)); f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        Assert.Equal(75ul, Assert.Single(Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailAvailable>()).MailList).CurrencySentAmount);
        using var read = new CharacterContext(Configuration); Assert.Equal(75ul, Assert.Single(read.CharacterMail).CurrencyAmount); Assert.Empty(read.Item);
    }

    [Fact]
    public async Task MarketplaceMailPublicationSqlFailureControlRestoresEscrowAndPublishesNothing()
    {
        var f = await MailFixture(); PublicationRecipient(f); var assets = MarketplaceMailAssets(f);
        var row = CharacterRow(); row.Id = 900; row.OwnerId = null; row.Location = (ushort)InventoryLocation.None; row.Soulbound = false;
        using (var seed = new CharacterContext(Configuration))
        {
            seed.Item.Add(row); await seed.SaveChangesAsync();
            await seed.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_marketplace BEFORE INSERT ON character_mail BEGIN SELECT RAISE(ABORT, 'Rejected marketplace header'); END;");
        }
        var escrow = new GameItem(row, f.Items);
        Assert.False(await Task.Run(() => MarketplaceMailDelivery.TrySendItemAuctionWonMail(Database(), f.Tables, assets, 100, escrow, f.Players)));
        Assert.Null(escrow.CharacterId); f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session));
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMail); Assert.Empty(read.CharacterMailAttachment); Assert.Null(Assert.Single(read.Item).OwnerId);
    }
}

