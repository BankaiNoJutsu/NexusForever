using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Fact]
    public async Task MailAttachmentTakePublishesOnlyAfterTheItemAndLinkCommit()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await MailFixture(command => command.ExecuteAsync(async attempt => { await release.Task; await attempt(); }));
        var (manager, mail) = await IncomingAttachmentMail(f);
        try
        {
            manager.MailTakeAttachment(mail.Id, 0, 1);
            Assert.Empty(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>());
            Assert.Empty(BankMessages(f.Player.Session).OfType<ServerItemAdd>());
            using var read = new CharacterContext(Configuration);
            Assert.Null(read.Item.Single().OwnerId); Assert.Single(read.CharacterMailAttachment);
        }
        finally { release.TrySetResult(); await WaitForBankMove(f.Inventory); }
        using var final = new CharacterContext(Configuration);
        Assert.Equal(100ul, final.Item.Single().OwnerId); Assert.Empty(final.CharacterMailAttachment);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MailAttachmentTakeRejectsLinkFailureOrAReplacedLinkWithoutSuccess(bool replaced)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingAttachmentMail(f);
        if (replaced)
        {
            using var transfer = new CharacterContext(Configuration);
            var other = CharacterRow(); other.Id = 901; other.OwnerId = null; other.Location = (ushort)InventoryLocation.None;
            transfer.Item.Add(other); transfer.CharacterMailAttachment.Single().ItemGuid = 901; await transfer.SaveChangesAsync();
        }
        else await SplitTrigger("character_mail_attachment", "DELETE");
        manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(GenericError.DbFailure, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerItemAdd>());
        using var read = new CharacterContext(Configuration);
        Assert.All(read.Item, item => Assert.Null(item.OwnerId));
        Assert.Equal(replaced ? 901ul : 900ul, Assert.Single(read.CharacterMailAttachment).ItemGuid);
        Assert.Equal(0, read.CharacterMail.Single().Flags & (byte)MailFlag.NotReturnable);
    }

    [Fact]
    public async Task MailAttachmentTakeCommittedControlKeepsOneItemAndRemovesOnlyItsLink()
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingAttachmentMail(f);
        manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        using (var normal = new CharacterContext(Configuration)) { f.Inventory.Save(normal); manager.Save(normal); await normal.SaveChangesAsync(); }
        using var read = new CharacterContext(Configuration); var stored = Assert.Single(read.Item);
        Assert.Equal(900ul, stored.Id); Assert.Equal(100ul, stored.OwnerId);
        Assert.Equal((ushort)InventoryLocation.Inventory, stored.Location); Assert.Empty(read.CharacterMailAttachment);
        Assert.NotEqual(0, read.CharacterMail.Single().Flags & (byte)MailFlag.NotReturnable);
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
    }

    private async Task<(MailManager Manager, MailItem Mail)> IncomingAttachmentMail(MailState f, uint count = 1, ulong gift = 30)
    {
        await IncomingCashMail(f, false);
        using var seed = new CharacterContext(Configuration);
        seed.CharacterMail.Single().CurrencyAmount = gift;
        for (uint i = 0; i < count; ++i)
        {
            var item = CharacterRow(); item.Id = 900 + i; item.OwnerId = null; item.Location = (ushort)InventoryLocation.None;
            seed.Item.Add(item); seed.CharacterMailAttachment.Add(new CharacterMailAttachmentModel { Id = 600, Index = i, ItemGuid = item.Id });
        }
        await seed.SaveChangesAsync(); seed.ChangeTracker.Clear();
        var row = seed.CharacterMail.AsNoTracking().Include(mail => mail.Attachment).ThenInclude(link => link.Item).Single();
        var model = new CharacterModel(); model.Mail.Add(row);
        var manager = new MailManager(f.Player, model, realmId: 2, itemManager: f.Items, gameTableManager: f.Tables, deliveryContexts: Database().CreateContext);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), manager);
        return (manager, (MailItem)InboxMail(manager));
    }
}
