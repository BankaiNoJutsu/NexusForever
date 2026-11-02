using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
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
    [Theory]
    [InlineData(0u, false)] [InlineData(0u, true)] [InlineData(1u, false)] [InlineData(1u, true)]
    public async Task MailSelectionPreservesPartialAndFullBagCapacityResults(uint capacity, bool gift)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingAttachmentMail(f, 2, gift ? 30ul : 0ul);
        SelectionBag(f, capacity);
        manager.MailTakeAllFromSelection([mail.Id], 1); await WaitForBankMove(f.Inventory);
        using var read = new CharacterContext(Configuration);
        Assert.Equal((int)capacity, read.Item.Count(row => row.OwnerId == 100));
        Assert.Equal(2 - (int)capacity, read.CharacterMailAttachment.Count());
        Assert.Equal(gift ? 130ul : 100ul, read.CharacterCurrency.Single().Amount);
        var replies = BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>().ToArray();
        Assert.Equal(2, replies.Length); Assert.Equal((int)capacity, replies.Count(reply => reply.Result == GenericError.Ok));
        Assert.Equal(2 - (int)capacity, replies.Count(reply => reply.Result == GenericError.ItemInventoryFull));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MailSelectionSkipsUnpaidCodAndCollectsPaidCodAttachments(bool paid)
    {
        var f = await MailFixture(); await IncomingAttachmentMail(f, 2);
        using (var setup = new CharacterContext(Configuration))
        {
            var row = setup.CharacterMail.Single(); row.IsCashOnDelivery = 1; row.HasPaidOrCollectedCurrency = paid ? (byte)1 : (byte)0;
            await setup.SaveChangesAsync();
        }
        var manager = ReloadSelectionMailbox(f);
        manager.MailTakeAllFromSelection([600ul], 1); await WaitForBankMove(f.Inventory);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(paid ? 2 : 0, read.Item.Count(row => row.OwnerId == 100));
        Assert.Equal(paid ? 0 : 2, read.CharacterMailAttachment.Count()); Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(paid ? 2 : 0, BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>().Count());
    }

    [Fact]
    public async Task MailSelectionOutOfRangeRejectsCashAndEveryAttachmentWithoutMutation()
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingAttachmentMail(f, 2);
        manager.MailTakeAllFromSelection([mail.Id], 0);
        Assert.Equal(GenericError.MailMailBoxOutOfRange, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        Assert.Equal(2, BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>().Count());
        Assert.All(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>(), reply => Assert.Equal(GenericError.MailMailBoxOutOfRange, reply.Result));
        using var read = new CharacterContext(Configuration); Assert.Equal(2, read.CharacterMailAttachment.Count());
        Assert.All(read.Item, item => Assert.Null(item.OwnerId)); Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
    }

    [Fact]
    public async Task MailSelectionCapturesMultipleMailsAndDuplicateSelectionIdsOnlyOnce()
    {
        var f = await MailFixture(); await IncomingAttachmentMail(f, 2);
        using (var setup = new CharacterContext(Configuration))
        {
            setup.CharacterMail.Add(new CharacterMailModel { Id = 601, RecipientId = 100, SenderId = 200, SenderType = (byte)SenderType.Player,
                CurrencyType = (byte)CurrencyType.Credits, CurrencyAmount = 20, CreateTime = DateTime.UtcNow });
            var item = CharacterRow(); item.Id = 902; item.OwnerId = null; item.Location = (ushort)InventoryLocation.None;
            setup.Item.Add(item); setup.CharacterMailAttachment.Add(new CharacterMailAttachmentModel { Id = 601, Index = 0, ItemGuid = 902 });
            await setup.SaveChangesAsync();
        }
        var manager = ReloadSelectionMailbox(f);
        manager.MailTakeAllFromSelection([600ul, 601ul, 600ul], 1); await WaitForBankMove(f.Inventory);
        using var read = new CharacterContext(Configuration); Assert.Equal(3, read.Item.Count());
        Assert.All(read.Item, item => Assert.Equal(100ul, item.OwnerId)); Assert.Empty(read.CharacterMailAttachment);
        Assert.Equal(150ul, read.CharacterCurrency.Single().Amount); Assert.All(read.CharacterMail, mail => Assert.Equal(1, mail.HasPaidOrCollectedCurrency));
        Assert.Equal(3, BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>().Count());
        Assert.Equal(2, BankMessages(f.Player.Session).OfType<ServerMailResult>().Count());
    }

    [Theory]
    [InlineData("item", "UPDATE")] [InlineData("character_mail", "UPDATE")]
    [InlineData("character_mail_attachment", "DELETE")] [InlineData("character_currency", "UPDATE")]
    public async Task MailSelectionRollsBackEveryItemLinkAndGiftWhenAnyComponentFails(string table, string operation)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingAttachmentMail(f, 2);
        await SplitTrigger(table, operation); manager.MailTakeAllFromSelection([mail.Id], 1); await WaitForBankMove(f.Inventory);
        using var read = new CharacterContext(Configuration); Assert.All(read.Item, item => Assert.Null(item.OwnerId));
        Assert.Equal(2, read.CharacterMailAttachment.Count()); Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(0, read.CharacterMail.Single().HasPaidOrCollectedCurrency); Assert.Equal(0, read.CharacterMail.Single().Flags & (byte)MailFlag.NotReturnable);
        Assert.Equal(2, BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>().Count());
        Assert.All(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>(), reply => Assert.Equal(GenericError.DbFailure, reply.Result));
        Assert.Equal(GenericError.DbFailure, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerItemAdd>());
    }

    [Fact]
    public async Task MailSelectionDefersEverySuccessUntilTheWholeCapturedUnitCommits()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await MailFixture(command => command.ExecuteAsync(async attempt => { await release.Task; await attempt(); }));
        var (manager, mail) = await IncomingAttachmentMail(f, 2);
        try
        {
            manager.MailTakeAllFromSelection([mail.Id], 1); Assert.Empty(BankMessages(f.Player.Session));
            using var read = new CharacterContext(Configuration); Assert.Equal(2, read.CharacterMailAttachment.Count());
            Assert.All(read.Item, item => Assert.Null(item.OwnerId)); Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
        }
        finally { release.TrySetResult(); await WaitForBankMove(f.Inventory); }
        Assert.Equal(2, BankMessages(f.Player.Session).OfType<ServerItemAdd>().Count());
        Assert.All(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>(), reply => Assert.Equal(GenericError.Ok, reply.Result));
    }

    [Fact]
    public async Task MailSelectionRecoversAnActualPostCommitAcknowledgementFailure()
    {
        var fault = new LoseAttachmentAcknowledgement(); int lost = 0;
        var f = await MailFixture(command =>
        {
            var options = new DbContextOptionsBuilder<CharacterContext>(); options.UseConfiguration(Configuration); options.AddInterceptors(fault);
            typeof(NexusForever.Game.RealmBank.RealmBankManager.BankSaveCommand).GetField("contexts", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(command, new Func<CharacterContext>(() => new CharacterContext(options.Options)));
            return command.ExecuteAsync(async attempt => { try { await attempt(); } catch (IOException) { ++lost; await attempt(); } });
        });
        var (manager, mail) = await IncomingAttachmentMail(f, 2);
        manager.MailTakeAllFromSelection([mail.Id, mail.Id], 1); await WaitForBankMove(f.Inventory);
        Assert.Equal(1, lost); using var read = new CharacterContext(Configuration);
        Assert.All(read.Item, item => Assert.Equal(100ul, item.OwnerId)); Assert.Empty(read.CharacterMailAttachment);
        Assert.Equal(130ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(2, BankMessages(f.Player.Session).OfType<ServerItemAdd>().Count());
        Assert.Equal(2, BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>().Count());
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
    }

    [Fact]
    public async Task MailSelectionPreparationFailureRestoresEveryIncomingItemAndItsReceipts()
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingAttachmentMail(f, 2);
        var originals = mail.ToArray(); int calls = 0;
        var player = (RecordingDispatchProxy<IPlayer>)(object)f.Player;
        player.SetMethodHandler("get_IsLoading", _ => ++calls == 2 ? throw new InvalidOperationException("Second selection Item preparation failure.") : false);
        manager.MailTakeAllFromSelection([mail.Id], 1);
        Assert.All(originals, link => { Assert.Null(link.Item.CharacterId); Assert.Equal(InventoryLocation.None, link.Item.Location); Assert.Null(f.Inventory.GetItem(link.Item.Guid)); });
        Assert.Equal(2, mail.Count()); Assert.Empty(BankMessages(f.Player.Session).OfType<ServerItemAdd>());
        Assert.All(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>(), reply => Assert.Equal(GenericError.MailBusy, reply.Result));
        player.SetMethodHandler("get_IsLoading", _ => false);
        manager.MailTakeAllFromSelection([mail.Id], 1); await WaitForBankMove(f.Inventory);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMailAttachment);
        Assert.All(read.Item, item => Assert.Equal(100ul, item.OwnerId)); Assert.Equal(130ul, read.CharacterCurrency.Single().Amount);
    }

    [Fact]
    public async Task MailSelectionPreservesLaterReadAndItemIntentAcrossTheCapturedSnapshot()
    {
        var stored = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await MailFixture(command => command.ExecuteAsync(async attempt => { await attempt(); stored.SetResult(); await release.Task; }));
        var (manager, mail) = await IncomingAttachmentMail(f, 2); var originals = mail.ToArray();
        try
        {
            manager.MailTakeAllFromSelection([mail.Id], 1); await stored.Task.WaitAsync(TimeSpan.FromSeconds(15));
            mail.MarkAsRead(); originals[0].Item.Durability = 0.25f; originals[1].Item.StackCount = 16;
            Assert.Empty(BankMessages(f.Player.Session));
        }
        finally { release.TrySetResult(); await WaitForBankMove(f.Inventory); }
        using (var normal = new CharacterContext(Configuration)) { f.Inventory.Save(normal); manager.Save(normal); await normal.SaveChangesAsync(); }
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMailAttachment);
        Assert.Equal(0.25f, read.Item.Single(row => row.Id == 900).Durability); Assert.Equal(16u, read.Item.Single(row => row.Id == 901).StackCount);
        Assert.Equal((byte)(MailFlag.IsRead | MailFlag.NotReturnable), read.CharacterMail.Single().Flags);
    }

    [Fact]
    public async Task MailSelectionCannotPublishToAReplacementManagerAfterStorage()
    {
        var stored = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await MailFixture(command => command.ExecuteAsync(async attempt => { await attempt(); stored.SetResult(); await release.Task; }));
        var (manager, mail) = await IncomingAttachmentMail(f, 2);
        try
        {
            manager.MailTakeAllFromSelection([mail.Id], 1); await stored.Task.WaitAsync(TimeSpan.FromSeconds(15));
            ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), f.Mail);
        }
        finally { release.TrySetResult(); await WaitForBankMove(f.Inventory); }
        Assert.Empty(BankMessages(f.Player.Session)); Assert.True(f.Inventory.RequiresAuthoritativeReload);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMailAttachment); Assert.Equal(130ul, read.CharacterCurrency.Single().Amount);
        Assert.All(read.Item, item => Assert.Equal(100ul, item.OwnerId));
    }

    [Fact]
    public async Task MailSelectionBusyRequestsCannotStartASecondReceiptOrCredit()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await MailFixture(command => command.ExecuteAsync(async attempt => { await release.Task; await attempt(); }));
        var (manager, mail) = await IncomingAttachmentMail(f, 2);
        try
        {
            manager.MailTakeAllFromSelection([mail.Id], 1); manager.MailTakeAllFromSelection([mail.Id], 1);
            Assert.Equal(GenericError.MailBusy, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
            Assert.Equal(2, BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>().Count());
            Assert.All(BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>(), reply => Assert.Equal(GenericError.MailBusy, reply.Result));
        }
        finally { release.TrySetResult(); await WaitForBankMove(f.Inventory); }
        using var read = new CharacterContext(Configuration); Assert.Equal(130ul, read.CharacterCurrency.Single().Amount); Assert.Empty(read.CharacterMailAttachment);
    }

    [Fact]
    public async Task MailSelectionDoesNotRequireASharedBankEntitlement()
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingAttachmentMail(f, 2);
        ((RecordingDispatchProxy<IAccountEntitlementManager>)(object)f.Player.Account.EntitlementManager)
            .SetMethodHandler(nameof(IAccountEntitlementManager.GetEntitlement), _ => null);
        manager.MailTakeAllFromSelection([mail.Id], 1); await WaitForBankMove(f.Inventory);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMailAttachment); Assert.Equal(130ul, read.CharacterCurrency.Single().Amount);
        Assert.All(read.Item, item => Assert.Equal(100ul, item.OwnerId));
    }

    [Fact]
    public async Task MailSelectionUsesExactLinksWithoutReinterpretingStoredReplyIndices()
    {
        var f = await MailFixture(); await IncomingAttachmentMail(f, 2, 0);
        using (var setup = new CharacterContext(Configuration))
        {
            await setup.CharacterMailAttachment.ExecuteDeleteAsync();
            setup.CharacterMailAttachment.AddRange(new CharacterMailAttachmentModel { Id = 600, Index = 3, ItemGuid = 900 },
                new CharacterMailAttachmentModel { Id = 600, Index = 8, ItemGuid = 901 });
            await setup.SaveChangesAsync();
        }
        var manager = ReloadSelectionMailbox(f); manager.MailTakeAllFromSelection([600ul], 1); await WaitForBankMove(f.Inventory);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.CharacterMailAttachment);
        Assert.All(read.Item, item => Assert.Equal(100ul, item.OwnerId));
        Assert.Equal(new uint[] { 3, 8 }, BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>().Select(reply => reply.Index).Order().ToArray());
    }

    private static void SelectionBag(MailState f, uint capacity)
    {
        var bags = (Dictionary<InventoryLocation, IBag>)typeof(Inventory).GetField("bags", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(f.Inventory)!;
        bags[InventoryLocation.Inventory] = new Bag(InventoryLocation.Inventory, capacity, f.Items);
    }

    private MailManager ReloadSelectionMailbox(MailState f)
    {
        using var read = new CharacterContext(Configuration);
        var model = new CharacterModel();
        foreach (var row in read.CharacterMail.AsNoTracking().Where(row => row.RecipientId == 100).Include(row => row.Attachment).ThenInclude(row => row.Item)) model.Mail.Add(row);
        var manager = new MailManager(f.Player, model, realmId: 2, itemManager: f.Items, gameTableManager: f.Tables, deliveryContexts: Database().CreateContext);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), manager);
        return manager;
    }
}
