using NexusForever.Database.Character;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.TestSupport;

internal sealed class TestInventory : IInventory
{
    private readonly bool createItems;
    private readonly IItemManager itemManager;

    public sealed record ItemCreateCall(InventoryLocation Location, uint ItemId, uint Count, ItemUpdateReason Reason);

    public TestBag InventoryBag { get; }
    public List<ItemCreateCall> CreatedItems { get; } = [];

    public TestInventory(
        uint slotsRemaining,
        bool createItems = false,
        IItemManager itemManager = null)
    {
        this.createItems = createItems;
        this.itemManager = itemManager;
        InventoryBag = new TestBag(slotsRemaining);
    }

    public void Save(CharacterContext context)
    {
    }

    public void Update(double lastTick)
    {
    }

    public bool IsVisualItemSlot(InventoryLocation location, uint bagIndex) => throw new NotSupportedException();
    public bool IsEquippableBagSlot(InventoryLocation location, uint bagIndex) => throw new NotSupportedException();
    public bool IsEquippableBankBagSlot(InventoryLocation location, uint bagIndex) => throw new NotSupportedException();
    public bool IsInventoryFull(InventoryLocation location) => InventoryBag.SlotsRemaining == 0u;
    public uint GetInventorySlotsRemaining(InventoryLocation location) => InventoryBag.SlotsRemaining;
    public bool CanCreateItem(InventoryLocation location, uint itemId, uint count) => count == 0u || InventoryBag.SlotsRemaining > 0u;
    public bool CanCreateItem(InventoryLocation location, IItemInfo info, uint count) => count == 0u || InventoryBag.SlotsRemaining > 0u;
    public bool HasItemCount(uint itemId, uint count) => throw new NotSupportedException();
    public uint GetItemCount(uint itemId) => throw new NotSupportedException();
    public IItem GetItem(ItemLocation itemLocation) => throw new NotSupportedException();
    public IItem GetItem(InventoryLocation location, uint bagIndex) => throw new NotSupportedException();
    public IItem GetItem(ulong guid) => throw new NotSupportedException();
    public IEnumerable<IItemVisual> GetItemVisuals() => throw new NotSupportedException();
    public IItem SpellCreate(Spell4BaseEntry spell4BaseEntry, ItemUpdateReason reason = ItemUpdateReason.NoReason) => throw new NotSupportedException();

    public void ItemCreate(InventoryLocation location, uint itemId, uint count, ItemUpdateReason reason = ItemUpdateReason.NoReason, uint charges = 0)
    {
        CreatedItems.Add(new ItemCreateCall(location, itemId, count, reason));
        if (!createItems)
        {
            DecrementSlots();
            return;
        }

        IItemInfo info = itemManager?.GetItemInfo(itemId)
            ?? throw new InvalidOperationException($"Missing item info for {itemId}.");
        ItemCreate(location, info, count, reason, charges);
    }

    public void ItemCreate(InventoryLocation location, IItemInfo info, uint count, ItemUpdateReason reason = ItemUpdateReason.NoReason, uint charges = 0)
    {
        if (!createItems)
            throw new NotSupportedException();

        var item = new NexusForever.Game.Entity.Item(42ul, info, count, charges, itemManager);
        InventoryBag.AddItem(item, InventoryBag.GetFirstAvailableBagIndex() ?? 0u);
        DecrementSlots();
    }

    public GenericError? CanMoveItem(IItem item, ItemLocation location) => throw new NotSupportedException();
    public GenericError? CanMoveItem(IItem item, InventoryLocation location, uint bagIndex) => throw new NotSupportedException();
    public void ItemMove(IItem item, ItemLocation location) => throw new NotSupportedException();
    public void ItemMove(IItem item, InventoryLocation location, uint bagIndex) => throw new NotSupportedException();
    public void ItemSplit(ulong itemGuid, ItemLocation newItemLocation, uint count) => throw new NotSupportedException();
    public IItem ItemDelete(ItemLocation from, ItemUpdateReason reason = ItemUpdateReason.Loot) => throw new NotSupportedException();
    public IItem ItemDelete(ItemLocation from, uint count, ItemUpdateReason reason = ItemUpdateReason.Loot) => throw new NotSupportedException();
    public void ItemDelete(uint itemId, uint count = 1, ItemUpdateReason reason = ItemUpdateReason.Loot) => throw new NotSupportedException();
    public void ItemRemove(IItem item, ItemUpdateReason reason = ItemUpdateReason.NoReason) => throw new NotSupportedException();
    public void AddItem(IItem item, InventoryLocation location, ItemUpdateReason reason = ItemUpdateReason.NoReason) => throw new NotSupportedException();
    public void LoadItem(IItem item, InventoryLocation location, uint bagIndex) => throw new NotSupportedException();
    public bool ItemUse(IItem item) => throw new NotSupportedException();
    public void ItemMoveToSupplySatchel(IItem item, uint amount) => throw new NotSupportedException();

    public IEnumerator<IBag> GetEnumerator()
    {
        yield return InventoryBag;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void DecrementSlots()
    {
        if (InventoryBag.SlotsRemaining > 0u)
            InventoryBag.SlotsRemaining--;
    }
}

internal sealed class TestBag : IBag
{
    private readonly List<IItem> items = [];

    public InventoryLocation Location => InventoryLocation.Inventory;
    public uint Slots => SlotsRemaining;
    public uint SlotsRemaining { get; set; }

    public TestBag(uint slotsRemaining)
    {
        SlotsRemaining = slotsRemaining;
    }

    public void Save(CharacterContext context)
    {
    }

    public IItem GetItem(ulong guid)
    {
        return items.SingleOrDefault(item => item.Guid == guid);
    }

    public IItem GetItem(uint bagIndex)
    {
        return items.SingleOrDefault(item => item.BagIndex == bagIndex);
    }

    public uint? GetFirstAvailableBagIndex()
    {
        return items.Count < Slots ? (uint)items.Count : null;
    }

    public uint? GetFirstAvailableBagIndex(ItemSlot slot)
    {
        return GetFirstAvailableBagIndex();
    }

    public void AddItem(IItem item, uint bagIndex)
    {
        item.BagIndex = bagIndex;
        items.Add(item);
    }

    public void RemoveItem(IItem item)
    {
        items.Remove(item);
    }

    public void MoveItem(IItem item, uint bagIndex) => throw new NotSupportedException();
    public void SwapItem(IItem item, IItem item2) => throw new NotSupportedException();
    public void Resize(int capacityChange) => throw new NotSupportedException();
    public IItem[] CreateSnapshot() => throw new NotSupportedException();
    public void RestoreSnapshot(IItem[] snapshot) => throw new NotSupportedException();

    public IEnumerator<IItem> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
