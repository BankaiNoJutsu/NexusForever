using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Network.World.Message.Model.Mail;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OutgoingMailRollbackRetainsTheOfflineReturnForAFreshSave(bool flush)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, false);
        manager.ReturnMail(mail.Id);
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(context); if (flush) await context.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        using (var read = new CharacterContext(Configuration)) Assert.Equal(100ul, read.CharacterMail.Single().RecipientId);
        using (var retry = new CharacterContext(Configuration)) { manager.Save(retry); await retry.SaveChangesAsync(); }
        using var final = new CharacterContext(Configuration); var stored = Assert.Single(final.CharacterMail);
        Assert.Equal(200ul, stored.RecipientId); Assert.Equal("Returned: Incoming cash", stored.Subject);
    }

    [Fact]
    public async Task OutgoingMailRecipientPublicationWaitsForTheCharacterCommit()
    {
        var f = await MailFixture(); var (recipient, inbox) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(context); await context.SaveChangesAsync();
            Assert.Empty(BankMessages(recipient.Session));
            await transaction.CommitAsync();
        }
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (!BankMessages(recipient.Session).OfType<ServerMailAvailable>().Any())
        {
            f.Player.Session.Events.Update(0); recipient.Session.Events.Update(0); await Task.Delay(5, timeout.Token);
        }
        Assert.Single(BankMessages(recipient.Session).OfType<ServerMailAvailable>());
        Assert.Same(mail, InboxMail(inbox));
    }

    [Fact]
    public async Task OutgoingMailCommittedOfflineReturnKeepsOneStoredHeader()
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, false);
        manager.ReturnMail(mail.Id);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        using (var repeat = new CharacterContext(Configuration)) { manager.Save(repeat); await repeat.SaveChangesAsync(); }
        using var read = new CharacterContext(Configuration); Assert.Equal(200ul, Assert.Single(read.CharacterMail).RecipientId);
    }
}
