using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Item;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Tests.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Static;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Loot;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Store = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Loot;

public sealed partial class LootBoundPickupTests
{
    [Fact]
    public void ConfirmedPickupIsBoundBeforeItsFirstInventoryPacket()
    {
        using var f = new Fixture(1);
        var loot = f.Loot(1);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.Empty(f.Messages.OfType<ServerItemAdd>());
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        var added = Assert.Single(f.Messages.OfType<ServerItemAdd>());
        Assert.True(added.InventoryItem.Item.DynamicFlags.HasFlag(DynamicItemFlags.Soulbound));
        Assert.True(Assert.Single(f.Actor.Player.Inventory.Single()).Soulbound);
        Assert.Single(f.Messages.OfType<ServerLootBindOnPickup>());
    }

    [Fact]
    public void ConfirmedPickupDoesNotMergeIntoAnExistingUnboundStack()
    {
        using var f = new Fixture(2);
        var old = f.Add(3, false);
        var loot = f.Loot(2);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.False(old.Soulbound);
        Assert.Equal(3u, old.StackCount);
        var accepted = Assert.Single(f.Actor.Player.Inventory.Single(), item => item.Guid != old.Guid);
        Assert.True(accepted.Soulbound);
        Assert.Equal(2u, accepted.StackCount);
    }

    [Fact]
    public void FullPhysicalBagCannotUseUnboundStackCapacityAndCanRetryAfterSpaceIsFreed()
    {
        using var f = new Fixture(1);
        var old = f.Add(3, false);
        var loot = f.Loot(1);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.False(loot.Item.Delivered);
        Assert.False(old.Soulbound);
        Assert.Equal(3u, old.StackCount);
        Assert.Empty(f.Messages.OfType<ServerLootGrant>());
        f.Actor.Player.Inventory.ItemDelete(Fixture.ItemId, 3);
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.True(Assert.Single(f.Actor.Player.Inventory.Single()).Soulbound);
        Assert.Single(f.Messages.OfType<ServerLootGrant>());
    }

    [Fact]
    public void ConfirmedPickupCanMergeIntoAnExistingBoundStack()
    {
        using var f = new Fixture(1);
        var old = f.Add(3, true);
        var loot = f.Loot(2);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.Same(old, Assert.Single(f.Actor.Player.Inventory.Single()));
        Assert.Equal(5u, old.StackCount);
        Assert.True(old.Soulbound);
    }

    [Fact]
    public void ConfirmedPickupWaitsForTheOriginalInventorySaveLease()
    {
        using var f = new Fixture(1);
        var loot = f.Loot(1);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.True(f.Actor.Player.Inventory.TryAcquireSaveLease(out var lease));
        using (lease)
        {
            Assert.Null(Record.Exception(() => Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id))));
            Assert.False(loot.Item.Delivered);
            Assert.Equal(0u, f.Actor.Player.Inventory.GetItemCount(Fixture.ItemId));
        }
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.True(Assert.Single(f.Actor.Player.Inventory.Single()).Soulbound);
    }

    [Fact]
    public async Task BoundPickupSurvivesFailedFullPlayerSaveRetryAndFreshReload()
    {
        using var f = new Fixture(1);
        var loot = f.Loot(2);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        await Assert.ThrowsAsync<IOException>(() => f.Persist(true));
        await f.Persist();
        using var stored = f.Storage.Context();
        var row = Assert.Single(stored.Item);
        Assert.Equal(2u, row.StackCount);
        Assert.True(row.Soulbound);
        using var fresh = f.Reload();
        var item = Assert.Single(fresh.Player.Inventory.Single());
        Assert.True(item.Soulbound);
        Assert.Equal(2u, item.StackCount);
        Assert.False(loot.Instance.GiveLoot(fresh.Player, loot.Item.Id));
    }

    private sealed class Fixture : IDisposable
    {
        internal const uint ItemId = 91003;
        internal readonly Store Storage = new(null);
        internal readonly Actor Actor;
        private readonly IItemManager items;
        private readonly IItemInfo info;
        private readonly uint slots;
        internal IEnumerable<object> Messages => ((RecordingDispatchProxy<IGameSession>)(object)Actor.Player.Session)
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]);

        internal Fixture(uint slots)
        {
            this.slots = slots;
            info = RecordingDispatchProxy<IItemInfo>.Create(out var metadata);
            metadata.SetProperty(nameof(IItemInfo.Id), ItemId);
            metadata.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = ItemId, MaxStackCount = 10, BindFlags = ItemBindFlags.BindOnPickup });
            metadata.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
            metadata.SetMethodReturn(nameof(IItemInfo.CanBindOnPickup), true);
            items = RecordingDispatchProxy<IItemManager>.Create(out var manager);
            manager.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
            ulong next = 1000;
            manager.SetMethodReturnFactory("get_" + nameof(IItemManager.NextItemId), () => next++);
            using var context = Storage.Context();
            Actor = Load(context.Character.AsNoTracking().Single());
            Actor.Set(nameof(Player.Guid), 4242u);
        }
        private Actor Load(CharacterModel model) => new(Storage, actorModel: model, inventoryFactory: (owner, loaded) =>
        {
            var inventory = BankDeferredCurrencyRecoveryTests.Inventory(owner, items, Storage.Source.Tables, null);
            var bag = new Bag(InventoryLocation.Inventory, slots, items);
            foreach (var row in loaded.Item) bag.AddItem(new GameItem(row, items, Storage.Source.Tables), row.BagIndex);
            typeof(Inventory).GetField("bags", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory,
                new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = bag });
            return inventory;
        });
        internal Actor Reload()
        {
            using var context = Storage.Context();
            return Load(context.Character.AsNoTracking().Include(row => row.Item)
                .Include(row => row.Quest).ThenInclude(row => row.QuestObjective).Single());
        }
        internal GameItem Add(uint count, bool bound)
        {
            Actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, ItemId, count);
            var item = Assert.IsType<GameItem>(Assert.Single(Actor.Player.Inventory.Single()));
            if (bound) item.MakeSoulbound();
            Assert.Equal(bound, item.Soulbound);
            return item;
        }
        internal (LootInstance Instance, LootInstanceItem Item) Loot(uint count)
        {
            var instance = new LootInstance(99, new Dictionary<ulong, uint> { [42] = 4242 }, LooterType.Player,
                LootEntityType.Creature, itemManager: items);
            return (instance, instance.AddLootItem(ItemId, LootItemType.StaticItem, count));
        }
        internal Task Persist(bool fail = false) => CharacterAtomicSave.Execute(() =>
        {
            var context = Storage.Context();
            if (fail) context.SavingChanges += (_, _) => throw new IOException("Owned bound-loot flush refusal.");
            return context;
        }, 42, Actor.Player.Save);
        public void Dispose() { Actor.Dispose(); Storage.Dispose(); }
    }
}
