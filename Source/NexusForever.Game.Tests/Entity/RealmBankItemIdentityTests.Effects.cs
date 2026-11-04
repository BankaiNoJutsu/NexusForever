using System.Collections.Immutable;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Fact]
    public void ItemUseRejectsStaleReferenceAtAnOccupiedSlot()
    {
        var (manager, player, items) = Access();
        var currentRow = CharacterRow();
        var (inventory, current, _) = SwapInventory(manager, player, items, currentRow, null);
        var staleRow = CharacterRow();
        staleRow.StackCount = 1;
        var stale = new GameItem(staleRow, items)
        {
            Location = current.Location,
            BagIndex = current.BagIndex
        };
        var achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out var hooks);
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.AchievementManager), achievements);

        Assert.False(inventory.ItemUse(stale));

        Assert.Same(current, inventory.GetItem(InventoryLocation.Inventory, currentRow.BagIndex));
        Assert.Equal(17u, current.StackCount);
        Assert.Equal(1u, stale.StackCount);
        Assert.Empty(hooks.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task BankEquipmentPropertiesAndVisualsPublishOnlyAfterTheWholeCommit(bool deposit, bool fail)
    {
        var character = CharacterRow(); character.Location = (ushort)InventoryLocation.Equipped; character.BagIndex = (uint)EquippedItem.Chest;
        await Seed(deposit ? character : null, deposit ? null : BankRow(401));
        var (manager, player, items) = Access();
        var (inventory, _, stored) = SwapInventory(manager, player, items, null, deposit ? null : BankRow(401));
        var equipped = new Bag(InventoryLocation.Equipped, 32, items);
        var bags = (Dictionary<InventoryLocation, IBag>)typeof(Inventory).GetField("bags", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(inventory)!;
        bags.Add(InventoryLocation.Equipped, equipped);
        var source = deposit ? new GameItem(character, items) : stored;
        if (deposit) equipped.AddItem(source, (uint)EquippedItem.Chest);
        var info = (RecordingDispatchProxy<IItemInfo>)(object)items.GetItemInfo(7001);
        info.SetMethodReturn(nameof(IItemInfo.IsEquippable), true); info.SetMethodReturn(nameof(IItemInfo.IsEquippableIntoSlot), true);
        info.SetMethodReturn(nameof(IItemInfo.GetDisplayId), (ushort)555);
        info.SetProperty(nameof(IItemInfo.Properties), ImmutableDictionary<Property, float>.Empty.Add(Property.BaseHealth, 10f));
        info.SetProperty(nameof(IItemInfo.SlotEntry), new ItemSlotEntry { Id = (uint)ItemSlot.ArmorChest });
        info.SetProperty(nameof(IItemInfo.TypeEntry), new Item2TypeEntry { ItemSlotId = (uint)ItemSlot.ArmorChest });
        var order = new List<string>(); var actor = (RecordingDispatchProxy<IPlayer>)(object)player;
        actor.SetMethodHandler(deposit ? nameof(IPlayer.RemoveItemProperty) : nameof(IPlayer.AddItemProperty), args =>
        {
            Assert.Equal(Property.BaseHealth, args[0]);
            if (!deposit) Assert.Equal(10f, args[2]);
            order.Add("property"); return null;
        });
        actor.SetMethodHandler(deposit ? nameof(IPlayer.RemoveVisual) : nameof(IPlayer.AddVisual), _ => { order.Add("visual"); return null; });
        ((RecordingDispatchProxy<IGameSession>)(object)player.Session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemMove) order.Add("move"); return null;
        });
        if (fail) await SplitTrigger(deposit ? "realm_bank_item" : "item", "INSERT");
        inventory.ItemMove(source, deposit ? InventoryLocation.RealmBank : InventoryLocation.Equipped,
            deposit ? 6u : (uint)EquippedItem.Chest);
        Assert.Empty(order);
        info.SetProperty(nameof(IItemInfo.Properties), ImmutableDictionary<Property, float>.Empty);
        await WaitForBankMove(inventory, source);
        if (fail) { Assert.Empty(order); Assert.True(inventory.RequiresAuthoritativeReload); }
        else Assert.Equal(new[] { "property", "visual", "move" }, order);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(fail ? (deposit ? 1 : 0) : (deposit ? 0 : 1), read.Item.Count());
        Assert.Equal(1, read.Item.Count() + read.RealmBankItem.Count());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task BankConsumeGameplayHookRunsOnceAfterCommitAndBarrierRelease(bool fail, bool lostAck)
    {
        await Seed(bank: BankRow(401));
        var (manager, player, items) = Access(lostAck ? command => command.ExecuteAsync(async attempt => { await attempt(); await attempt(); }) : null);
        var (inventory, _, source) = SwapInventory(manager, player, items, null, BankRow(401));
        var achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out var hooks);
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.AchievementManager), achievements);
        hooks.SetMethodHandler(nameof(ICharacterAchievementManager.CheckAchievements), args =>
        {
            Assert.False(inventory.IsSaveBlocked); Assert.Equal(7001u, args[2]);
            using var read = new CharacterContext(Configuration); Assert.Equal(16u, Assert.Single(read.RealmBankItem).StackCount);
            return null;
        });
        if (fail) await SplitTrigger("realm_bank_item", "UPDATE");
        Assert.True(inventory.ItemUse(source));
        Assert.Empty(hooks.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
        await WaitForBankMove(inventory, source); player.Session.Events.Update(0); player.Session.Events.Update(0);
        Assert.Equal(fail ? 0 : 1, hooks.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)).Count);
        Assert.Equal(fail, inventory.RequiresAuthoritativeReload);
        using var final = new CharacterContext(Configuration); Assert.Equal(fail ? 17u : 16u, Assert.Single(final.RealmBankItem).StackCount);
    }

    [Fact]
    public async Task FirstOpenAfterBankWriteKeepsTheExistingItemAndItsDirtyCountdown()
    {
        var (inventory, manager, player, _, source) = await SplitFixture(false);
        inventory.ItemMove(source, SplitTarget(true)); await WaitForBankMove(inventory, source);
        source.ExpirationTimeLeft = 1233;
        manager.OpenRealmBank(player);
        Assert.Same(source, inventory.GetItem(401ul)); Assert.Equal(1233u, source.ExpirationTimeLeft);
        Assert.False(inventory.RequiresAuthoritativeReload);
        using var read = new CharacterContext(Configuration); Assert.Equal(1234u, Assert.Single(read.RealmBankItem).ExpirationTimeLeft);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BankFinalPublicationCallbackCannotLeaveItsReplacedActorWritable(bool gameplay)
    {
        var (inventory, _, player, _, source) = await SplitFixture(true);
        var session = player.Session; var events = session.Events;
        var replacement = RecordingDispatchProxy<IGameSession>.Create(out var newSession);
        var actor = (RecordingDispatchProxy<IPlayer>)(object)player;
        bool changed = false;
        void ReplaceActor() { changed = true; actor.SetProperty(nameof(IPlayer.Session), replacement); }
        var achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out var hooks);
        actor.SetProperty(nameof(IPlayer.AchievementManager), achievements);
        hooks.SetMethodHandler(nameof(ICharacterAchievementManager.CheckAchievements), _ => { if (gameplay) ReplaceActor(); return null; });
        ((RecordingDispatchProxy<IGameSession>)(object)session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (!gameplay && args[0] is ServerItemMove) ReplaceActor();
            return null;
        });
        if (gameplay) Assert.True(inventory.ItemUse(source));
        else inventory.ItemMove(source, SplitTarget(false));
        var complete = (Task)typeof(Inventory).GetField("realmBankMoveSave", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(inventory)!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (!complete.IsCompleted || !changed) { events.Update(0); await Task.Delay(10, timeout.Token); }
        await complete;
        Assert.True(changed); Assert.True(inventory.RequiresAuthoritativeReload); Assert.True(inventory.IsSaveBlocked);
        Assert.False(inventory.TryAcquireSaveLease(out _));
        Assert.Single(BankMessages(session)); Assert.Empty(newSession.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        using var read = new CharacterContext(Configuration);
        if (gameplay) { Assert.Empty(read.Item); Assert.Equal(16u, Assert.Single(read.RealmBankItem).StackCount); }
        else { Assert.Empty(read.RealmBankItem); Assert.Equal(401ul, Assert.Single(read.Item).Id); }
    }
}
