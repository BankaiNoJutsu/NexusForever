using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Entity;

[Collection(LegacyServiceProviderCollection.Name)]
public class ItemInfoDisplaySourceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CacheItemDisplaySourceEntries_WithMissingSourceTableUsesEmptyCache(bool includeEmptyTable)
    {
        var assetManager = new AssetManager();
        using var scope = new LegacyServiceProviderScope(BuildProvider(
            assetManager,
            includeEmptyTable ? CreateGameTable<ItemDisplaySourceEntryEntry>() : null));

        InvokeCacheItemDisplaySourceEntries(assetManager);

        Assert.Null(assetManager.GetItemDisplaySource(55u));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetDisplayId_WithUnavailableDisplaySourceRowsReturnsZero(bool includeEmptyTable)
    {
        var assetManager = new AssetManager();
        using var scope = new LegacyServiceProviderScope(BuildProvider(
            assetManager,
            includeEmptyTable ? CreateGameTable<ItemDisplaySourceEntryEntry>() : null));
        InvokeCacheItemDisplaySourceEntries(assetManager);

        ItemInfo itemInfo = CreateItemInfo(itemSourceId: 55u, item2TypeId: 7u);

        Assert.Equal((ushort)0, itemInfo.GetDisplayId());
    }

    [Fact]
    public void GetDisplayId_WithTableBackedSingleMatchingSourceReturnsDisplayId()
    {
        var assetManager = new AssetManager();
        using var scope = new LegacyServiceProviderScope(BuildProvider(
            assetManager,
            CreateGameTable(
                CreateDisplaySource(id: 1u, itemSourceId: 55u, item2TypeId: 7u, itemDisplayId: 1234u),
                CreateDisplaySource(id: 2u, itemSourceId: 55u, item2TypeId: 8u, itemDisplayId: 5678u))));
        InvokeCacheItemDisplaySourceEntries(assetManager);

        ItemInfo itemInfo = CreateItemInfo(itemSourceId: 55u, item2TypeId: 7u);

        Assert.Equal((ushort)1234, itemInfo.GetDisplayId());
    }

    [Fact]
    public void GetDisplayId_WithTableBackedMultipleSourcesUsesPowerLevelFallback()
    {
        var assetManager = new AssetManager();
        using var scope = new LegacyServiceProviderScope(BuildProvider(
            assetManager,
            CreateGameTable(
                CreateDisplaySource(id: 1u, itemSourceId: 55u, item2TypeId: 7u, itemDisplayId: 1234u, minLevel: 1u, maxLevel: 20u),
                CreateDisplaySource(id: 2u, itemSourceId: 55u, item2TypeId: 7u, itemDisplayId: 5678u, minLevel: 21u, maxLevel: 50u))));
        InvokeCacheItemDisplaySourceEntries(assetManager);

        ItemInfo itemInfo = CreateItemInfo(itemSourceId: 55u, item2TypeId: 7u, powerLevel: 30u);

        Assert.Equal((ushort)5678, itemInfo.GetDisplayId());
    }

    private static IServiceProvider BuildProvider(
        AssetManager assetManager,
        GameTable<ItemDisplaySourceEntryEntry> displaySourceTable)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (displaySourceTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.ItemDisplaySourceEntry), displaySourceTable);

        return new ServiceCollection()
            .AddSingleton(assetManager)
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
    }

    private static ItemInfo CreateItemInfo(
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
}
