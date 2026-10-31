using System.Collections.Concurrent;
using System.Reflection;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed class BagReservationTests
{
    [Fact]
    public void EmptyReservationChangesAvailabilityWithoutChangingPhysicalContents()
    {
        var bag = new Bag(InventoryLocation.Inventory, 3);
        Assert.True(bag.TryReserveSlots(new Dictionary<uint, IItem> { [0] = null }, out var token));
        using (token)
        {
            Assert.Equal(3u, bag.SlotsRemaining);
            Assert.Equal(2u, bag.AvailableSlots);
            Assert.True(bag.HasReservedSlots);
            Assert.True(bag.IsSlotReserved(0));
            Assert.Empty(bag);
            Assert.Equal(1u, bag.GetFirstAvailableBagIndex());
        }
        token.Dispose();
        Assert.False(bag.HasReservedSlots);
        Assert.Equal(3u, bag.AvailableSlots);
        Assert.Equal(0u, bag.GetFirstAvailableBagIndex());
    }

    [Theory]
    [InlineData("add")]
    [InlineData("remove")]
    [InlineData("move-source")]
    [InlineData("move-destination")]
    [InlineData("swap-first")]
    [InlineData("swap-second")]
    [InlineData("grow")]
    [InlineData("shrink")]
    [InlineData("restore")]
    public void OverlapRejectionPrecedesEveryBagOrItemMutation(string operation)
    {
        var f = new Fixture(4);
        GameItem first = f.Add(1, 0), second = f.Add(2, 1), fresh = f.Item(3);
        IItem[] before = f.Bag.CreateSnapshot();
        Assert.True(f.Bag.TryReserveSlots(new Dictionary<uint, IItem> { [0] = first, [2] = null }, out var token));
        using (token)
        {
            Action action = operation switch
            {
                "add" => () => f.Bag.AddItem(fresh, 2),
                "remove" => () => f.Bag.RemoveItem(first),
                "move-source" => () => f.Bag.MoveItem(first, 3),
                "move-destination" => () => f.Bag.MoveItem(second, 2),
                "swap-first" => () => f.Bag.SwapItem(first, second),
                "swap-second" => () => f.Bag.SwapItem(second, first),
                "grow" => () => f.Bag.Resize(1),
                "shrink" => () => f.Bag.Resize(-1),
                "restore" => () => f.Bag.RestoreSnapshot([second, first, fresh]),
                _ => throw new ArgumentException(operation)
            };
            Assert.Throws<InvalidOperationException>(action);
            Assert.Equal(before, f.Bag.CreateSnapshot());
            Assert.Equal(4u, f.Bag.Slots);
            Assert.Equal(2u, f.Bag.SlotsRemaining);
            Assert.Equal(1u, f.Bag.AvailableSlots);
            Assert.Equal(0u, first.BagIndex);
            Assert.Equal(1u, second.BagIndex);
            Assert.Equal(InventoryLocation.Inventory, first.Location);
            Assert.Equal(InventoryLocation.Inventory, second.Location);
            Assert.Equal(InventoryLocation.None, first.PreviousLocation);
            Assert.Equal(InventoryLocation.None, second.PreviousLocation);
            Assert.Equal(InventoryLocation.None, fresh.Location);
            Assert.Empty(f.ManagerProxy.GetInvocations("get_NextItemId"));
        }
    }

    [Fact]
    public void NonoverlappingOperationsRemainAvailableWhileReservationIsHeld()
    {
        var f = new Fixture(5);
        var held = f.Add(1, 0);
        var other = f.Add(2, 1);
        Assert.True(f.Bag.TryReserveSlots(new Dictionary<uint, IItem> { [0] = held }, out var token));
        using (token)
        {
            f.Bag.MoveItem(other, 2);
            var added = f.Item(3);
            f.Bag.AddItem(added, 1);
            f.Bag.SwapItem(other, added);
            f.Bag.RemoveItem(added);
            Assert.Same(held, f.Bag.GetItem(0u));
            Assert.Same(other, f.Bag.GetItem(1u));
            Assert.Equal(3u, f.Bag.SlotsRemaining);
            Assert.Equal(3u, f.Bag.AvailableSlots);
        }
    }

    [Theory]
    [InlineData("out-of-range")]
    [InlineData("wrong-object")]
    [InlineData("wrong-empty")]
    [InlineData("wrong-location")]
    [InlineData("wrong-index")]
    [InlineData("empty-set")]
    public void FailedAcquisitionDoesNotLeaveAnyPartialReservation(string kind)
    {
        var f = new Fixture(3);
        var first = f.Add(1, 0);
        Dictionary<uint, IItem> requested = new() { [1] = null };
        switch (kind)
        {
            case "out-of-range": requested[3] = null; break;
            case "wrong-object": requested[0] = f.Item(1); break;
            case "wrong-empty": requested[0] = null; break;
            case "wrong-location": first.Location = InventoryLocation.PlayerBank; requested[0] = first; break;
            case "wrong-index": first.BagIndex = 2; requested[0] = first; break;
            case "empty-set": requested.Clear(); break;
        }
        Assert.False(f.Bag.TryReserveSlots(requested, out var token));
        Assert.Null(token);
        Assert.False(f.Bag.HasReservedSlots);
        Assert.Equal(2u, f.Bag.AvailableSlots);
    }

    [Fact]
    public void CompetingWholePlansCannotReserveEachOthersSlotsPartially()
    {
        var bag = new Bag(InventoryLocation.Inventory, 4);
        Assert.True(bag.TryReserveSlots(new Dictionary<uint, IItem> { [1] = null }, out var first));
        using (first)
        {
            Assert.False(bag.TryReserveSlots(new Dictionary<uint, IItem> { [0] = null, [1] = null, [2] = null }, out var loser));
            Assert.Null(loser);
            Assert.False(bag.IsSlotReserved(0));
            Assert.False(bag.IsSlotReserved(2));
            Assert.True(bag.TryReserveSlots(new Dictionary<uint, IItem> { [2] = null }, out var independent));
            using (independent)
            {
                first.Dispose();
                first.Dispose();
                Assert.False(bag.IsSlotReserved(1));
                Assert.True(bag.IsSlotReserved(2));
                Assert.Equal(3u, bag.AvailableSlots);
            }
        }
        Assert.Equal(4u, bag.AvailableSlots);
    }

    [Fact]
    public async Task ConcurrentAcquisitionHasExactlyOneOwnerUntilExplicitRelease()
    {
        var bag = new Bag(InventoryLocation.Inventory, 1);
        var winners = new ConcurrentBag<Bag.SlotReservation>();
        await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(() =>
        {
            if (bag.TryReserveSlots(new Dictionary<uint, IItem> { [0] = null }, out var token)) winners.Add(token);
        })));
        Assert.Single(winners).Dispose();
        Assert.True(bag.TryReserveSlots(new Dictionary<uint, IItem> { [0] = null }, out var next));
        next.Dispose();
    }

    [Fact]
    public void CallerCannotExpandCapabilityByMutatingTheOriginalDictionary()
    {
        var f = new Fixture(2);
        var requested = new Dictionary<uint, IItem> { [0] = null };
        Assert.True(f.Bag.TryReserveSlots(requested, out var token));
        using (token)
        {
            requested.Clear();
            requested[1] = null;
            var item = f.Item(1);
            item.Location = InventoryLocation.Inventory;
            item.BagIndex = 1;
            Assert.Throws<InvalidOperationException>(() => token.PublishItem(item, 1));
            Assert.True(f.Bag.IsSlotReserved(0));
            Assert.False(f.Bag.IsSlotReserved(1));
        }
    }

    [Fact]
    public void PublicationChangesPhysicalCountOnceAndDoesNotCallItemSetters()
    {
        var f = new Fixture(2);
        Assert.True(f.Bag.TryReserveSlots(new Dictionary<uint, IItem> { [0] = null }, out var token));
        using (token)
        {
            var item = f.Item(1);
            item.Location = InventoryLocation.Inventory;
            item.BagIndex = 0;
            object revisions = typeof(GameItem).GetField("persistenceRevision", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(item);
            token.PublishItem(item, 0);
            Assert.Equal(revisions, typeof(GameItem).GetField("persistenceRevision", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(item));
            Assert.Same(item, f.Bag.GetItem(0u));
            Assert.Equal(1u, f.Bag.SlotsRemaining);
            Assert.Equal(1u, f.Bag.AvailableSlots);
            Assert.Throws<InvalidOperationException>(() => token.PublishItem(item, 0));
            Assert.Throws<InvalidOperationException>(() => f.Bag.RemoveItem(item));
        }
        Assert.Equal(1u, f.Bag.AvailableSlots);
        f.Bag.RemoveItem(f.Bag.GetItem(0u));
        Assert.Equal(2u, f.Bag.SlotsRemaining);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WrongDestinationOrReleasedCapabilityCannotPublish(bool released)
    {
        var f = new Fixture(2);
        Assert.True(f.Bag.TryReserveSlots(new Dictionary<uint, IItem> { [0] = null }, out var token));
        using (token)
        {
            var item = f.Item(1);
            if (released) { token.Dispose(); item.Location = InventoryLocation.Inventory; }
            Assert.Throws<InvalidOperationException>(() => token.PublishItem(item, 0));
            Assert.Empty(f.Bag);
            Assert.Equal(2u, f.Bag.SlotsRemaining);
        }
    }

    [Fact]
    public void EnumeratorUsesAStableArrayWhileSubsequentBagContentsChange()
    {
        var f = new Fixture(2);
        var first = f.Add(1, 0);
        using var enumerator = f.Bag.GetEnumerator();
        f.Bag.RemoveItem(first);
        f.Add(2, 1);
        Assert.True(enumerator.MoveNext());
        Assert.Same(first, enumerator.Current);
        Assert.False(enumerator.MoveNext());
        Assert.Null(f.Bag.GetItem(999ul));
    }

    [Fact]
    public void ForeignObjectCannotRemoveAnActualResidentOrShrinkAwayAnOccupiedSlot()
    {
        var f = new Fixture(2);
        var actual = f.Add(1, 1);
        var foreign = f.Item(1);
        foreign.Location = actual.Location;
        foreign.BagIndex = actual.BagIndex;
        Assert.Throws<ArgumentException>(() => f.Bag.RemoveItem(foreign));
        Assert.Throws<InvalidOperationException>(() => f.Bag.Resize(-1));
        Assert.Same(actual, f.Bag.GetItem(1u));
        Assert.Equal(2u, f.Bag.Slots);
        Assert.Equal(1u, f.Bag.SlotsRemaining);
        f.Bag.RemoveItem(actual);
        f.Bag.Resize(-1);
        Assert.Equal(1u, f.Bag.SlotsRemaining);
    }

    private sealed class Fixture
    {
        public Bag Bag { get; }
        public IItemManager Manager { get; }
        public RecordingDispatchProxy<IItemManager> ManagerProxy { get; }

        public Fixture(uint capacity)
        {
            IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out var ip);
            ip.SetProperty(nameof(IItemInfo.Id), 9011u);
            ip.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 9011, MaxStackCount = 10 });
            Manager = RecordingDispatchProxy<IItemManager>.Create(out var proxy);
            ManagerProxy = proxy;
            proxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
            Bag = new Bag(InventoryLocation.Inventory, capacity);
        }

        public GameItem Item(ulong id) => new(new ItemModel
        {
            Id = id, OwnerId = 42, ItemId = 9011, Location = (ushort)InventoryLocation.Inventory,
            StackCount = 3, Durability = 1
        }, Manager);

        public GameItem Add(ulong id, uint index)
        {
            var item = Item(id);
            Bag.AddItem(item, index);
            return item;
        }
    }
}
