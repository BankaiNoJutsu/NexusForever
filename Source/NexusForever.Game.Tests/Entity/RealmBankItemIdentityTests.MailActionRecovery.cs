using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false, 0)] [InlineData(true, 0)]
    [InlineData(false, 1)] [InlineData(true, 1)]
    [InlineData(false, 2)] [InlineData(true, 2)]
    public async Task ManualMailActionIncompleteAcknowledgementRetainsTheOutcome(bool returning, int kind)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, false);
        if (returning) manager.ReturnMail(mail.Id); else manager.MailDelete(mail.Id);
        var options = new DbContextOptionsBuilder<CharacterContext>(); options.UseConfiguration(Configuration);
        if (kind == 2) options.AddInterceptors(new LoseOutgoingAcknowledgement());
        using (var context = new CharacterContext(options.Options))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (kind == 1) await transaction.CreateSavepointAsync("manual_action");
            manager.Save(context);
            if (kind != 0) await context.SaveChangesAsync();
            if (kind == 1) await transaction.RollbackToSavepointAsync("manual_action");
            if (kind == 2) await Assert.ThrowsAsync<IOException>(() => transaction.CommitAsync());
            else await transaction.CommitAsync();
        }
        f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session));
        using (var read = new CharacterContext(Configuration))
        {
            if (kind != 2) Assert.Equal(100ul, Assert.Single(read.CharacterMail).RecipientId);
            else if (returning) Assert.Equal(200ul, Assert.Single(read.CharacterMail).RecipientId);
            else Assert.Empty(read.CharacterMail);
        }
        using (var retry = new CharacterContext(Configuration)) { manager.Save(retry); await retry.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailUnavailable>());
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ManualMailActionOldManagerCannotPublishACommittedOutcome(bool returning)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, false);
        if (returning) manager.ReturnMail(mail.Id); else manager.MailDelete(mail.Id);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), f.Mail);
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        Assert.Empty(BankMessages(f.Player.Session));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ManualMailActionChecksTheActorBeforeEachSuccessPacket(bool returning)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, false);
        var session = (RecordingDispatchProxy<IGameSession>)(object)f.Player.Session;
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerMailUnavailable)
                ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), f.Mail);
            return null;
        });
        if (returning) manager.ReturnMail(mail.Id); else manager.MailDelete(mail.Id);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailUnavailable>());
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerMailResult>());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ManualMailActionRejectsACrossActionRepeatWhilePending(bool returning)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, false);
        if (returning) { manager.ReturnMail(mail.Id); manager.MailDelete(mail.Id); }
        else { manager.MailDelete(mail.Id); manager.ReturnMail(mail.Id); }
        Assert.Equal(GenericError.MailBusy, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerMailUnavailable>());
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>(), result => result.Result == GenericError.Ok);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ManualMailActionHeaderFailureRollsBackTheCoupledCharacterChange(bool returning)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, false);
        if (returning) manager.ReturnMail(mail.Id); else manager.MailDelete(mail.Id);
        await SplitTrigger("character_mail", returning ? "UPDATE" : "DELETE");
        string original;
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var character = context.Character.Single(row => row.Id == 100); original = character.Name; character.Name = "ManualActionRollback";
            manager.Save(context);
            var error = await Assert.ThrowsAnyAsync<Exception>(() => context.SaveChangesAsync());
            Assert.Contains("split failure", error.ToString(), StringComparison.Ordinal);
            await transaction.RollbackAsync();
        }
        f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session));
        using (var read = new CharacterContext(Configuration))
        {
            Assert.Equal(original, read.Character.Single(row => row.Id == 100).Name);
            Assert.Equal(100ul, Assert.Single(read.CharacterMail).RecipientId);
            await read.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_split");
        }
        using (var retry = new CharacterContext(Configuration)) { manager.Save(retry); await retry.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ManualMailActionRejectsAnAlreadyReplacedManager(bool returning)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, false);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), f.Mail);
        if (returning) manager.ReturnMail(mail.Id); else manager.MailDelete(mail.Id);
        Assert.Equal(GenericError.MailBusy, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        using var read = new CharacterContext(Configuration); Assert.Equal(100ul, Assert.Single(read.CharacterMail).RecipientId);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ManualMailActionConfirmsTheExistingAttachmentMembershipPolicy(bool returning)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingAttachmentMail(f, 2);
        if (returning) manager.ReturnMail(mail.Id); else manager.MailDelete(mail.Id);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(returning ? 2 : 0, read.CharacterMailAttachment.Count());
        Assert.Equal(2, read.Item.Count(row => row.Id == 900 || row.Id == 901));
        Assert.All(read.Item.Where(row => row.Id == 900 || row.Id == 901), item => Assert.Null(item.OwnerId));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ManualMailActionRequiresAnActorEventQueue(bool returning)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, false);
        ((RecordingDispatchProxy<IGameSession>)(object)f.Player.Session).SetProperty(nameof(IGameSession.Events), null);
        if (returning) manager.ReturnMail(mail.Id); else manager.MailDelete(mail.Id);
        Assert.Equal(GenericError.MailBusy, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        using var read = new CharacterContext(Configuration); Assert.Equal(100ul, Assert.Single(read.CharacterMail).RecipientId);
    }

    [Fact]
    public async Task ManualMailActionSourceEnqueueFailureStillAllowsRecipientDelivery()
    {
        var f = await MailFixture(); var (recipient, inbox) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false);
        ((RecordingDispatchProxy<IGameSession>)(object)f.Player.Session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerMailUnavailable) throw new IOException("Manual return source enqueue failed.");
            return null;
        });
        manager.ReturnMail(mail.Id);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        await DrainOutgoingMail(f, recipient);
        Assert.Single(BankMessages(recipient.Session).OfType<ServerMailAvailable>());
        Assert.Same(mail, InboxMail(inbox));
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerMailResult>());
    }
}
