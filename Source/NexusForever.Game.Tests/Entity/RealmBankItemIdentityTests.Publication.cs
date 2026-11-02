using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Entity;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BankMoveWaitsForSqlAndItsOriginalActorBeforePublication(bool bankSource)
    {
        var (inventory, _, player, _, source) = await SplitFixture(bankSource);
        var events = InstallBankActor(player.Session);
        inventory.ItemMove(source, SplitTarget(!bankSource));
        await WaitForStoredBankChange(bankSource, 401);
        Assert.Empty(BankMessages(player.Session));
        Assert.True(inventory.IsSaveBlocked);
        events.Update(0); await WaitForBankMove(inventory, source);
        Assert.IsType<ServerItemMove>(Assert.Single(BankMessages(player.Session)));
        Assert.False(inventory.IsSaveBlocked);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectedBankMoveNeverPublishesItsPreparedSuccessPacket(bool bankSource)
    {
        var (inventory, _, player, _, source) = await SplitFixture(bankSource);
        InstallBankActor(player.Session);
        await SplitTrigger(bankSource ? "item" : "realm_bank_item", "INSERT");
        inventory.ItemMove(source, SplitTarget(!bankSource)); await WaitForBankMove(inventory, source);
        Assert.Empty(BankMessages(player.Session).OfType<ServerItemMove>());
        Assert.IsType<ServerItemError>(Assert.Single(BankMessages(player.Session)));
        Assert.True(inventory.RequiresAuthoritativeReload);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BankSplitKeepsItsWholeOrderedPacketBatchUntilActorCommit(bool bankSource)
    {
        var (inventory, _, player, _, source) = await SplitFixture(bankSource);
        var events = InstallBankActor(player.Session);
        inventory.ItemSplit(source.Guid, SplitTarget(!bankSource), 3);
        Assert.Empty(BankMessages(player.Session));
        var created = Assert.IsType<NexusForever.Game.Entity.Item>(inventory.GetItem(205ul));
        await WaitForStoredBankChange(bankSource, 205);
        Assert.Empty(BankMessages(player.Session)); Assert.True(inventory.IsSaveBlocked);
        events.Update(0); await WaitForBankMove(inventory, source, created);
        var messages = BankMessages(player.Session);
        Assert.Equal(2, messages.Length); Assert.IsType<ServerItemAdd>(messages[0]); Assert.IsType<ServerItemStackCountUpdate>(messages[1]);
    }

    private static EventQueue InstallBankActor(IGameSession session)
    {
        var events = new EventQueue();
        ((RecordingDispatchProxy<IGameSession>)(object)session).SetProperty(nameof(IGameSession.Events), events);
        return events;
    }

    [Theory]
    [InlineData("session")]
    [InlineData("account-session")]
    [InlineData("account")]
    [InlineData("realm")]
    [InlineData("character")]
    [InlineData("inventory")]
    [InlineData("logout")]
    [InlineData("invalidated")]
    public async Task CommittedBankBatchCannotPublishToAReplacedOrFinalActor(string change)
    {
        var (inventory, _, player, _, source) = await SplitFixture(false);
        var session = player.Session; var events = InstallBankActor(session);
        inventory.ItemMove(source, SplitTarget(true)); await WaitForStoredBankChange(false, 401);
        var actor = (RecordingDispatchProxy<IPlayer>)(object)player;
        var account = (RecordingDispatchProxy<IAccount>)(object)player.Account;
        if (change == "session") actor.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        if (change == "account-session") account.SetProperty(nameof(IAccount.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        if (change == "account") account.SetProperty(nameof(IAccount.Id), 13u);
        if (change == "realm") actor.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { RealmId = 3, Id = 100 });
        if (change == "character") actor.SetProperty(nameof(IPlayer.CharacterId), 101ul);
        if (change == "inventory") actor.SetProperty(nameof(IPlayer.Inventory), RecordingDispatchProxy<IInventory>.Create(out _));
        if (change == "logout")
        {
            var logout = RecordingDispatchProxy<ILogoutManager>.Create(out var state);
            state.SetProperty(nameof(ILogoutManager.State), LogoutState.Logout); actor.SetProperty(nameof(IPlayer.LogoutManager), logout);
        }
        if (change == "invalidated") inventory.InvalidateRewardDeliveries(); else inventory.Update(0);
        var complete = (Task)typeof(Inventory).GetField("realmBankMoveSave", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(inventory)!;
        await complete.WaitAsync(TimeSpan.FromSeconds(15));
        events.Update(0); events.Update(0);
        Assert.Empty(BankMessages(session)); Assert.True(inventory.RequiresAuthoritativeReload);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.Item);
        Assert.Equal(12u, Assert.Single(read.RealmBankItem).AccountId);
    }

    [Fact]
    public async Task BankPublicationRejectsReentrantMutationWithoutChangingCommittedState()
    {
        var (inventory, _, player, _, source) = await SplitFixture(true);
        InstallBankActor(player.Session); bool attempted = false;
        ((RecordingDispatchProxy<IGameSession>)(object)player.Session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemMove)
            {
                attempted = true; Assert.True(inventory.IsSaveBlocked);
                Assert.NotNull(inventory.CanMoveItem(source, InventoryLocation.Inventory, 6));
                Assert.False(inventory.CanCreateItem(InventoryLocation.Inventory, source.Info, 1));
                Assert.Throws<InvalidOperationException>(() => inventory.ItemDelete(SplitTarget(false)));
                using var rejected = new CharacterContext(Configuration);
                var unrelated = CharacterRow(); unrelated.Id = 999; rejected.Item.Add(unrelated);
                Assert.Throws<InvalidOperationException>(() => inventory.Save(rejected));
                Assert.Throws<InvalidOperationException>(() => rejected.SaveChanges());
            }
            return null;
        });
        inventory.ItemMove(source, SplitTarget(false)); await WaitForBankMove(inventory, source);
        Assert.True(attempted); Assert.False(inventory.RequiresAuthoritativeReload);
        using var read = new CharacterContext(Configuration); Assert.Equal(401ul, Assert.Single(read.Item).Id); Assert.Empty(read.RealmBankItem);
    }

    [Fact]
    public async Task LoadingARealBankInventoryDoesNotStartAWriteOrQueueNotifications()
    {
        await Seed(bank: BankRow(401)); var (manager, player, items) = Access();
        var (inventory, _, _) = SwapInventory(manager, player, items, null, null);
        manager.EnsureLoaded(player);
        Assert.NotNull(inventory.GetItem(401ul)); Assert.Empty(BankMessages(player.Session));
        Assert.Null(typeof(Inventory).GetField("realmBankMoveSave", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(inventory));
        using var read = new CharacterContext(Configuration); Assert.Equal(17u, Assert.Single(read.RealmBankItem).StackCount); Assert.Empty(read.Item);
    }

    [Fact]
    public async Task BankResyncCannotBypassThePendingPublicationBarrier()
    {
        var (inventory, manager, player, _, source) = await SplitFixture(false);
        InstallBankActor(player.Session);
        inventory.ItemMove(source, SplitTarget(true));
        manager.SyncRealmBankToClient(player); Assert.Empty(BankMessages(player.Session));
        await WaitForStoredBankChange(false, 401);
        manager.SyncRealmBankToClient(player); Assert.Empty(BankMessages(player.Session));
        await WaitForBankMove(inventory, source);
        player.Session.Events.Update(0);
        Assert.Single(BankMessages(player.Session).OfType<ServerItemMove>());
        Assert.Single(BankMessages(player.Session).OfType<ServerItemAdd>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BankCreationPublishesItsStackAndNewItemAsOneCommittedUnit(bool failInsert)
    {
        var row = BankRow(401); row.ExpirationTimeLeft = 0;
        await Seed(bank: row); var (manager, player, items) = Access();
        ((RecordingDispatchProxy<IItemManager>)(object)items).SetProperty(nameof(IItemManager.NextItemId), 205ul);
        ((RecordingDispatchProxy<IItemInfo>)(object)items.GetItemInfo(7001)).SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        var (inventory, _, stored) = SwapInventory(manager, player, items, null, row);
        InstallBankActor(player.Session);
        if (failInsert) await SplitTrigger("realm_bank_item", "INSERT");
        inventory.ItemCreate(InventoryLocation.RealmBank, items.GetItemInfo(7001), 8);
        Assert.Empty(BankMessages(player.Session));
        var created = Assert.IsType<NexusForever.Game.Entity.Item>(inventory.GetItem(205ul));
        await WaitForBankMove(inventory, stored, created);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.Item);
        if (failInsert)
        {
            Assert.Equal(17u, Assert.Single(read.RealmBankItem).StackCount);
            Assert.IsType<ServerItemError>(Assert.Single(BankMessages(player.Session))); Assert.True(inventory.RequiresAuthoritativeReload);
        }
        else
        {
            Assert.Equal(25, read.RealmBankItem.Sum(item => (int)item.StackCount));
            Assert.Equal(10u, read.RealmBankItem.Single(item => item.Id == 205).Charges);
            var messages = BankMessages(player.Session); Assert.Equal(2, messages.Length);
            Assert.IsType<ServerItemStackCountUpdate>(messages[0]); Assert.IsType<ServerItemAdd>(messages[1]);
        }
    }

    [Fact]
    public async Task SplitPayloadPreparationFailureRestoresBothBagsBeforeSqlEnrollment()
    {
        var (inventory, _, player, items, source) = await SplitFixture(true);
        InstallBankActor(player.Session);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        int reads = 0;
        tableProxy.SetMethodHandler("get_ItemRuneInstance", _ => ++reads == 1 ? null : throw new InvalidOperationException("Payload preparation failed."));
        typeof(Inventory).GetField("gameTableManager", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, tables);
        items.GetItemInfo(7001).Entry.ItemRuneInstanceId = 1;
        inventory.ItemSplit(source.Guid, SplitTarget(false), 3);
        Assert.Equal(17u, source.StackCount); Assert.Null(inventory.GetItem(205ul)); Assert.Same(source, inventory.GetItem(401ul));
        Assert.False(inventory.IsSaveBlocked); Assert.False(inventory.RequiresAuthoritativeReload);
        Assert.IsType<ServerItemError>(Assert.Single(BankMessages(player.Session)));
        using var read = new CharacterContext(Configuration); Assert.Empty(read.Item); Assert.Equal(17u, Assert.Single(read.RealmBankItem).StackCount);
    }

    [Fact]
    public async Task FinalizedBankOwnerSettlesWithoutPollingItsActorWhenSqlCompletesLater()
    {
        await Seed(CharacterRow());
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var (manager, player, items) = Access(command => command.ExecuteAsync(async attempt =>
        {
            entered.SetResult(); await release.Task; await attempt();
        }));
        var (inventory, source, _) = SwapInventory(manager, player, items, CharacterRow(), null);
        inventory.ItemMove(source, SplitTarget(true)); await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
        inventory.InvalidateRewardDeliveries(); Assert.Empty(BankMessages(player.Session));
        release.SetResult();
        var complete = (Task)typeof(Inventory).GetField("realmBankMoveSave", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(inventory)!;
        await complete.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Empty(BankMessages(player.Session)); Assert.True(inventory.RequiresAuthoritativeReload);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.Item); Assert.Equal(401ul, Assert.Single(read.RealmBankItem).Id);
    }

    [Fact]
    public async Task RetriedSplitPublishesOnceAndNeverAllocatesTheCreatedIdentityAgain()
    {
        await Seed(bank: BankRow(401)); int attempts = 0;
        var (manager, player, items) = Access(command => command.ExecuteAsync(async attempt =>
        {
            ++attempts; await attempt(); // Commit succeeded, but its acknowledgement is lost.
            ++attempts; await attempt();
        }));
        ((RecordingDispatchProxy<IItemManager>)(object)items).SetProperty(nameof(IItemManager.NextItemId), 205ul);
        var (inventory, _, source) = SwapInventory(manager, player, items, null, BankRow(401));
        inventory.ItemSplit(source.Guid, SplitTarget(false), 3);
        var created = Assert.IsType<NexusForever.Game.Entity.Item>(inventory.GetItem(205ul));
        await WaitForBankMove(inventory, source, created);
        player.Session.Events.Update(0); player.Session.Events.Update(0);
        Assert.Equal(2, attempts); Assert.Equal(2, BankMessages(player.Session).Length);
        Assert.Single(((RecordingDispatchProxy<IItemManager>)(object)items).GetInvocations("get_NextItemId"));
        using var read = new CharacterContext(Configuration);
        Assert.Equal(14u, Assert.Single(read.RealmBankItem).StackCount); Assert.Equal(3u, Assert.Single(read.Item).StackCount);
        Assert.False(inventory.RequiresAuthoritativeReload);
    }

    [Fact]
    public async Task PendingWithdrawalCannotPublishASecondCharacterMoveAheadOfItsBankCommit()
    {
        var (inventory, _, player, _, source) = await SplitFixture(true);
        InstallBankActor(player.Session);
        inventory.ItemMove(source, SplitTarget(false));
        inventory.ItemMove(source, InventoryLocation.Inventory, 7);
        Assert.Equal(6u, source.BagIndex); Assert.Empty(BankMessages(player.Session).OfType<ServerItemMove>());
        Assert.Single(BankMessages(player.Session).OfType<ServerItemError>());
        await WaitForBankMove(inventory, source);
        Assert.Single(BankMessages(player.Session).OfType<ServerItemMove>());
        using var read = new CharacterContext(Configuration); Assert.Equal(6u, Assert.Single(read.Item).BagIndex);
    }

    [Fact]
    public async Task NormalSaveLeaseDefersBankMoveAndExpiryWithoutLosingElapsedTime()
    {
        var (inventory, _, player, _, source) = await SplitFixture(true);
        InstallBankActor(player.Session);
        Assert.True(inventory.TryAcquireSaveLease(out var save));
        inventory.ItemMove(source, SplitTarget(false)); inventory.Update(1234);
        Assert.Equal(InventoryLocation.RealmBank, source.Location); Assert.Equal(1234u, source.ExpirationTimeLeft);
        save.Dispose(); inventory.Update(1); await WaitForBankMove(inventory, source);
        Assert.Null(inventory.GetItem(401ul));
        using var read = new CharacterContext(Configuration); Assert.Empty(read.Item); Assert.Empty(read.RealmBankItem);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SimultaneousBankExpirationsCommitOrRollBackTogether(bool failSecond)
    {
        var first = BankRow(401); first.ExpirationTimeLeft = 1;
        var second = BankRow(402); second.BagIndex = 4; second.ExpirationTimeLeft = 1;
        await Seed(bank: first);
        using (var extra = new CharacterContext(Configuration)) { extra.RealmBankItem.Add(second); await extra.SaveChangesAsync(); }
        var (manager, player, items) = Access();
        var (inventory, _, stored) = SwapInventory(manager, player, items, null, first);
        var other = new NexusForever.Game.Entity.Item(BankItemModel(second), items);
        inventory.LoadItem(other, InventoryLocation.RealmBank, 4); InstallBankActor(player.Session);
        if (failSecond)
        {
            using var trigger = new CharacterContext(Configuration);
            await trigger.Database.ExecuteSqlRawAsync(Configuration.Provider == NexusForever.Database.DatabaseProvider.Sqlite
                ? "CREATE TRIGGER reject_expiry BEFORE DELETE ON realm_bank_item WHEN OLD.id=402 BEGIN SELECT RAISE(ABORT, 'expiry failure'); END;"
                : "CREATE TRIGGER reject_expiry BEFORE DELETE ON realm_bank_item FOR EACH ROW BEGIN IF OLD.id=402 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='expiry failure'; END IF; END");
        }
        inventory.Update(1); Assert.Empty(BankMessages(player.Session)); await WaitForBankMove(inventory, stored, other);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(failSecond ? 2 : 0, read.RealmBankItem.Count()); Assert.Empty(read.Item);
        if (failSecond) Assert.IsType<ServerItemError>(Assert.Single(BankMessages(player.Session)));
        else Assert.Equal(2, BankMessages(player.Session).OfType<ServerItemDelete>().Count());
    }

    [Fact]
    public async Task DeferredOpenPreservesAnExistingCommittedItemAndItsLaterCountdown()
    {
        var (inventory, manager, player, _, source) = await SplitFixture(false);
        InstallBankActor(player.Session);
        inventory.ItemMove(source, SplitTarget(true));
        manager.OpenRealmBank(player); manager.OpenRealmBank(player);
        await WaitForBankMove(inventory, source);
        source.ExpirationTimeLeft = 1233;
        player.Session.Events.Update(0);
        Assert.Same(source, inventory.GetItem(401ul)); Assert.Equal(1233u, source.ExpirationTimeLeft);
        Assert.Single(BankMessages(player.Session).OfType<ServerItemMove>());
        Assert.Single(BankMessages(player.Session).OfType<ServerItemAdd>());
        Assert.False(inventory.RequiresAuthoritativeReload);
        using var read = new CharacterContext(Configuration); Assert.Equal(1234u, Assert.Single(read.RealmBankItem).ExpirationTimeLeft);
    }

    [Fact]
    public async Task BankLoadQuarantinesAnExistingIdentityWhoseSqlBaselineChanged()
    {
        var (inventory, manager, player, _, source) = await SplitFixture(true);
        using (var change = new CharacterContext(Configuration)) { change.RealmBankItem.Single().StackCount = 4; await change.SaveChangesAsync(); }
        Assert.Throws<InvalidOperationException>(() => manager.EnsureLoaded(player));
        Assert.True(inventory.RequiresAuthoritativeReload); Assert.Same(source, inventory.GetItem(401ul));
        Assert.Empty(BankMessages(player.Session));
        using var read = new CharacterContext(Configuration); Assert.Equal(4u, Assert.Single(read.RealmBankItem).StackCount);
    }

    private static object[] BankMessages(IGameSession session) => ((RecordingDispatchProxy<IGameSession>)(object)session)
        .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).ToArray();

    private async Task WaitForStoredBankChange(bool bankSource, ulong id)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (true)
        {
            using var context = new CharacterContext(Configuration);
            if (bankSource ? context.Item.AsNoTracking().Any(row => row.Id == id) : context.RealmBankItem.AsNoTracking().Any(row => row.Id == id)) return;
            await Task.Delay(10, timeout.Token);
        }
    }
}
