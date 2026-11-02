using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OutgoingMailOfflinePublicationRetainsDirtyIntentUntilInboxAdoption(bool afterPublication)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(context); await context.SaveChangesAsync();
            if (!afterPublication) mail.MarkAsRead(); await transaction.CommitAsync();
        }
        f.Player.Session.Events.Update(0);
        if (afterPublication) mail.MarkAsRead();
        using (var save = new CharacterContext(Configuration)) { manager.Save(save); await save.SaveChangesAsync(); }
        using (var read = new CharacterContext(Configuration)) Assert.NotEqual(0, read.CharacterMail.Single().Flags & (byte)MailFlag.IsRead);
        var (recipient, inbox) = RecipientMailbox(f); await PollMailbox(recipient, inbox);
        Assert.Same(mail, InboxMail(inbox));
    }

    [Fact]
    public async Task OutgoingMailQueuedReceiverNotificationWaitsForASubsequentSenderSave()
    {
        var f = await MailFixture(); var (recipient, inbox) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        using (var first = new CharacterContext(Configuration)) { manager.Save(first); await first.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); mail.MarkAsRead();
        using (var second = new CharacterContext(Configuration))
        {
            using var transaction = await second.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(second); recipient.Session.Events.Update(0);
            Assert.Empty(BankMessages(recipient.Session));
            await second.SaveChangesAsync(); await transaction.CommitAsync();
        }
        await DrainOutgoingMail(f, recipient); Assert.Same(mail, InboxMail(inbox));
        using var read = new CharacterContext(Configuration); Assert.NotEqual(0, read.CharacterMail.Single().Flags & (byte)MailFlag.IsRead);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OutgoingMailQueuedNotificationRejectsAReplacedSourceActor(bool session)
    {
        var f = await MailFixture(); var (recipient, inbox) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0);
        var proxy = (RecordingDispatchProxy<IPlayer>)(object)f.Player;
        if (session)
        {
            var replacement = RecordingDispatchProxy<IGameSession>.Create(out var sp); sp.SetProperty(nameof(IGameSession.Events), new EventQueue());
            proxy.SetProperty(nameof(IPlayer.Session), replacement);
            ((RecordingDispatchProxy<IAccount>)(object)f.Player.Account).SetProperty(nameof(IAccount.Session), replacement);
        }
        else proxy.SetProperty(nameof(IPlayer.MailManager), f.Mail);
        recipient.Session.Events.Update(0); Assert.Empty(BankMessages(recipient.Session));
        await PollMailbox(recipient, inbox); Assert.NotSame(mail, InboxMail(inbox));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OutgoingMailPublishedOwnerWaitsForRecoveryOfASubsequentSave(bool lostAcknowledgement)
    {
        var f = await MailFixture(); var (recipient, inbox) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        using (var first = new CharacterContext(Configuration)) { manager.Save(first); await first.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); mail.MarkAsRead();
        var options = new DbContextOptionsBuilder<CharacterContext>(); options.UseConfiguration(Configuration);
        if (lostAcknowledgement) options.AddInterceptors(new LoseOutgoingAcknowledgement());
        using (var second = new CharacterContext(options.Options))
        {
            using var transaction = await second.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(second); await second.SaveChangesAsync();
            if (lostAcknowledgement) await Assert.ThrowsAsync<IOException>(() => transaction.CommitAsync());
            else await transaction.RollbackAsync();
        }
        recipient.Session.Events.Update(0); await PollMailbox(recipient, inbox); Assert.Empty(BankMessages(recipient.Session));
        using (var retry = new CharacterContext(Configuration)) { manager.Save(retry); await retry.SaveChangesAsync(); }
        await DrainOutgoingMail(f, recipient); Assert.Same(mail, InboxMail(inbox));
        using var read = new CharacterContext(Configuration); Assert.NotEqual(0, read.CharacterMail.Single().Flags & (byte)MailFlag.IsRead);
    }

    [Fact]
    public async Task OutgoingMailInboxAdoptionRejectsAnActiveSenderSaveAtTheEnqueueBoundary()
    {
        var f = await MailFixture(); var (recipient, inbox) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        using (var first = new CharacterContext(Configuration)) { manager.Save(first); await first.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); mail.MarkAsRead();
        using (var second = new CharacterContext(Configuration))
        {
            using var transaction = await second.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(second);
            // The callback's readiness check may precede this new save. Enqueue
            // must make the final ownership decision itself.
            inbox.EnqueueMail(mail); Assert.Empty(BankMessages(recipient.Session));
            await second.SaveChangesAsync(); await transaction.CommitAsync();
        }
        await DrainOutgoingMail(f, recipient); Assert.Same(mail, InboxMail(inbox));
    }
}
