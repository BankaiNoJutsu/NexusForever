using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Mail;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Mail;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OutgoingMailCreatureAttachmentRollsBackWithTheCharacterAndRetriesOneIdentity(bool flush)
    {
        var f = await CreatureMailFixture();
        f.Mail.SendMail(10u, DeliverySpeed.Instant, 20u, 21u, [7001u]);
        string name;
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var character = context.Character.Single(row => row.Id == 100); name = character.Name;
            character.Name = "UncommittedCreatureMail"; f.Mail.Save(context);
            if (flush) await context.SaveChangesAsync();
            Assert.Empty(BankMessages(f.Player.Session)); await transaction.RollbackAsync();
        }
        using (var read = new CharacterContext(Configuration))
        {
            Assert.Equal(name, read.Character.Single(row => row.Id == 100).Name);
            Assert.Empty(read.CharacterMail); Assert.Empty(read.CharacterMailAttachment); Assert.Empty(read.Item);
        }
        using (var retry = new CharacterContext(Configuration)) { f.Mail.Save(retry); await retry.SaveChangesAsync(); }
        await DrainOutgoingMail(f, f.Player);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailAvailable>());
        using var final = new CharacterContext(Configuration);
        Assert.Equal(501ul, Assert.Single(final.CharacterMail).Id);
        Assert.Equal(900ul, Assert.Single(final.CharacterMailAttachment).ItemGuid);
        Assert.Equal(900ul, Assert.Single(final.Item).Id);
    }

    [Theory]
    [InlineData("none")] [InlineData("item")] [InlineData("character_mail_attachment")]
    public async Task OutgoingMailCreatureGrantUsesTheCharacterTransaction(string fault)
    {
        var f = await CreatureMailFixture();
        f.Mail.SendMail(10u, DeliverySpeed.Instant, 20u, 21u, [7001u]);
        if (fault != "none") await SplitTrigger(fault, "INSERT");
        using (var context = new CharacterContext(Configuration))
        {
            context.Character.Single(row => row.Id == 100).Name = "WithCreatureMail";
            f.Mail.Save(context);
            if (fault == "none") await context.SaveChangesAsync();
            else await Assert.ThrowsAnyAsync<Exception>(() => context.SaveChangesAsync());
        }
        if (fault == "none") await DrainOutgoingMail(f, f.Player);
        else { f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session)); }
        using var read = new CharacterContext(Configuration);
        Assert.Equal(fault == "none", read.Character.Single(row => row.Id == 100).Name == "WithCreatureMail");
        Assert.Equal(fault == "none" ? 1 : 0, read.CharacterMail.Count());
        Assert.Equal(fault == "none" ? 1 : 0, read.CharacterMailAttachment.Count());
        Assert.Equal(fault == "none" ? 1 : 0, read.Item.Count());
    }

    private async Task<MailState> CreatureMailFixture()
    {
        var f = await MailFixture();
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)f.Tables;
        tables.SetProperty(nameof(IGameTableManager.Creature2), PlayerBindPointTests.Table(new Creature2Entry { Id = 10 }));
        tables.SetProperty(nameof(IGameTableManager.LocalizedText), PlayerBindPointTests.Table(new LocalizedTextEntry { Id = 20 }, new LocalizedTextEntry { Id = 21 }));
        ((RecordingDispatchProxy<IItemManager>)(object)f.Items).SetProperty(nameof(IItemManager.NextItemId), 900ul);
        return f;
    }

    [Theory]
    [InlineData(DeliverySpeed.Instant)] [InlineData(DeliverySpeed.Hour)]
    public async Task OutgoingMailCreatureAttachmentRetainsLaterIntentThroughSelfDelivery(DeliverySpeed speed)
    {
        var f = await CreatureMailFixture(); f.Mail.SendMail(10u, speed, 20u, 21u, [7001u]);
        const System.Reflection.BindingFlags fields = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var queued = (Queue<IMailItem>)typeof(MailManager).GetField("outgoingMail", fields)!.GetValue(f.Mail)!;
        var original = Assert.Single(queued); var item = original.GetAttachment(0).Item;
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            f.Mail.Save(context); await context.SaveChangesAsync(); item.StackCount = 3; await transaction.CommitAsync();
        }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        if (speed == DeliverySpeed.Instant) Assert.Same(original, InboxMail(f.Mail));
        else
        {
            Assert.Same(original, Assert.Single((List<IMailItem>)typeof(MailManager).GetField("pendingMail", fields)!.GetValue(f.Mail)!));
            Assert.Empty(BankMessages(f.Player.Session));
        }
        using (var read = new CharacterContext(Configuration)) Assert.Equal(1u, Assert.Single(read.Item).StackCount);
        using (var save = new CharacterContext(Configuration)) { f.Mail.Save(save); await save.SaveChangesAsync(); }
        using var final = new CharacterContext(Configuration); Assert.Equal(3u, Assert.Single(final.Item).StackCount);
        Assert.Single(final.CharacterMail); Assert.Single(final.CharacterMailAttachment);
    }
}
