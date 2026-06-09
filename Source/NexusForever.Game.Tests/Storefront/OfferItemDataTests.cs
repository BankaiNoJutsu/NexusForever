using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.World.Model;
using NexusForever.Game.Storefront;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Storefront;

[Collection(LegacyServiceProviderCollection.Name)]
public class OfferItemDataTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructor_WithMissingAccountItemStaticDataThrowsInvalidItemId(bool includeEmptyTable)
    {
        using var scope = new LegacyServiceProviderScope(BuildProvider(includeEmptyTable ? CreateGameTable<AccountItemEntry>() : null));

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new OfferItemData(CreateModel(123)));

        Assert.Equal("ItemId", exception.Message);
    }

    [Fact]
    public void Constructor_WithKnownAccountItemBuildsOfferItemData()
    {
        using var scope = new LegacyServiceProviderScope(BuildProvider(CreateGameTable(new AccountItemEntry
        {
            Id = 123u
        })));

        var itemData = new OfferItemData(CreateModel(123));
        var packet = itemData.Build();

        Assert.Equal(10u, itemData.OfferId);
        Assert.Equal<ushort>(123, itemData.ItemId);
        Assert.Equal(0u, itemData.Type);
        Assert.Equal(2u, itemData.Amount);
        Assert.Equal(123u, itemData.Entry.Id);
        Assert.Equal(0u, packet.Type);
        Assert.Equal<ushort>(123, packet.AccountItemId);
        Assert.Equal(2u, packet.Amount);
        Assert.Equal(123u, packet.Type1AccountItemId);
        Assert.Equal(2u, packet.Type1Amount);
        Assert.Equal(123u, packet.Type2AccountItemId);
    }

    private static StoreOfferItemDataModel CreateModel(ushort itemId)
    {
        return new StoreOfferItemDataModel
        {
            Id     = 10u,
            ItemId = itemId,
            Type   = 0u,
            Amount = 2u
        };
    }

    private static IServiceProvider BuildProvider(GameTable<AccountItemEntry> accountItemTable = null)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (accountItemTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.AccountItem), accountItemTable);

        return new ServiceCollection()
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(instance, value);
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public);
        return (uint)idField.GetValue(entry);
    }
}
