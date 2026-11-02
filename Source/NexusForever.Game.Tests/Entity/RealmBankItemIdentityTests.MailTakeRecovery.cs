using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Entity;
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
    public async Task MailAttachmentTakeRecoversAnActualPostCommitAcknowledgementFailure()
    {
        var fault = new LoseAttachmentAcknowledgement();
        int failedAcknowledgements = 0;
        var f = await MailFixture(command =>
        {
            var options = new DbContextOptionsBuilder<CharacterContext>(); options.UseConfiguration(Configuration); options.AddInterceptors(fault);
            // Keep the existing immutable command and its production Stage path;
            // replace only the private fresh-context factory for this injection.
            typeof(NexusForever.Game.RealmBank.RealmBankManager.BankSaveCommand).GetField("contexts",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(command, new Func<CharacterContext>(() => new CharacterContext(options.Options)));
            return command.ExecuteAsync(async attempt =>
            {
                try { await attempt(); }
                catch (IOException) { ++failedAcknowledgements; await attempt(); }
            });
        });
        var (manager, mail) = await IncomingAttachmentMail(f);
        manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(1, failedAcknowledgements);
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerItemAdd>());
        using var read = new CharacterContext(Configuration); Assert.Equal(100ul, Assert.Single(read.Item).OwnerId);
        Assert.Empty(read.CharacterMailAttachment); Assert.NotEqual(0, read.CharacterMail.Single().Flags & (byte)MailFlag.NotReturnable);
    }

    private sealed class LoseAttachmentAcknowledgement : DbTransactionInterceptor
    {
        private int lost;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            => Interlocked.Exchange(ref lost, 1) == 0
                ? Task.FromException(new IOException("Lost attachment take commit acknowledgement.")) : Task.CompletedTask;
    }

    [Fact]
    public async Task MailAttachmentTakeReplaysAFreshContextAfterStorageBeforeActorAdoption()
    {
        int attempts = 0;
        var f = await MailFixture(command => command.ExecuteAsync(async attempt =>
        { await attempt(); ++attempts; await attempt(); ++attempts; }));
        var (manager, mail) = await IncomingAttachmentMail(f);
        manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(2, attempts);
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(100ul, Assert.Single(read.Item).OwnerId); Assert.Empty(read.CharacterMailAttachment);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerItemAdd>());
    }

    [Theory]
    [InlineData("item")] [InlineData("character_mail")]
    public async Task MailAttachmentTakeRollsBackTheWholeUnitWhenItsItemOrHeaderWriteFails(string table)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingAttachmentMail(f);
        await SplitTrigger(table, "UPDATE"); manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(GenericError.DbFailure, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerItemAdd>());
        using var read = new CharacterContext(Configuration); Assert.Null(Assert.Single(read.Item).OwnerId);
        Assert.Single(read.CharacterMailAttachment); Assert.Equal(0, read.CharacterMail.Single().Flags & (byte)MailFlag.NotReturnable);
    }

    [Fact]
    public async Task MailAttachmentTakePreservesLaterReadAndItemIntentAfterTheStoredSnapshot()
    {
        var stored = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await MailFixture(command => command.ExecuteAsync(async attempt => { await attempt(); stored.SetResult(); await release.Task; }));
        var (manager, mail) = await IncomingAttachmentMail(f); var item = mail.GetAttachment(0).Item;
        try
        {
            manager.MailTakeAttachment(mail.Id, 0, 1); await stored.Task.WaitAsync(TimeSpan.FromSeconds(15));
            mail.MarkAsRead(); item.Durability = 0.25f;
            Assert.Empty(BankMessages(f.Player.Session));
        }
        finally { release.TrySetResult(); await WaitForBankMove(f.Inventory); }
        Assert.Same(item, f.Inventory.GetItem(900)); Assert.Null(mail.GetAttachment(0));
        using (var normal = new CharacterContext(Configuration)) { f.Inventory.Save(normal); manager.Save(normal); await normal.SaveChangesAsync(); }
        using var read = new CharacterContext(Configuration);
        Assert.Equal(0.25f, Assert.Single(read.Item).Durability);
        Assert.Equal((byte)(MailFlag.IsRead | MailFlag.NotReturnable), read.CharacterMail.Single().Flags);
    }

    [Fact]
    public async Task MailAttachmentTakePreparationFailureRestoresTheIncomingItemAndPermitsRetry()
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingAttachmentMail(f); var item = mail.GetAttachment(0).Item;
        var pp = (RecordingDispatchProxy<IPlayer>)(object)f.Player;
        pp.SetMethodHandler("get_IsLoading", _ => throw new InvalidOperationException("Injected mail preparation failure."));
        manager.MailTakeAttachment(mail.Id, 0, 1);
        Assert.Equal(GenericError.MailBusy, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        Assert.Null(item.CharacterId); Assert.Equal(InventoryLocation.None, item.Location);
        Assert.Null(f.Inventory.GetItem(900)); Assert.Same(item, mail.GetAttachment(0).Item);
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerItemAdd>());
        pp.SetMethodHandler("get_IsLoading", _ => false);
        manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(GenericError.Ok, BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>().Last().Result);
        using var read = new CharacterContext(Configuration); Assert.Equal(100ul, Assert.Single(read.Item).OwnerId); Assert.Empty(read.CharacterMailAttachment);
    }

    [Fact]
    public async Task MailAttachmentTakeLosingItsManagerAfterStorageCannotPublishSuccess()
    {
        var stored = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await MailFixture(command => command.ExecuteAsync(async attempt => { await attempt(); stored.SetResult(); await release.Task; }));
        var (manager, mail) = await IncomingAttachmentMail(f);
        try
        {
            manager.MailTakeAttachment(mail.Id, 0, 1); await stored.Task.WaitAsync(TimeSpan.FromSeconds(15));
            ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), f.Mail);
        }
        finally { release.TrySetResult(); await WaitForBankMove(f.Inventory); }
        Assert.Empty(BankMessages(f.Player.Session)); Assert.True(f.Inventory.RequiresAuthoritativeReload);
        using var read = new CharacterContext(Configuration); Assert.Equal(100ul, Assert.Single(read.Item).OwnerId); Assert.Empty(read.CharacterMailAttachment);
    }

    [Fact]
    public async Task MailAttachmentTakeExcludesOtherMailMutationsAndNormalSavingUntilSettlement()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await MailFixture(command => command.ExecuteAsync(async attempt => { await release.Task; await attempt(); }));
        var (manager, mail) = await IncomingAttachmentMail(f);
        try
        {
            manager.MailTakeAttachment(mail.Id, 0, 1);
            manager.MailDelete(mail.Id); manager.ReturnMail(mail.Id); manager.MailTakeCash(mail.Id, 1); manager.MailTakeAttachment(mail.Id, 0, 1);
            Assert.All(BankMessages(f.Player.Session).OfType<ServerMailResult>(), result => Assert.Equal(GenericError.MailBusy, result.Result));
            Assert.Equal(GenericError.MailBusy, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
            using var normal = new CharacterContext(Configuration);
            Assert.Throws<InvalidOperationException>(() => manager.Save(normal)); Assert.True(normal.IsSaveRejected);
        }
        finally { release.TrySetResult(); await WaitForBankMove(f.Inventory); }
        Assert.Equal(GenericError.Ok, BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>().Last().Result);
    }

    [Fact]
    public async Task MailAttachmentTakeDoesNotRequireASharedBankEntitlement()
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingAttachmentMail(f);
        ((RecordingDispatchProxy<IAccountEntitlementManager>)(object)f.Player.Account.EntitlementManager)
            .SetMethodHandler(nameof(IAccountEntitlementManager.GetEntitlement), _ => null);
        manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        using var read = new CharacterContext(Configuration); Assert.Equal(100ul, Assert.Single(read.Item).OwnerId);
    }

    [Fact]
    public async Task MailAttachmentTakeRejectsATriggerThatChangesASiblingItem()
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingAttachmentMail(f, 2);
        using (var setup = new CharacterContext(Configuration))
            await setup.Database.ExecuteSqlRawAsync(Configuration.Provider == DatabaseProvider.Sqlite
                ? "CREATE TRIGGER corrupt_mail_peer AFTER DELETE ON character_mail_attachment WHEN OLD.id=600 BEGIN UPDATE item SET stackCount=stackCount+1 WHERE id=901; END;"
                : "CREATE TRIGGER corrupt_mail_peer AFTER DELETE ON character_mail_attachment FOR EACH ROW BEGIN IF OLD.id=600 THEN UPDATE item SET stackCount=stackCount+1 WHERE id=901; END IF; END");
        manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(GenericError.DbFailure, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        var save = (Task)typeof(NexusForever.Game.Entity.Inventory).GetField("realmBankMoveSave",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(f.Inventory)!;
        Assert.Contains("Mail Item SQL differs", save.Exception?.ToString() ?? "Missing saved-task error.");
        using var read = new CharacterContext(Configuration); Assert.Equal(2, read.CharacterMailAttachment.Count());
        Assert.All(read.Item, item => { Assert.Null(item.OwnerId); Assert.Equal(17u, item.StackCount); });
    }

    [Fact]
    public async Task MailAttachmentTakeRetainsTheSchemaGuardAgainstDuplicateCustody()
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingAttachmentMail(f);
        using (var other = new CharacterContext(Configuration))
        {
            other.CharacterMail.Add(new CharacterMailModel { Id = 601, RecipientId = 200, SenderId = 100, SenderType = (byte)SenderType.Player, CreateTime = DateTime.UtcNow });
            other.CharacterMailAttachment.Add(new CharacterMailAttachmentModel { Id = 601, Index = 0, ItemGuid = 900 });
            await Assert.ThrowsAsync<DbUpdateException>(() => other.SaveChangesAsync());
        }
        manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMailAttachment); Assert.Equal(100ul, read.Item.Single().OwnerId);
    }

    [Fact]
    public async Task MailAttachmentTakeRejectsAnItemAlsoPresentInRealmBankStorage()
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingAttachmentMail(f);
        using (var other = new CharacterContext(Configuration)) { other.RealmBankItem.Add(BankRow(900)); await other.SaveChangesAsync(); }
        manager.MailTakeAttachment(mail.Id, 0, 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(GenericError.DbFailure, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>()).Result);
        using var read = new CharacterContext(Configuration); Assert.Single(read.CharacterMailAttachment);
        Assert.Equal(2, read.RealmBankItem.Count()); Assert.Null(read.Item.Single().OwnerId);
    }
}
