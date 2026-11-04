using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

// Real marketplace producers, SQL persistence, and recipient actor publication.
public sealed partial class RealmBankItemIdentityTests
{
    [Fact]
    public async Task MarketplaceMailPublicationFromAWorkerWaitsForTheRecipientActor()
    {
        var f = await MailFixture(); PublicationRecipient(f); var assets = MarketplaceMailAssets(f);
        Assert.True(await Task.Run(() => MarketplaceMailDelivery.TrySendCommodityAuctionFillMail(Database(), f.Tables, assets, 100, 7001, 1, f.Players, f.Items)));
        Assert.Empty(BankMessages(f.Player.Session));
        using (var read = new CharacterContext(Configuration)) { Assert.Single(read.CharacterMail); Assert.Single(read.CharacterMailAttachment); }
        f.Player.Session.Events.Update(0);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailAvailable>());
    }

    [Fact]
    public async Task MarketplaceMailPublicationWaitsForAnAlreadyEnrolledMailboxSave()
    {
        var f = await MailFixture(); PublicationRecipient(f); var assets = MarketplaceMailAssets(f);
        using (var held = new CharacterContext(Configuration))
        {
            f.Mail.Save(held);
            Assert.True(await Task.Run(() => MarketplaceMailDelivery.TrySendCommodityAuctionFillMail(Database(), f.Tables, assets, 100, 7001, 1, f.Players, f.Items)));
            f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session));
        }
        f.Player.Session.Events.Update(0); Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailAvailable>());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MarketplaceMailPublicationDropsAReplacedActorAndLoginRecoversSql(bool sessionChanged)
    {
        var f = await MailFixture(); PublicationRecipient(f); var assets = MarketplaceMailAssets(f); var old = f.Player.Session;
        Assert.True(await Task.Run(() => MarketplaceMailDelivery.TrySendCommodityAuctionFillMail(Database(), f.Tables, assets, 100, 7001, 1, f.Players, f.Items)));
        Assert.Empty(BankMessages(old));
        if (sessionChanged)
        {
            var replacement = RecordingDispatchProxy<IGameSession>.Create(out var sp); sp.SetProperty(nameof(IGameSession.Events), new EventQueue());
            ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.Session), replacement);
            ((RecordingDispatchProxy<IAccount>)(object)f.Player.Account).SetProperty(nameof(IAccount.Session), replacement);
        }
        using var read = new CharacterContext(Configuration); var model = new CharacterModel();
        model.Mail.Add(read.CharacterMail.AsNoTracking().Include(mail => mail.Attachment).ThenInclude(link => link.Item).Single());
        var manager = new MailManager(f.Player, model, realmId: 2, itemManager: f.Items, gameTableManager: f.Tables, deliveryContexts: Database().CreateContext);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), manager);
        old.Events.Update(0); Assert.Empty(BankMessages(old));
        manager.SendInitialPackets(); Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailAvailable>());
    }

    [Fact]
    public async Task MarketplaceMailPublicationCannotTurnAnEnqueueErrorIntoAFailedCommit()
    {
        var f = await MailFixture(); PublicationRecipient(f); var assets = MarketplaceMailAssets(f);
        var row = CharacterRow(); row.Id = 900; row.OwnerId = null; row.Location = (ushort)InventoryLocation.None; row.Soulbound = false;
        using (var seed = new CharacterContext(Configuration)) { seed.Item.Add(row); await seed.SaveChangesAsync(); }
        var escrow = new GameItem(row, f.Items); int failures = 0;
        var session = (RecordingDispatchProxy<IGameSession>)(object)f.Player.Session;
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerMailAvailable) { ++failures; throw new IOException("Recipient enqueue unavailable."); }
            return null;
        });
        Assert.True(await Task.Run(() => MarketplaceMailDelivery.TrySendItemAuctionWonMail(Database(), f.Tables, assets, 100, escrow, f.Players)));
        Assert.Equal(100ul, escrow.CharacterId);
        f.Player.Session.Events.Update(0); Assert.Equal(1, failures); Assert.Equal(100ul, escrow.CharacterId);
        using var read = new CharacterContext(Configuration); Assert.Single(read.CharacterMail); Assert.Single(read.CharacterMailAttachment);
        Assert.Equal(100ul, Assert.Single(read.Item).OwnerId);
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), _ => null);
        f.Player.MailManager.SendInitialPackets(); Assert.NotEmpty(BankMessages(f.Player.Session).OfType<ServerMailAvailable>());
    }

    [Fact]
    public async Task MarketplaceMailPublicationRetryDropsAReboundSessionWithoutAnAuctionGuard()
    {
        var f = await MailFixture(); PublicationRecipient(f);
        var events = new EventQueue();
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var sp);
        sp.SetProperty(nameof(IWorldSession.Events), events);
        sp.SetProperty(nameof(IWorldSession.Player), f.Player);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.Session), session);
        ((RecordingDispatchProxy<IAccount>)(object)f.Player.Account).SetProperty(nameof(IAccount.Session), session);
        var manager = new MailManager(f.Player, new CharacterModel(), realmId: 2, itemManager: f.Items, gameTableManager: f.Tables, deliveryContexts: Database().CreateContext);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), manager);
        int attempts = 0;
        sp.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerMailAvailable) { attempts++; throw new IOException("Recipient enqueue unavailable."); }
            return null;
        });

        Assert.True(await Task.Run(() => MarketplaceMailDelivery.TrySendCommodityAuctionFillMail(
            Database(), f.Tables, MarketplaceMailAssets(f), 100, 7001, 1, f.Players, f.Items)));
        events.Update(0);
        Assert.Equal(1, attempts);
        Assert.True(events.PendingEvents);

        sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        events.Update(0);
        Assert.Equal(1, attempts);
        Assert.False(events.PendingEvents);
        using var read = new CharacterContext(Configuration);
        Assert.Single(read.CharacterMail);
        Assert.Single(read.CharacterMailAttachment);
    }

    [Fact]
    public async Task MarketplaceMailPublicationChecksSqlAccountOwnershipBeyondMatchingStaleObjects()
    {
        var f = await MailFixture(); PublicationRecipient(f);
        ((RecordingDispatchProxy<IAccount>)(object)f.Player.Account).SetProperty(nameof(IAccount.Id), 13u);
        var manager = new MailManager(f.Player, new CharacterModel(), realmId: 2, itemManager: f.Items, gameTableManager: f.Tables, deliveryContexts: Database().CreateContext);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), manager);
        var assets = MarketplaceMailAssets(f);
        Assert.True(await Task.Run(() => MarketplaceMailDelivery.TrySendCommodityAuctionFillMail(Database(), f.Tables, assets, 100, 7001, 1, f.Players, f.Items)));
        f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session));
        using var read = new CharacterContext(Configuration); Assert.Equal(12u, read.Character.Single(character => character.Id == 100).AccountId); Assert.Single(read.CharacterMail);
    }

    [Fact]
    public async Task MarketplaceMailPublicationPreparationFailureDoesNotReverseConfirmedSqlControl()
    {
        var f = await MailFixture(); PublicationRecipient(f); var assets = MarketplaceMailAssets(f); var database = Database();
        var row = CharacterRow(); row.Id = 900; row.OwnerId = null; row.Location = (ushort)InventoryLocation.None; row.Soulbound = false;
        using (var seed = new CharacterContext(Configuration)) { seed.Item.Add(row); await seed.SaveChangesAsync(); }
        var escrow = new GameItem(row, f.Items);
        Assert.True(await Task.Run(() => MarketplaceMailDelivery.TrySendItemAuctionWonMail(database, f.Tables, assets, 100, escrow,
            context => context.OnSaveSettled(committed => { if (committed) database.Initialise(null); }), f.Players)));
        Assert.Equal(100ul, escrow.CharacterId);
        using var read = new CharacterContext(Configuration); Assert.Single(read.CharacterMail); Assert.Single(read.CharacterMailAttachment);
    }

    private static void PublicationRecipient(MailState f)
        => ((RecordingDispatchProxy<IPlayerManager>)(object)f.Players).SetMethodReturn(nameof(IPlayerManager.GetPlayer), f.Player);
}

