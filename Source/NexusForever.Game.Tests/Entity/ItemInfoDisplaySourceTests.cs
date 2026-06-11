using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Entity;

public class ItemInfoDisplaySourceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CacheItemDisplaySourceEntries_WithMissingSourceTableUsesEmptyCache(bool includeEmptyTable)
    {
        GameTableManager gameTableManager = CreateGameTableManager(includeEmptyTable ? CreateGameTable<ItemDisplaySourceEntryEntry>() : null);
        var assetManager = new AssetManager(null, gameTableManager);

        InvokeCacheItemDisplaySourceEntries(assetManager);

        Assert.Null(assetManager.GetItemDisplaySource(55u));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetDisplayId_WithUnavailableDisplaySourceRowsReturnsZero(bool includeEmptyTable)
    {
        GameTableManager gameTableManager = CreateGameTableManager(includeEmptyTable ? CreateGameTable<ItemDisplaySourceEntryEntry>() : null);
        var assetManager = new AssetManager(null, gameTableManager);
        InvokeCacheItemDisplaySourceEntries(assetManager);

        ItemInfo itemInfo = CreateItemInfo(assetManager, itemSourceId: 55u, item2TypeId: 7u);

        Assert.Equal((ushort)0, itemInfo.GetDisplayId());
    }

    [Fact]
    public void GetDisplayId_WithTableBackedSingleMatchingSourceReturnsDisplayId()
    {
        GameTableManager gameTableManager = CreateGameTableManager(CreateGameTable(
                CreateDisplaySource(id: 1u, itemSourceId: 55u, item2TypeId: 7u, itemDisplayId: 1234u),
                CreateDisplaySource(id: 2u, itemSourceId: 55u, item2TypeId: 8u, itemDisplayId: 5678u)));
        var assetManager = new AssetManager(null, gameTableManager);
        InvokeCacheItemDisplaySourceEntries(assetManager);

        ItemInfo itemInfo = CreateItemInfo(assetManager, itemSourceId: 55u, item2TypeId: 7u);

        Assert.Equal((ushort)1234, itemInfo.GetDisplayId());
    }

    [Fact]
    public void GetDisplayId_WithTableBackedMultipleSourcesUsesPowerLevelFallback()
    {
        GameTableManager gameTableManager = CreateGameTableManager(CreateGameTable(
                CreateDisplaySource(id: 1u, itemSourceId: 55u, item2TypeId: 7u, itemDisplayId: 1234u, minLevel: 1u, maxLevel: 20u),
                CreateDisplaySource(id: 2u, itemSourceId: 55u, item2TypeId: 7u, itemDisplayId: 5678u, minLevel: 21u, maxLevel: 50u)));
        var assetManager = new AssetManager(null, gameTableManager);
        InvokeCacheItemDisplaySourceEntries(assetManager);

        ItemInfo itemInfo = CreateItemInfo(assetManager, itemSourceId: 55u, item2TypeId: 7u, powerLevel: 30u);

        Assert.Equal((ushort)5678, itemInfo.GetDisplayId());
    }

    private static GameTableManager CreateGameTableManager(GameTable<ItemDisplaySourceEntryEntry> displaySourceTable)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (displaySourceTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.ItemDisplaySourceEntry), displaySourceTable);

        return gameTableManager;
    }

    private static ItemInfo CreateItemInfo(
        AssetManager assetManager,
        uint itemSourceId,
        uint item2TypeId,
        uint powerLevel = 0u,
        uint itemDisplayId = 0u)
    {
        var itemInfo = (ItemInfo)RuntimeHelpers.GetUninitializedObject(typeof(ItemInfo));
        SetAutoProperty(itemInfo, nameof(ItemInfo.Entry), new Item2Entry
        {
            Id            = 1u,
            ItemSourceId  = itemSourceId,
            Item2TypeId   = item2TypeId,
            PowerLevel    = powerLevel,
            ItemDisplayId = itemDisplayId
        });
        SetPrivateField(itemInfo, "assetManager", assetManager);
        return itemInfo;
    }

    private static ItemDisplaySourceEntryEntry CreateDisplaySource(
        uint id,
        uint itemSourceId,
        uint item2TypeId,
        uint itemDisplayId,
        uint minLevel = 0u,
        uint maxLevel = 0u)
    {
        return new ItemDisplaySourceEntryEntry
        {
            Id            = id,
            ItemSourceId  = itemSourceId,
            Item2TypeId   = item2TypeId,
            ItemDisplayId = itemDisplayId,
            ItemMinLevel  = minLevel,
            ItemMaxLevel  = maxLevel
        };
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        return table;
    }

    private static void InvokeCacheItemDisplaySourceEntries(AssetManager assetManager)
    {
        typeof(AssetManager)
            .GetMethod("CacheItemDisplaySourceEntries", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(assetManager, null);
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(instance, value);
    }
}
