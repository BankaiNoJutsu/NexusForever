using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model.Mail;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Fact]
    public async Task OutgoingMailNoFlushCommitCannotPublishOrConsumeTheQueuedReturn()
    {
        var f = await MailFixture(); var (recipient, _) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(context); await transaction.CommitAsync();
        }
        f.Player.Session.Events.Update(0); recipient.Session.Events.Update(0);
        Assert.Empty(BankMessages(recipient.Session));
        using (var read = new CharacterContext(Configuration)) Assert.Equal(100ul, read.CharacterMail.Single().RecipientId);
        using (var retry = new CharacterContext(Configuration)) { manager.Save(retry); await retry.SaveChangesAsync(); }
        await DrainOutgoingMail(f, recipient);
        Assert.Single(BankMessages(recipient.Session).OfType<ServerMailAvailable>());
    }

    [Fact]
    public async Task OutgoingMailSavepointRollbackAndOuterCommitKeepTheReturnForRetry()
    {
        var f = await MailFixture(); var (recipient, _) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await transaction.CreateSavepointAsync("outgoing_mail"); manager.Save(context); await context.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("outgoing_mail"); await transaction.CommitAsync();
        }
        f.Player.Session.Events.Update(0); recipient.Session.Events.Update(0);
        Assert.Empty(BankMessages(recipient.Session));
        using (var read = new CharacterContext(Configuration)) Assert.Equal(100ul, read.CharacterMail.Single().RecipientId);
        using (var retry = new CharacterContext(Configuration)) { manager.Save(retry); await retry.SaveChangesAsync(); }
        await DrainOutgoingMail(f, recipient);
    }

    [Fact]
    public async Task OutgoingMailLosingItsSourceManagerCannotPublishTheOriginalCallback()
    {
        var f = await MailFixture(); var (recipient, _) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), f.Mail);
        f.Player.Session.Events.Update(0); recipient.Session.Events.Update(0);
        Assert.Empty(BankMessages(recipient.Session));
        using var read = new CharacterContext(Configuration); Assert.Equal(200ul, read.CharacterMail.Single().RecipientId);
    }

    [Fact]
    public async Task OutgoingMailSqlPollingWaitsForOriginalActorAdoption()
    {
        var f = await MailFixture(); var (recipient, inbox) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        // Only poll the receiver. The source actor has not adopted its save yet.
        await PollMailbox(recipient, inbox); Assert.Empty(BankMessages(recipient.Session));
        await DrainOutgoingMail(f, recipient); Assert.Same(mail, InboxMail(inbox));
    }

    private static async Task DrainOutgoingMail(MailState f, IPlayer recipient)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (!BankMessages(recipient.Session).OfType<ServerMailAvailable>().Any())
        {
            f.Player.Session.Events.Update(0); recipient.Session.Events.Update(0); await Task.Delay(5, timeout.Token);
        }
    }
}
