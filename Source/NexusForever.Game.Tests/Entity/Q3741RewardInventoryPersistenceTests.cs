using System.Collections.Immutable;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microting.EntityFrameworkCore.MySql.Infrastructure;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Entity;

public class Q3741RewardInventoryPersistenceTests
{
    private const ulong CharacterId = 3741ul;

    public Q3741RewardInventoryPersistenceTests()
    {
        EnsureInventoryLocationCapacities();
    }

    [Fact]
    public void Q3741Rewards_ItemCreatePresentsBothRewardsAndInventorySaveStagesPersistentRows()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(
            out RecordingDispatchProxy<IGameSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(
            out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), CharacterId);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.IsLoading), false);

        IItemInfo vendiShot = CreateItemInfo(81917u, maxStackCount: 50u);
        IItemInfo supplySack = CreateItemInfo(29614u, maxStackCount: 4u);
        IItemManager itemManager = CreateItemManager(vendiShot, supplySack);
        var inventory = new Inventory(player, new CharacterModel(), itemManager: itemManager);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);

        inventory.ItemCreate(InventoryLocation.Inventory, 81917u, 3u);
        inventory.ItemCreate(InventoryLocation.Inventory, 29614u, 1u);

        ServerItemAdd[] itemAdds = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
            .OfType<ServerItemAdd>()
            .ToArray();
        Assert.Equal(2, itemAdds.Length);
        Assert.Contains(itemAdds, packet =>
            packet.InventoryItem.Item.Item2Id == 81917u
            && packet.InventoryItem.Item.StackCount == 3u
            && packet.InventoryItem.Item.LocationData.Location == InventoryLocation.Inventory);
        Assert.Contains(itemAdds, packet =>
            packet.InventoryItem.Item.Item2Id == 29614u
            && packet.InventoryItem.Item.StackCount == 1u
            && packet.InventoryItem.Item.LocationData.Location == InventoryLocation.Inventory);

        using CharacterContext context = CreateCharacterContext();
        inventory.Save(context);

        EntityEntry<ItemModel>[] persistentItems = context.ChangeTracker
            .Entries<ItemModel>()
            .Where(entry => entry.State == EntityState.Added)
            .ToArray();
        Assert.Equal(2, persistentItems.Length);
        Assert.Contains(persistentItems, entry =>
            entry.Entity.OwnerId == CharacterId
            && entry.Entity.ItemId == 81917u
            && entry.Entity.StackCount == 3u
            && entry.Entity.Location == (ushort)InventoryLocation.Inventory);
        Assert.Contains(persistentItems, entry =>
            entry.Entity.OwnerId == CharacterId
            && entry.Entity.ItemId == 29614u
            && entry.Entity.StackCount == 1u
            && entry.Entity.Location == (ushort)InventoryLocation.Inventory);
    }

    private static IItemManager CreateItemManager(params IItemInfo[] itemInfos)
    {
        Dictionary<uint, IItemInfo> items = itemInfos.ToDictionary(info => info.Id);
        ulong nextItemId = 3741000ul;

        IItemManager itemManager = RecordingDispatchProxy<IItemManager>.Create(
            out RecordingDispatchProxy<IItemManager> itemManagerProxy);
        itemManagerProxy.SetMethodHandler("get_NextItemId", _ => nextItemId++);
        itemManagerProxy.SetMethodHandler(
            nameof(IItemManager.GetItemInfo),
            args => items.GetValueOrDefault((uint)args[0]));
        return itemManager;
    }

    private static IItemInfo CreateItemInfo(uint itemId, uint maxStackCount)
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(
            out RecordingDispatchProxy<IItemInfo> itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Id), itemId);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id            = itemId,
            MaxStackCount = maxStackCount
        });
        itemInfoProxy.SetProperty(
            nameof(IItemInfo.Properties),
            ImmutableDictionary<Property, float>.Empty);
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.IsEquippable), false);
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.IsEquippableBag), false);
        return itemInfo;
    }

    private static CharacterContext CreateCharacterContext()
    {
        DbContextOptions<CharacterContext> options = new DbContextOptionsBuilder<CharacterContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_character;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        return new CharacterContext(options);
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
