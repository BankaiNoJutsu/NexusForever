using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ManualMailActionCommittedControlPersistsOneOutcome(bool returning)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, false);
        if (returning) manager.ReturnMail(mail.Id); else manager.MailDelete(mail.Id);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailUnavailable>());
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        using var read = new CharacterContext(Configuration);
        if (returning) Assert.Equal(200ul, Assert.Single(read.CharacterMail).RecipientId);
        else Assert.Empty(read.CharacterMail);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ManualMailActionPublishesOnlyAfterTheNormalCharacterCommit(bool returning)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, false);
        if (returning) manager.ReturnMail(mail.Id); else manager.MailDelete(mail.Id);
        Assert.Empty(BankMessages(f.Player.Session));
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(context); await context.SaveChangesAsync();
            f.Player.Session.Events.Update(0);
            Assert.Empty(BankMessages(f.Player.Session));
            await transaction.CommitAsync();
        }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailUnavailable>());
        var result = Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>());
        Assert.Equal(GenericError.Ok, result.Result);
        using var read = new CharacterContext(Configuration);
        if (returning) Assert.Equal(200ul, Assert.Single(read.CharacterMail).RecipientId);
        else Assert.Empty(read.CharacterMail);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ManualMailActionRollbackRetainsTheUnpublishedOutcomeForAFreshSave(bool returning)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, false);
        if (returning) manager.ReturnMail(mail.Id); else manager.MailDelete(mail.Id);
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(context); await context.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        f.Player.Session.Events.Update(0);
        Assert.Empty(BankMessages(f.Player.Session));
        using (var read = new CharacterContext(Configuration)) Assert.Equal(100ul, Assert.Single(read.CharacterMail).RecipientId);
        using (var retry = new CharacterContext(Configuration)) { manager.Save(retry); await retry.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailUnavailable>());
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
    }
}
