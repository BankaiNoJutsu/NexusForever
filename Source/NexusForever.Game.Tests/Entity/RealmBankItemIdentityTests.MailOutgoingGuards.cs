using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OutgoingMailSaveExcludesConcurrentReturnAndDelete(bool delete)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, false);
        manager.MailMarkAsRead(mail.Id);
        using var context = new CharacterContext(Configuration);
        using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        manager.Save(context);
        if (delete) manager.MailDelete(mail.Id); else manager.ReturnMail(mail.Id);
        Assert.Equal(GenericError.MailBusy, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        await context.SaveChangesAsync(); await transaction.CommitAsync();
        using var read = new CharacterContext(Configuration); Assert.Equal(100ul, Assert.Single(read.CharacterMail).RecipientId);
    }

    [Fact]
    public async Task OutgoingMailRecipientAccountRewriteRejectsTheWholeSave()
    {
        var f = await MailFixture(); var (recipient, _) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        bool rejected = false;
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(context);
            await context.Character.Where(row => row.Id == 200).ExecuteUpdateAsync(setters => setters.SetProperty(row => row.AccountId, 14u));
            try { await context.SaveChangesAsync(); await transaction.CommitAsync(); }
            catch (InvalidOperationException) when (context.IsSaveRejected) { rejected = true; }
        }
        Assert.True(rejected); f.Player.Session.Events.Update(0); recipient.Session.Events.Update(0);
        Assert.Empty(BankMessages(recipient.Session));
        using var read = new CharacterContext(Configuration);
        Assert.Equal(13u, read.Character.Single(row => row.Id == 200).AccountId);
        Assert.Equal(100ul, read.CharacterMail.Single().RecipientId);
    }

    [Fact]
    public async Task OutgoingMailLostAcknowledgementRetainsLaterReadIntentAndOneIdentity()
    {
        var f = await MailFixture(); var (recipient, inbox) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        var options = new DbContextOptionsBuilder<CharacterContext>(); options.UseConfiguration(Configuration);
        options.AddInterceptors(new LoseOutgoingAcknowledgement());
        using (var context = new CharacterContext(options.Options))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(context); await context.SaveChangesAsync();
            await Assert.ThrowsAsync<IOException>(() => transaction.CommitAsync());
        }
        mail.MarkAsRead(); await PollMailbox(recipient, inbox); Assert.Empty(BankMessages(recipient.Session));
        using (var retry = new CharacterContext(Configuration)) { manager.Save(retry); await retry.SaveChangesAsync(); }
        await DrainOutgoingMail(f, recipient); Assert.Same(mail, InboxMail(inbox));
        using var read = new CharacterContext(Configuration); var stored = Assert.Single(read.CharacterMail);
        Assert.Equal(600ul, stored.Id); Assert.NotEqual(0, stored.Flags & (byte)MailFlag.IsRead);
    }

    [Fact]
    public async Task OutgoingMailReadAfterFlushCrossesWithTheOriginalAndStaysDirty()
    {
        var f = await MailFixture(); var (recipient, inbox) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(context); await context.SaveChangesAsync(); mail.MarkAsRead(); await transaction.CommitAsync();
        }
        await DrainOutgoingMail(f, recipient); Assert.Same(mail, InboxMail(inbox));
        using (var read = new CharacterContext(Configuration)) Assert.Equal(0, read.CharacterMail.Single().Flags & (byte)MailFlag.IsRead);
        using (var save = new CharacterContext(Configuration)) { inbox.Save(save); await save.SaveChangesAsync(); }
        using var final = new CharacterContext(Configuration); Assert.NotEqual(0, final.CharacterMail.Single().Flags & (byte)MailFlag.IsRead);
    }

    [Fact]
    public async Task OutgoingMailRepeatedSavesBeforeAdoptionPublishOnlyOnce()
    {
        var f = await MailFixture(); var (recipient, inbox) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        using (var first = new CharacterContext(Configuration)) { manager.Save(first); await first.SaveChangesAsync(); }
        mail.MarkAsRead();
        using (var second = new CharacterContext(Configuration)) { manager.Save(second); manager.Save(second); await second.SaveChangesAsync(); }
        await DrainOutgoingMail(f, recipient);
        f.Player.Session.Events.Update(0); recipient.Session.Events.Update(0);
        Assert.Single(BankMessages(recipient.Session).OfType<ServerMailAvailable>()); Assert.Same(mail, InboxMail(inbox));
        using var read = new CharacterContext(Configuration); Assert.NotEqual(0, Assert.Single(read.CharacterMail).Flags & (byte)MailFlag.IsRead);
    }

    [Fact]
    public async Task OutgoingMailReceiverReplacementCanRecoverTheOriginalAfterNotificationWasDropped()
    {
        var f = await MailFixture(); var (recipient, _) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); mail.MarkAsRead();
        var old = recipient.Session;
        var replacement = RecordingDispatchProxy<IGameSession>.Create(out var sp); sp.SetProperty(nameof(IGameSession.Events), new EventQueue());
        var pp = (RecordingDispatchProxy<IPlayer>)(object)recipient; pp.SetProperty(nameof(IPlayer.Session), replacement);
        ((RecordingDispatchProxy<IAccount>)(object)recipient.Account).SetProperty(nameof(IAccount.Session), replacement);
        var inbox = new MailManager(recipient, new CharacterModel(), realmId: 2, itemManager: f.Items,
            gameTableManager: f.Tables, deliveryContexts: Database().CreateContext); pp.SetProperty(nameof(IPlayer.MailManager), inbox);
        old.Events.Update(0); Assert.Empty(BankMessages(old));
        await PollMailbox(recipient, inbox); Assert.Same(mail, InboxMail(inbox));
        Assert.Equal(MailFlag.IsRead, mail.Flags & MailFlag.IsRead);
    }

    private sealed class LoseOutgoingAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("Lost outgoing mail commit acknowledgement."));
    }
}
