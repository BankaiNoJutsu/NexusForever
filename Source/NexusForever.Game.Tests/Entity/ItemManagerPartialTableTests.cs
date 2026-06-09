using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Entity;

[Collection(LegacyServiceProviderCollection.Name)]
public class ItemManagerPartialTableTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitialiseItemInfo_WithMissingItemTableUsesEmptyCache(bool includeEmptyTable)
    {
        using var scope = new LegacyServiceProviderScope(BuildProvider(
            itemTable: includeEmptyTable ? CreateGameTable<Item2Entry>() : null));
        var itemManager = new ItemManager();

        Invoke(itemManager, "InitialiseItemInfo");

        Assert.Null(itemManager.GetItemInfo(1u));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitialiseEquippedItemSlots_WithMissingItemSlotTableUsesEmptyCache(bool includeEmptyTable)
    {
        using var scope = new LegacyServiceProviderScope(BuildProvider(
            itemSlotTable: includeEmptyTable ? CreateGameTable<ItemSlotEntry>() : null));
        var itemManager = new ItemManager();

        Invoke(itemManager, "InitialiseEquippedItemSlots");

        Assert.Empty(itemManager.GetEquippedBagIndexes(ItemSlot.ArmorChest));
    }

    [Fact]
    public void InitialiseEquippedItemSlots_WithTableBackedSlotCachesEquippedIndexes()
    {
        ItemSlotEntry armorChest = new()
        {
            Id                = (uint)ItemSlot.ArmorChest,
            EquippedSlotFlags = (1u << (int)EquippedItem.Chest) | (1u << (int)EquippedItem.Head)
        };
        ItemSlotEntry armorLegs = new()
        {
            Id                = (uint)ItemSlot.ArmorLegs,
            EquippedSlotFlags = 0u
        };

        using var scope = new LegacyServiceProviderScope(BuildProvider(
            itemSlotTable: CreateGameTable(armorChest, armorLegs)));
        var itemManager = new ItemManager();

        Invoke(itemManager, "InitialiseEquippedItemSlots");

        Assert.Equal(
            [EquippedItem.Chest, EquippedItem.Head],
            itemManager.GetEquippedBagIndexes(ItemSlot.ArmorChest));
        Assert.Empty(itemManager.GetEquippedBagIndexes(ItemSlot.ArmorLegs));
    }

    private static IServiceProvider BuildProvider(
        GameTable<Item2Entry> itemTable = null,
        GameTable<ItemSlotEntry> itemSlotTable = null)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (itemTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Item), itemTable);
        if (itemSlotTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.ItemSlot), itemSlotTable);

        return new ServiceCollection()
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        return table;
    }

    private static void Invoke(ItemManager itemManager, string methodName)
    {
        typeof(ItemManager)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(itemManager, null);
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        backingField.SetValue(instance, value);
    }
}
