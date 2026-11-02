using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public async Task MailExpiryDeprecationBatchIncompleteAcknowledgementKeepsEveryOutcome(int kind)
    {
        var f = await MailFixture(); var manager = await ExpiringVisibleMail(f, count: 2); manager.Update(1d);
        var options = new DbContextOptionsBuilder<CharacterContext>(); options.UseConfiguration(Configuration);
        if (kind == 2) options.AddInterceptors(new LoseOutgoingAcknowledgement());
        using (var context = new CharacterContext(options.Options))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (kind == 1) await transaction.CreateSavepointAsync("expiry_batch");
            manager.Save(context);
            if (kind != 0) await context.SaveChangesAsync();
            if (kind == 1) await transaction.RollbackToSavepointAsync("expiry_batch");
            if (kind == 2) await Assert.ThrowsAsync<IOException>(() => transaction.CommitAsync());
            else await transaction.CommitAsync();
        }
        f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session));
        using (var read = new CharacterContext(Configuration)) Assert.Equal(kind == 2 ? 0 : 2, read.CharacterMail.Count());
        using (var retry = new CharacterContext(Configuration)) { manager.Save(retry); await retry.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        Assert.Equal(new ulong[] { 600, 601 }, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailItemDeprecation>()).MailIds);
    }

    [Fact]
    public async Task MailExpiryDeprecationRetainsOneGroupedPacketAcrossRepeatedUpdates()
    {
        var f = await MailFixture(); var manager = await ExpiringVisibleMail(f, count: 2);
        manager.Update(1d); manager.Update(1d); manager.Update(1d); Assert.Empty(BankMessages(f.Player.Session));
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0); manager.Update(1d);
        Assert.Equal(new ulong[] { 600, 601 }, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailItemDeprecation>()).MailIds);
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerMailResult>());
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerMailUnavailable>());
    }

    [Fact]
    public async Task MailExpiryDeprecationSecondHeaderFailureRollsBackTheCharacterAndWholeBatch()
    {
        var f = await MailFixture(); var manager = await ExpiringVisibleMail(f, count: 2); manager.Update(1d);
        using (var trigger = new CharacterContext(Configuration))
            await trigger.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_expiry BEFORE DELETE ON character_mail WHEN OLD.id=601 BEGIN SELECT RAISE(ABORT, 'expiry batch failure'); END;");
        string original;
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var character = context.Character.Single(row => row.Id == 100); original = character.Name; character.Name = "ExpiryBatchRollback";
            manager.Save(context); var error = await Assert.ThrowsAnyAsync<Exception>(() => context.SaveChangesAsync());
            Assert.Contains("expiry batch failure", error.ToString(), StringComparison.Ordinal); await transaction.RollbackAsync();
        }
        f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session));
        using (var read = new CharacterContext(Configuration))
        {
            Assert.Equal(2, read.CharacterMail.Count()); Assert.Equal(original, read.Character.Single(row => row.Id == 100).Name);
            await read.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_expiry");
        }
        using (var retry = new CharacterContext(Configuration)) { manager.Save(retry); await retry.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        Assert.Equal(new ulong[] { 600, 601 }, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailItemDeprecation>()).MailIds);
    }

    [Fact]
    public async Task MailExpiryDeprecationOldManagerCannotPublishTheCommittedBatch()
    {
        var f = await MailFixture(); var manager = await ExpiringVisibleMail(f, count: 2); manager.Update(1d);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), f.Mail);
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session));
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMail);
    }

    [Fact]
    public async Task MailExpiryDeprecationDoesNotPublishToAReplacementSession()
    {
        var f = await MailFixture(); var manager = await ExpiringVisibleMail(f); var old = f.Player.Session; manager.Update(1d);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        var replacement = RecordingDispatchProxy<IGameSession>.Create(out var sp); sp.SetProperty(nameof(IGameSession.Events), new EventQueue());
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.Session), replacement);
        ((RecordingDispatchProxy<IAccount>)(object)f.Player.Account).SetProperty(nameof(IAccount.Session), replacement);
        old.Events.Update(0); old.Events.Update(0); replacement.Events.Update(0);
        Assert.Empty(BankMessages(old)); Assert.Empty(BankMessages(replacement));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MailExpiryDeprecationKeepsPendingAndLoginExpiredMailSilent(bool atLogin)
    {
        var f = await MailFixture(); var manager = await ExpiringVisibleMail(f, pending: !atLogin, atLogin: atLogin);
        manager.Update(1d);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session));
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMail);
    }

    [Fact]
    public async Task MailExpiryDeprecationDoesNotRequireASharedBankEntitlement()
    {
        var f = await MailFixture(); var manager = await ExpiringVisibleMail(f);
        ((RecordingDispatchProxy<IAccountEntitlementManager>)(object)f.Player.Account.EntitlementManager)
            .SetMethodHandler(nameof(IAccountEntitlementManager.GetEntitlement), _ => null);
        manager.Update(1d);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailItemDeprecation>());
    }

    [Fact]
    public async Task MailExpiryDeprecationWaitsForAFollowingNormalSaveScope()
    {
        var f = await MailFixture(); var manager = await ExpiringVisibleMail(f); manager.Update(1d);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        using (var later = new CharacterContext(Configuration))
        {
            using var transaction = await later.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(later); f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session));
            await transaction.RollbackAsync();
        }
        f.Player.Session.Events.Update(0); Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailItemDeprecation>());
    }

    [Fact]
    public async Task MailExpiryDeprecationPreservesTheExistingAttachmentItemRetentionPolicy()
    {
        var f = await MailFixture(); var manager = await ExpiringVisibleMail(f, attachments: 2); manager.Update(1d);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailItemDeprecation>());
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMail); Assert.Empty(read.CharacterMailAttachment);
        Assert.Equal(2, read.Item.Count(row => row.Id == 900 || row.Id == 901));
        Assert.All(read.Item.Where(row => row.Id == 900 || row.Id == 901), item => Assert.Null(item.OwnerId));
    }

    [Fact]
    public async Task MailExpiryDeprecationWaitsForAnOwnedCashReceiptBeforeExpiring()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await MailFixture(command => command.ExecuteAsync(async attempt => { await release.Task; await attempt(); }));
        var manager = await ExpiringVisibleMail(f);
        try
        {
            manager.MailTakeCash(600, 1); manager.Update(1d);
            Assert.Empty(BankMessages(f.Player.Session).OfType<ServerMailItemDeprecation>());
            Assert.NotNull(InboxMail(manager));
        }
        finally { release.TrySetResult(); await WaitForBankMove(f.Inventory); }
        manager.Update(1d);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailItemDeprecation>());
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMail); Assert.Equal(130ul, read.CharacterCurrency.Single().Amount);
    }

    [Fact]
    public async Task MailExpiryDeprecationWithoutAnActorQueueDeletesSilently()
    {
        var f = await MailFixture(); var manager = await ExpiringVisibleMail(f);
        ((RecordingDispatchProxy<IGameSession>)(object)f.Player.Session).SetProperty(nameof(IGameSession.Events), null);
        manager.Update(1d);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        Assert.Empty(BankMessages(f.Player.Session));
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMail);
    }
}
