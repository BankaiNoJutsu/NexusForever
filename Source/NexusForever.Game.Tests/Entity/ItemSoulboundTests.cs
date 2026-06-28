using System.Collections.Immutable;
using System.Reflection;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Item;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Static;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Entity;

public class ItemSoulboundTests
{
    private const ulong CharacterId = 1234ul;
    private const ulong ItemGuid    = 0xCAFEul;
    private const uint ItemId       = 91005u;

    public ItemSoulboundTests()
    {
        EnsureInventoryLocationCapacities();
    }

    [Fact]
    public void Build_SoulboundItem_SetsDynamicSoulboundFlag()
    {
        var item = new NexusForever.Game.Entity.Item(CharacterId, CreateItemInfo(ItemBindFlags.None));

        item.MakeSoulbound();

        Assert.Equal(DynamicItemFlags.Soulbound, item.Build().DynamicFlags & DynamicItemFlags.Soulbound);
    }

    [Fact]
    public void ItemMove_BindOnEquipItem_RefreshesSharedItemWithSoulboundFlag()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IPlayer player = CreatePlayer(session);
        IItemManager itemManager = CreateItemManager();
        var inventory = new NexusForever.Game.Entity.Inventory(player, new CharacterModel(), itemManager: itemManager);
        var item = new NexusForever.Game.Entity.Item(CharacterId, CreateItemInfo(ItemBindFlags.BindOnEquip), itemManager: itemManager);
        inventory.LoadItem(item, InventoryLocation.Inventory, 0u);
        sessionProxy.Invocations.Clear();

        inventory.ItemMove(item, InventoryLocation.Equipped, (uint)EquippedItem.System);

        Assert.True(item.Soulbound);
        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> equipCalls =
            sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        Assert.Collection(equipCalls,
            call => Assert.IsType<ServerItemMove>(call.Arguments[0]),
            call =>
            {
                var itemAdd = Assert.IsType<ServerItemAdd>(call.Arguments[0]);
                Assert.Equal(item.Guid, itemAdd.InventoryItem.Item.ItemGuid);
                Assert.Equal(InventoryLocation.Equipped, itemAdd.InventoryItem.Item.LocationData.Location);
                Assert.Equal((uint)EquippedItem.System, itemAdd.InventoryItem.Item.LocationData.BagIndex);
                Assert.Equal(DynamicItemFlags.Soulbound, itemAdd.InventoryItem.Item.DynamicFlags & DynamicItemFlags.Soulbound);
                Assert.Equal(ItemUpdateReason.NoReason, itemAdd.InventoryItem.Reason);
            });

        sessionProxy.Invocations.Clear();

        inventory.ItemMove(item, InventoryLocation.Inventory, 1u);

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> unequipCalls =
            sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        RecordingDispatchProxy<IGameSession>.Invocation unequipCall = Assert.Single(unequipCalls);
        Assert.IsType<ServerItemMove>(unequipCall.Arguments[0]);
        Assert.True(item.Soulbound);
    }

    private static IPlayer CreatePlayer(IGameSession session)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), CharacterId);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.IsLoading), false);
        return player;
    }

    private static IItemManager CreateItemManager()
    {
        IItemManager itemManager = RecordingDispatchProxy<IItemManager>.Create(out RecordingDispatchProxy<IItemManager> itemManagerProxy);
        itemManagerProxy.SetProperty(nameof(IItemManager.NextItemId), ItemGuid);
        itemManagerProxy.SetMethodHandler(nameof(IItemManager.GetEquippedBagIndexes), _ => new[] { EquippedItem.System });
        return itemManager;
    }

    private static IItemInfo CreateItemInfo(ItemBindFlags bindFlags)
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Id), ItemId);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id            = ItemId,
            MaxStackCount = 1u,
            BindFlags     = bindFlags
        });
        itemInfoProxy.SetProperty(nameof(IItemInfo.SlotEntry), new ItemSlotEntry
        {
            Id = (uint)ItemSlot.ArmorSystem
        });
        itemInfoProxy.SetProperty(nameof(IItemInfo.TypeEntry), new Item2TypeEntry
        {
            ItemSlotId = (uint)ItemSlot.ArmorSystem
        });
        itemInfoProxy.SetProperty(nameof(IItemInfo.Properties), ImmutableDictionary<Property, float>.Empty);
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.IsEquippable), true);
        itemInfoProxy.SetMethodHandler(nameof(IItemInfo.IsEquippableIntoSlot), args => (EquippedItem)args[0] == EquippedItem.System);
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), false);
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.IsEquippableBag), false);
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.CanBindOnEquip), (bindFlags & ItemBindFlags.BindOnEquip) != 0);
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.CanBindOnPickup), (bindFlags & ItemBindFlags.BindOnPickup) != 0);
        return itemInfo;
    }

    private static void EnsureInventoryLocationCapacities()
    {
        if (AssetManager.InventoryLocationCapacities != null)
            return;

        var entries = new Dictionary<InventoryLocation, uint>();
        foreach (FieldInfo field in typeof(InventoryLocation).GetFields())
        {
            foreach (InventoryLocationAttribute attribute in field.GetCustomAttributes<InventoryLocationAttribute>())
            {
                InventoryLocation location = (InventoryLocation)field.GetValue(null);
                entries.Add(location, attribute.DefaultCapacity);
            }
        }

        typeof(AssetManager)
            .GetProperty(nameof(AssetManager.InventoryLocationCapacities))
            .SetValue(null, entries.ToImmutableDictionary());
    }
}
