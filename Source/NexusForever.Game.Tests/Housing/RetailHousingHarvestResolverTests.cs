using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Housing;

public class RetailHousingHarvestResolverTests
{
    [Fact]
    public void TryResolveYield_SelectsHighestTierWithItem()
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.HousingContributionInfo), CreateGameTable(new HousingContributionInfoEntry
        {
            Id                           = 100u,
            ContributionPointRequirement = 0u,
            Item2IdTier00                = 10u,
            ContributionPointValueTier00 = 1u,
            Item2IdTier02                = 20u,
            ContributionPointValueTier02 = 5u
        }));

        var plug = new HousingPlugItemEntry { HousingContributionInfoId00 = 100u };

        bool resolved = RetailHousingHarvestResolver.TryResolveYield(plug, gameTableManager, uint.MaxValue, out RetailHousingHarvestResolver.HarvestYield yield);

        Assert.True(resolved);
        Assert.Equal(20u, yield.Item2Id);
        Assert.Equal(5u, yield.Quantity);
        Assert.Equal((byte)2, yield.TierIndex);
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
        FieldInfo idField = typeof(T).GetField("Id")!;
        return (uint)idField.GetValue(entry)!;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }
}
