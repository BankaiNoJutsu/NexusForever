using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData("foreign")] [InlineData("inventory")] [InlineData("bank")]
    [InlineData("player")] [InlineData("named-gm")] [InlineData("named-auction")]
    public async Task MarketplaceMailedLegacyAdmissionRejectsForeignCustodyAndUnprovenOrigins(string fault)
    {
        var f = await MailFixture(); Assert.True(ProduceMarketplaceCommodityMail(f, false));
        using (var change = new CharacterContext(Configuration))
        {
            var item = change.Item.Single(); var mail = change.CharacterMail.Single();
            if (fault == "foreign") item.OwnerId = 200;
            if (fault == "inventory") item.Location = (ushort)InventoryLocation.Inventory;
            if (fault == "bank") item.Location = (ushort)InventoryLocation.RealmBank;
            if (fault == "player") mail.SenderType = (byte)SenderType.Player;
            if (fault == "named-gm") { mail.SenderType = (byte)SenderType.GM; mail.SenderId = 200; }
            if (fault == "named-auction") mail.SenderId = 200;
            await change.SaveChangesAsync();
        }
        var (manager, loaded) = LoadMarketplaceMailbox(f); manager.MailTakeAttachment(loaded.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(GenericError.MailBusy, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerItemAdd>());
        using var read = new CharacterContext(Configuration); Assert.Single(read.CharacterMailAttachment);
        Assert.Equal(fault == "foreign" ? 200ul : 100ul, Assert.Single(read.Item).OwnerId);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MarketplaceMailedAuctionEscrowCanBeCollectedAfterItsProducerCommits(bool returned)
    {
        var f = await MailFixture(); var assets = MarketplaceMailAssets(f);
        var row = CharacterRow(); row.Id = 900; row.OwnerId = null; row.Location = (ushort)InventoryLocation.None; row.Soulbound = false;
        using (var seed = new CharacterContext(Configuration)) { seed.Item.Add(row); await seed.SaveChangesAsync(); }
        var escrow = new GameItem(row, f.Items);
        Assert.True(returned
            ? MarketplaceMailDelivery.TrySendItemAuctionReturnMail(Database(), f.Tables, assets, 100, escrow)
            : MarketplaceMailDelivery.TrySendItemAuctionWonMail(Database(), f.Tables, assets, 100, escrow));
        var (manager, mail) = LoadMarketplaceMailbox(f); manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMailAttachment); Assert.Equal(900ul, Assert.Single(read.Item).Id);
    }

    [Fact]
    public async Task MarketplaceMailedSystemItemCanBeCollectedAfterItsProducerCommits()
    {
        var f = await MailFixture(); var assets = MarketplaceMailAssets(f);
        Assert.True(MarketplaceMailDelivery.TrySendSystemItemMail(Database(), f.Tables, assets, 100, 7001, 1, "System", "Attachment", itemManager: f.Items));
        var (manager, mail) = LoadMarketplaceMailbox(f); manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMailAttachment); Assert.Equal(100ul, Assert.Single(read.Item).OwnerId);
    }

    [Fact]
    public async Task MarketplaceMailedLegacyItemsCanBeCollectedAsOneBulkSelection()
    {
        var f = await MailFixture(); Assert.True(ProduceMarketplaceCommodityMail(f, false)); Assert.True(ProduceMarketplaceCommodityMail(f, true, 1001, 901));
        var manager = LoadMarketplaceMailManager(f); manager.MailTakeAllFromSelection([1000, 1001], 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(2, BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>().Count());
        Assert.All(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>(), result => Assert.Equal(GenericError.Ok, result.Result));
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMailAttachment); Assert.Equal(2, read.Item.Count());
        Assert.All(read.Item, item => { Assert.Equal(100ul, item.OwnerId); Assert.Equal((ushort)InventoryLocation.Inventory, item.Location); });
    }

    [Theory]
    [InlineData("item", "UPDATE")] [InlineData("character_mail", "UPDATE")] [InlineData("character_mail_attachment", "DELETE")]
    public async Task MarketplaceMailedLegacyTakeRollsBackEverySqlComponentAndQuarantines(string table, string operation)
    {
        var f = await MailFixture(); Assert.True(ProduceMarketplaceCommodityMail(f, false)); var (manager, mail) = LoadMarketplaceMailbox(f);
        await SplitTrigger(table, operation); manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(GenericError.DbFailure, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerItemAdd>());
        using (var read = new CharacterContext(Configuration))
        {
            var item = Assert.Single(read.Item); Assert.Equal(100ul, item.OwnerId); Assert.Equal((ushort)InventoryLocation.None, item.Location);
            Assert.Single(read.CharacterMailAttachment); Assert.Equal(0, read.CharacterMail.Single().Flags & (byte)MailFlag.NotReturnable);
            await read.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_split");
        }
        manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(GenericError.MailBusy, BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>().Last().Result);
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerItemAdd>());
        using var final = new CharacterContext(Configuration); Assert.Single(final.CharacterMailAttachment);
    }

    [Fact]
    public async Task MarketplaceMailedLegacyTakeRecoversActualLostCommitAcknowledgement()
    {
        int lost = 0; var fault = new LoseAttachmentAcknowledgement();
        var f = await MailFixture(command =>
        {
            var options = new DbContextOptionsBuilder<CharacterContext>(); options.UseConfiguration(Configuration); options.AddInterceptors(fault);
            typeof(NexusForever.Game.RealmBank.RealmBankManager.BankSaveCommand).GetField("contexts", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(command, new Func<CharacterContext>(() => new CharacterContext(options.Options)));
            return command.ExecuteAsync(async attempt => { try { await attempt(); } catch (IOException) { ++lost; await attempt(); } });
        });
        Assert.True(ProduceMarketplaceCommodityMail(f, false)); var (manager, mail) = LoadMarketplaceMailbox(f);
        manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(1, lost); Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerItemAdd>());
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMailAttachment); Assert.Equal((ushort)InventoryLocation.Inventory, Assert.Single(read.Item).Location);
    }

    [Fact]
    public async Task MarketplaceMailedLegacyTakePreparationFailureRestoresDetachedOwnership()
    {
        var f = await MailFixture(); Assert.True(ProduceMarketplaceCommodityMail(f, false)); var (manager, mail) = LoadMarketplaceMailbox(f);
        var pp = (RecordingDispatchProxy<IPlayer>)(object)f.Player;
        pp.SetMethodHandler("get_IsLoading", _ => throw new InvalidOperationException("Legacy mailbox preparation failed."));
        manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(GenericError.MailBusy, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        Assert.Null(f.Inventory.GetItem(900)); var attachment = Assert.Single(mail);
        Assert.Equal(100ul, attachment.Item.CharacterId); Assert.Equal(InventoryLocation.None, attachment.Item.Location);
        pp.SetMethodHandler("get_IsLoading", _ => false);
        manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>(), result => result.Result == GenericError.Ok);
    }

    [Fact]
    public async Task MarketplaceMailedLegacyTakeRetainsTheCompleteStoredItemWitness()
    {
        var f = await MailFixture(); Assert.True(ProduceMarketplaceCommodityMail(f, false)); var (manager, mail) = LoadMarketplaceMailbox(f);
        using (var change = new CharacterContext(Configuration)) { change.Item.Single().StackCount = 2; await change.SaveChangesAsync(); }
        manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(GenericError.DbFailure, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        using var read = new CharacterContext(Configuration); Assert.Equal(2u, Assert.Single(read.Item).StackCount); Assert.Single(read.CharacterMailAttachment);
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerItemAdd>());
    }

    [Fact]
    public async Task MarketplaceMailedLegacyTakePublishesOnlyAfterStorageCommits()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await MailFixture(command => command.ExecuteAsync(async attempt => { await release.Task; await attempt(); }));
        Assert.True(ProduceMarketplaceCommodityMail(f, false)); var (manager, mail) = LoadMarketplaceMailbox(f);
        try
        {
            manager.MailTakeAttachment(mail.Id, 0, 1);
            Assert.Empty(BankMessages(f.Player.Session));
            using var read = new CharacterContext(Configuration); Assert.Equal((ushort)InventoryLocation.None, Assert.Single(read.Item).Location); Assert.Single(read.CharacterMailAttachment);
        }
        finally { release.TrySetResult(); await WaitForBankMove(f.Inventory); }
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
    }
}
