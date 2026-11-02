using System.Reflection;
using Identity = NexusForever.Game.Abstract.Identity;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Fact]
    public async Task BankPaymentCannotAdoptWithoutAConfirmedSqlAttempt()
    {
        var f = await MailFixture();
        var payment = f.Currency.CaptureBankCurrency(f.Player, [new(CurrencyType.Credits, 20, true)], () => true, _ => { });
        Assert.Throws<InvalidOperationException>(payment.Adopt); payment.Abort();
        Assert.Equal(100ul, Assert.Single(f.Currency).Amount);
        using var read = new CharacterContext(Configuration); Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
    }

    [Fact]
    public async Task MissingCharacterDatabaseCannotAcknowledgePlayerMail()
    {
        var f = await MailFixture();
        typeof(Inventory).GetField("realmBankManager", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(f.Inventory, new NexusForever.Game.RealmBank.RealmBankManager(null, f.Items));
        f.Mail.SendMail(MailRequest([401ul], 30)); await WaitForBankMove(f.Inventory, f.Item);
        Assert.True(f.Inventory.RequiresAuthoritativeReload);
        Assert.Equal(GenericError.DbFailure, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        using var read = new CharacterContext(Configuration); Assert.Single(read.RealmBankItem); Assert.Empty(read.CharacterMail);
        Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
    }

    [Fact]
    public async Task AReplacedMailManagerCannotInitiateAPlayerSend()
    {
        var f = await MailFixture();
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), RecordingDispatchProxy<IMailManager>.Create(out _));
        f.Mail.SendMail(MailRequest([401ul], 30));
        Assert.Equal(GenericError.MailBusy, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        Assert.Same(f.Item, f.Inventory.GetItem(401ul)); Assert.Equal(100ul, Assert.Single(f.Currency).Amount);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMail); Assert.Single(read.RealmBankItem);
    }

    [Fact]
    public async Task CommittedMailNotificationChecksTheActualRecipientCharacter()
    {
        var f = await MailFixture(); var (wrong, inbox) = RecipientMailbox(f);
        var proxy = (RecordingDispatchProxy<IPlayer>)(object)wrong;
        proxy.SetProperty(nameof(IPlayer.CharacterId), 201ul); proxy.SetProperty(nameof(IPlayer.Identity), new Identity { RealmId = 2, Id = 201 });
        typeof(MailManager).GetField("deliveryCharacter", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(inbox, 201ul);
        f.Mail.SendMail(MailRequest([401ul])); await WaitForBankMove(f.Inventory, f.Item);
        for (int i = 0; i < 10; ++i) { f.Player.Session.Events.Update(0); await Task.Delay(5); }
        wrong.Session.Events.Update(0); Assert.Empty(BankMessages(wrong.Session));
        using var read = new CharacterContext(Configuration); Assert.Equal(200ul, read.CharacterMail.Single().RecipientId);
    }

    [Fact]
    public async Task InboxRefreshFindsALaterCommitWithALowerIdAndSuppressesKnownRows()
    {
        var f = await MailFixture(); var (recipient, inbox) = RecipientMailbox(f);
        f.Mail.SendMail(MailRequest([], 30)); await WaitForBankMove(f.Inventory, f.Item); await PollMailbox(recipient, inbox);
        Assert.Single(BankMessages(recipient.Session).OfType<ServerMailAvailable>());
        using (var insert = new CharacterContext(Configuration))
        {
            insert.CharacterMail.Add(new CharacterMailModel { Id = 400, RecipientId = 200, SenderType = 0, Subject = "Late lower id", Message = "Late", CreateTime = DateTime.UtcNow.AddMinutes(-1) });
            await insert.SaveChangesAsync();
        }
        await PollMailbox(recipient, inbox);
        var packets = BankMessages(recipient.Session).OfType<ServerMailAvailable>().ToArray(); Assert.Equal(2, packets.Length);
        Assert.Equal(400ul, Assert.Single(packets[1].MailList).MailId);
        await PollMailbox(recipient, inbox); Assert.Equal(2, BankMessages(recipient.Session).OfType<ServerMailAvailable>().Count());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlayerMailPersistsExistingAndNewCarriedAttachmentsInTheSameUnit(bool created)
    {
        var f = await MailFixture();
        NexusForever.Game.Entity.Item attachment;
        if (created)
        {
            ((RecordingDispatchProxy<NexusForever.Game.Abstract.IItemManager>)(object)f.Items).SetProperty(nameof(NexusForever.Game.Abstract.IItemManager.NextItemId), 205ul);
            attachment = new NexusForever.Game.Entity.Item(100, f.Items.GetItemInfo(7001), 2, itemManager: f.Items);
            f.Inventory.LoadItem(attachment, InventoryLocation.Inventory, 0);
        }
        else
        {
            f.Inventory.ItemMove(f.Item, new ItemLocation { Location = InventoryLocation.Inventory, BagIndex = 0 });
            await WaitForBankMove(f.Inventory, f.Item); attachment = f.Item;
        }
        f.Mail.SendMail(MailRequest([attachment.Guid], 30)); await WaitForBankMove(f.Inventory, attachment);
        Assert.False(f.Inventory.RequiresAuthoritativeReload); Assert.False(attachment.PendingCreate);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(attachment.Guid, Assert.Single(read.CharacterMailAttachment).ItemGuid);
        Assert.Null(Assert.Single(read.Item).OwnerId); Assert.Equal(created ? 1 : 0, read.RealmBankItem.Count());
        Assert.Equal(50ul, read.CharacterCurrency.Single().Amount);
    }

    [Fact]
    public async Task MailWithoutAttachmentsCannotCommitOnlyItsPayment()
    {
        var f = await MailFixture(); await SplitTrigger("character_mail", "INSERT");
        f.Mail.SendMail(MailRequest([], 30)); await WaitForBankMove(f.Inventory, f.Item);
        using var read = new CharacterContext(Configuration);
        Assert.Empty(read.CharacterMail); Assert.Equal(100ul, read.CharacterCurrency.Single().Amount); Assert.Single(read.RealmBankItem);
        Assert.Equal(GenericError.DbFailure, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        Assert.True(f.Inventory.RequiresAuthoritativeReload);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MailRetryRetainsOneIdentityAndDebitsOnlyOnce(bool lostAck)
    {
        int attempts = 0;
        var f = await MailFixture(command => command.ExecuteAsync(async attempt =>
        {
            try
            {
                ++attempts; await attempt();
                if (lostAck) throw new IOException("Lost actual commit acknowledgement.");
            }
            catch (Exception error) when (lostAck ? error is IOException : error is DbUpdateException)
            {
                if (!lostAck) { using var drop = new CharacterContext(Configuration); await drop.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_split"); }
                ++attempts; await attempt();
            }
        }));
        if (!lostAck) await SplitTrigger("character_mail_attachment", "INSERT");
        f.Mail.SendMail(MailRequest([401ul], 30)); Assert.Empty(BankMessages(f.Player.Session));
        await WaitForBankMove(f.Inventory, f.Item);
        Assert.Equal(2, attempts); Assert.False(f.Inventory.RequiresAuthoritativeReload);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(501ul, Assert.Single(read.CharacterMail).Id); Assert.Equal(401ul, Assert.Single(read.CharacterMailAttachment).ItemGuid);
        Assert.Equal(50ul, Assert.Single(read.CharacterCurrency).Amount); Assert.Equal(50ul, Assert.Single(f.Currency).Amount);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>());
    }

    [Theory]
    [InlineData(0ul)]
    [InlineData(30ul)]
    public async Task MailWithoutAttachmentsStillOwnsItsHeaderAndDebit(ulong gift)
    {
        var f = await MailFixture(); f.Mail.SendMail(MailRequest([], gift)); await WaitForBankMove(f.Inventory, f.Item);
        using var read = new CharacterContext(Configuration);
        Assert.Single(read.RealmBankItem); Assert.Empty(read.Item); Assert.Empty(read.CharacterMailAttachment);
        Assert.Equal(gift, Assert.Single(read.CharacterMail).CurrencyAmount); Assert.Equal(80ul - gift, Assert.Single(read.CharacterCurrency).Amount);
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        Assert.False(f.Inventory.RequiresAuthoritativeReload);
    }

    [Theory]
    [InlineData("recipient")]
    [InlineData("account")]
    [InlineData("collision")]
    [InlineData("rewrite")]
    [InlineData("extra")]
    public async Task MailConflictingSqlWitnessRejectsTheWholeUnit(string conflict)
    {
        var f = await MailFixture(command => command.ExecuteAsync(async attempt =>
        {
            using (var alter = new CharacterContext(Configuration))
            {
                if (conflict == "recipient") { alter.Character.Single(c => c.Id == 200).DeleteTime = DateTime.UtcNow; await alter.SaveChangesAsync(); }
                if (conflict == "account") { alter.Character.Single(c => c.Id == 100).AccountId = 99; await alter.SaveChangesAsync(); }
            }
            await attempt();
        }));
        using (var alter = new CharacterContext(Configuration))
        {
            if (conflict == "collision")
            {
                alter.CharacterMail.Add(new CharacterMailModel { Id = 501, RecipientId = 200, Subject = "Prior", Message = "Prior", CreateTime = DateTime.UtcNow });
                await alter.SaveChangesAsync();
            }
            if (conflict == "rewrite") await alter.Database.ExecuteSqlRawAsync(Configuration.Provider == DatabaseProvider.Sqlite
                ? "CREATE TRIGGER rewrite_mail AFTER INSERT ON character_mail BEGIN UPDATE character_mail SET currencyAmount=999 WHERE id=NEW.id; END;"
                : "CREATE TRIGGER rewrite_mail BEFORE INSERT ON character_mail FOR EACH ROW SET NEW.currencyAmount=999");
            if (conflict == "extra")
            {
                var extra = CharacterRow(); extra.Id = 900; alter.Item.Add(extra); await alter.SaveChangesAsync();
                await alter.Database.ExecuteSqlRawAsync(Configuration.Provider == DatabaseProvider.Sqlite
                    ? "CREATE TRIGGER extra_mail AFTER INSERT ON character_mail_attachment WHEN NEW.\"index\"=0 BEGIN INSERT INTO character_mail_attachment (id, \"index\", itemGuid) VALUES (NEW.id,9,900); END;"
                    : "CREATE TRIGGER extra_mail AFTER INSERT ON character_mail_attachment FOR EACH ROW INSERT INTO character_mail_attachment (id, `index`, itemGuid) VALUES (NEW.id,9,900)");
            }
        }
        f.Mail.SendMail(MailRequest([401ul], 30)); await WaitForBankMove(f.Inventory, f.Item);
        Assert.True(f.Inventory.RequiresAuthoritativeReload);
        Assert.Equal(GenericError.DbFailure, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        using var read = new CharacterContext(Configuration);
        Assert.Single(read.RealmBankItem); Assert.DoesNotContain(read.Item, i => i.Id == 401); Assert.Empty(read.CharacterMailAttachment);
        Assert.Equal(conflict == "collision" ? 1 : 0, read.CharacterMail.Count()); Assert.Equal(100ul, Assert.Single(read.CharacterCurrency).Amount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MailInboxUsesTheCommittedOriginalAndPreservesLaterDirtyItemAndCurrencyIntent(bool delayed)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await MailFixture(command => command.ExecuteAsync(async attempt => { entered.SetResult(); await release.Task; await attempt(); }));
        var (recipient, inbox) = RecipientMailbox(f);
        f.Mail.SendMail(MailRequest([401ul], 30, speed: delayed ? NexusForever.Game.Static.Mail.DeliverySpeed.Hour : NexusForever.Game.Static.Mail.DeliverySpeed.Instant));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
        f.Item.StackCount = 16; f.Item.MicrochipIds.Add(99);
        f.Currency.CurrencyAddAmount(CurrencyType.Credits, 7);
        Assert.Equal(100ul, Assert.Single(f.Currency).Amount); Assert.Empty(BankMessages(recipient.Session));
        release.SetResult(); await WaitForBankMove(f.Inventory, f.Item);
        await PollMailbox(recipient, inbox);
        var packets = BankMessages(recipient.Session).OfType<ServerMailAvailable>().ToArray();
        Assert.Equal(delayed ? 0 : 1, packets.Length);
        var mail = delayed ? Assert.Single((List<NexusForever.Game.Abstract.Mail.IMailItem>)typeof(MailManager)
            .GetField("pendingMail", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(inbox)!) : InboxMail(inbox);
        Assert.Same(f.Item, mail.GetAttachment(0).Item); Assert.False(mail.PendingCreate);
        Assert.Equal(57ul, Assert.Single(f.Currency).Amount);
        using (var read = new CharacterContext(Configuration)) { Assert.Equal(17u, read.Item.Single().StackCount); Assert.Equal(50ul, read.CharacterCurrency.Single().Amount); }
        using (var save = new CharacterContext(Configuration)) { inbox.Save(save); f.Currency.Save(save); await save.SaveChangesAsync(); }
        using var final = new CharacterContext(Configuration);
        Assert.Single(final.CharacterMail); Assert.Single(final.CharacterMailAttachment); Assert.Equal(16u, final.Item.Single().StackCount);
        Assert.Contains("99", final.Item.Single().MicrochipIds); Assert.Equal(57ul, final.CharacterCurrency.Single().Amount);
        await PollMailbox(recipient, inbox); Assert.Equal(delayed ? 0 : 1, BankMessages(recipient.Session).OfType<ServerMailAvailable>().Count());
    }

    [Fact]
    public async Task MailCommittedAfterSenderFinalizationIsRecoveredByTheRecipientActor()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await MailFixture(command => command.ExecuteAsync(async attempt => { entered.SetResult(); await release.Task; await attempt(); }));
        var (recipient, inbox) = RecipientMailbox(f); var old = f.Player.Session;
        f.Mail.SendMail(MailRequest([401ul], 30)); await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        release.SetResult();
        var completion = (Task)typeof(Inventory).GetField("realmBankMoveSave", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f.Inventory)!;
        await completion.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Empty(BankMessages(old)); Assert.True(f.Inventory.RequiresAuthoritativeReload);
        await PollMailbox(recipient, inbox);
        Assert.Single(BankMessages(recipient.Session).OfType<ServerMailAvailable>());
        Assert.NotSame(f.Item, InboxMail(inbox).GetAttachment(0).Item);
        using var read = new CharacterContext(Configuration); Assert.Equal(50ul, read.CharacterCurrency.Single().Amount); Assert.Single(read.CharacterMailAttachment);
    }

    [Fact]
    public async Task AReplacedRecipientSessionCannotPublishAnAlreadyLoadedInbox()
    {
        var f = await MailFixture(); var (recipient, inbox) = RecipientMailbox(f);
        f.Mail.SendMail(MailRequest([401ul])); await WaitForBankMove(f.Inventory, f.Item);
        var old = recipient.Session; inbox.Update(30);
        ((RecordingDispatchProxy<IPlayer>)(object)recipient).SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        for (int i = 0; i < 100; ++i) { old.Events.Update(0); await Task.Delay(5); }
        Assert.Empty(BankMessages(old));
        using var read = new CharacterContext(Configuration); Assert.Single(read.CharacterMail);
    }

    private (IPlayer Player, MailManager Inbox) RecipientMailbox(MailState f)
    {
        var (_, recipient, _) = Access(); var proxy = (RecordingDispatchProxy<IPlayer>)(object)recipient;
        proxy.SetProperty(nameof(IPlayer.CharacterId), 200ul); proxy.SetProperty(nameof(IPlayer.Identity), new Identity { RealmId = 2, Id = 200 });
        ((RecordingDispatchProxy<IAccount>)(object)recipient.Account).SetProperty(nameof(IAccount.Id), 13u);
        var session = RecordingDispatchProxy<IGameSession>.Create(out var sp); sp.SetProperty(nameof(IGameSession.Events), new EventQueue());
        proxy.SetProperty(nameof(IPlayer.Session), session); ((RecordingDispatchProxy<IAccount>)(object)recipient.Account).SetProperty(nameof(IAccount.Session), session);
        proxy.SetProperty(nameof(IPlayer.Inventory), RecordingDispatchProxy<IInventory>.Create(out _));
        var inbox = new MailManager(recipient, new CharacterModel(), realmId: 2, itemManager: f.Items, gameTableManager: f.Tables, deliveryContexts: Database().CreateContext);
        proxy.SetProperty(nameof(IPlayer.MailManager), inbox);
        ((RecordingDispatchProxy<IPlayerManager>)(object)f.Players).SetMethodReturn(nameof(IPlayerManager.GetPlayer), recipient);
        return (recipient, inbox);
    }
    private static NexusForever.Game.Abstract.Mail.IMailItem InboxMail(MailManager inbox)
        => Assert.Single((Dictionary<ulong, NexusForever.Game.Abstract.Mail.IMailItem>)typeof(MailManager)
            .GetField("availableMail", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(inbox)!).Value;
    private static async Task PollMailbox(IPlayer recipient, MailManager inbox)
    {
        inbox.Update(30);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while ((bool)typeof(MailManager).GetField("deliveryLoading", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(inbox)!)
        { recipient.Session.Events.Update(0); await Task.Delay(5, timeout.Token); }
        recipient.Session.Events.Update(0);
    }
}
