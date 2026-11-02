using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Fact]
    public async Task MailExpiryDeprecationWaitsForTheNormalCharacterCommit()
    {
        var f = await MailFixture(); var manager = await ExpiringVisibleMail(f);
        manager.Update(1d); Assert.Empty(BankMessages(f.Player.Session));
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(context); await context.SaveChangesAsync();
            f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session));
            await transaction.CommitAsync();
        }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        Assert.Equal(600ul, Assert.Single(Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailItemDeprecation>()).MailIds));
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMail);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MailExpiryDeprecationIncompleteCommitKeepsTheOutcomeForRetry(bool flush)
    {
        var f = await MailFixture(); var manager = await ExpiringVisibleMail(f);
        manager.Update(1d);
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(context);
            if (flush) { await context.SaveChangesAsync(); await transaction.RollbackAsync(); }
            else await transaction.CommitAsync();
        }
        f.Player.Session.Events.Update(0); Assert.Empty(BankMessages(f.Player.Session));
        using (var read = new CharacterContext(Configuration)) Assert.Single(read.CharacterMail);
        using (var retry = new CharacterContext(Configuration)) { manager.Save(retry); await retry.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailItemDeprecation>());
    }

    [Fact]
    public async Task MailExpiryDeprecationCommittedControlDeletesTheHeader()
    {
        var f = await MailFixture(); var manager = await ExpiringVisibleMail(f);
        manager.Update(1d);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        f.Player.Session.Events.Update(0); f.Player.Session.Events.Update(0);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailItemDeprecation>());
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMail);
    }

    private async Task<MailManager> ExpiringVisibleMail(MailState f, int count = 1, bool pending = false, bool atLogin = false, uint attachments = 0)
    {
        if (attachments > 0) await IncomingAttachmentMail(f, attachments);
        else await IncomingCashMail(f, false);
        using (var seed = new CharacterContext(Configuration))
        {
            if (pending) seed.CharacterMail.Single().DeliveryTime = (byte)DeliverySpeed.Hour;
            for (int i = 1; i < count; ++i)
                seed.CharacterMail.Add(new CharacterMailModel { Id = 600ul + (ulong)i, RecipientId = 100, SenderId = 200,
                    SenderType = (byte)SenderType.Player, Subject = "Expiry batch", Message = "Second expiry header", CreateTime = DateTime.UtcNow.AddMinutes(-1) });
            await seed.SaveChangesAsync();
        }
        using var read = new CharacterContext(Configuration);
        var model = new CharacterModel();
        foreach (var mail in read.CharacterMail.AsNoTracking().Include(mail => mail.Attachment).ThenInclude(link => link.Item).AsEnumerable().OrderBy(mail => mail.Id)) model.Mail.Add(mail);
        var config = new WorldConfig { MailExpiryDays = atLogin ? 0.000001f : 30f };
        var shared = RecordingDispatchProxy<ISharedConfiguration>.Create(out var proxy);
        proxy.SetMethodReturn(nameof(ISharedConfiguration.Get), config);
        var manager = new MailManager(f.Player, model, realmId: 2, itemManager: f.Items,
            gameTableManager: f.Tables, sharedConfiguration: shared, deliveryContexts: Database().CreateContext);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), manager);
        config.MailExpiryDays = 0.000001f;
        return manager;
    }
}
