using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Entity;

public class CurrencyManagerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanAfford_WithMissingCurrencyStaticDataThrowsInvalidCurrency(bool includeEmptyTable)
    {
        CurrencyManager manager = CreateManager(includeEmptyTable ? CreateGameTable<CurrencyTypeEntry>() : null);

        Assert.Throws<ArgumentNullException>(() => manager.CanAfford(CurrencyType.Credits, 1ul));

        Assert.Empty(manager);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CurrencyAddAmount_WithMissingCurrencyStaticDataThrowsBeforeMutating(bool includeEmptyTable)
    {
        CurrencyManager manager = CreateManager(includeEmptyTable ? CreateGameTable<CurrencyTypeEntry>() : null);

        Assert.Throws<ArgumentNullException>(() => manager.CurrencyAddAmount(CurrencyType.Credits, 1ul));

        Assert.Empty(manager);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CurrencySubtractAmount_WithMissingCurrencyStaticDataThrowsBeforeMutating(bool includeEmptyTable)
    {
        CurrencyManager manager = CreateManager(includeEmptyTable ? CreateGameTable<CurrencyTypeEntry>() : null);

        Assert.Throws<ArgumentNullException>(() => manager.CurrencySubtractAmount(CurrencyType.Credits, 1ul));

        Assert.Empty(manager);
    }

    [Fact]
    public void CanAfford_WithKnownCurrencyAndZeroAmountReturnsTrue()
    {
        CurrencyManager manager = CreateManager(CreateGameTable(new CurrencyTypeEntry
        {
            Id = (uint)CurrencyType.Credits
        }));

        Assert.True(manager.CanAfford(CurrencyType.Credits, 0ul));
        Assert.Empty(manager);
    }

    private static CurrencyManager CreateManager(GameTable<CurrencyTypeEntry> currencyTypeTable)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        if (currencyTypeTable != null)
            proxy.SetProperty(nameof(IGameTableManager.CurrencyType), currencyTypeTable);

        return new CurrencyManager(gameTableManager);
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
