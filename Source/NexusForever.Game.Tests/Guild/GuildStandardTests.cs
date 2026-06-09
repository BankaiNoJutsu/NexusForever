using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Guild;

[Collection(LegacyServiceProviderCollection.Name)]
public class GuildStandardTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructor_WithMissingGuildStandardPartStaticDataThrowsInvalidPart(bool includeEmptyTable)
    {
        using var scope = new LegacyServiceProviderScope(BuildProvider(
            includeEmptyTable ? CreateGameTable<GuildStandardPartEntry>() : null,
            null));

        Assert.Throws<ArgumentException>(() => new GuildStandard.GuildStandardPart(
            GuildStandardPartType.Background,
            10,
            0,
            0,
            0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_WithMissingDyeColorRampStaticDataRejectsNonZeroDye(bool includeEmptyTable)
    {
        using var scope = new LegacyServiceProviderScope(BuildProvider(
            CreateGameTable(new GuildStandardPartEntry
            {
                Id = 10u,
                GuildStandardPartTypeEnum = (uint)GuildStandardPartType.Background
            }),
            includeEmptyTable ? CreateGameTable<DyeColorRampEntry>() : null));

        var part = new GuildStandard.GuildStandardPart(
            GuildStandardPartType.Background,
            10,
            1,
            0,
            0);

        Assert.False(part.Validate());
    }

    [Fact]
    public void Validate_WithKnownStaticDataAcceptsMatchingStandardPartAndDye()
    {
        using var scope = new LegacyServiceProviderScope(BuildProvider(
            CreateGameTable(new GuildStandardPartEntry
            {
                Id = 10u,
                GuildStandardPartTypeEnum = (uint)GuildStandardPartType.Background
            }),
            CreateGameTable(new DyeColorRampEntry
            {
                Id = 1u
            })));

        var part = new GuildStandard.GuildStandardPart(
            GuildStandardPartType.Background,
            10,
            1,
            0,
            0);

        Assert.True(part.Validate());
        Assert.Equal(10u, part.GuildStandardPartEntry.Id);
        Assert.Equal<ushort>(1, part.DyeColorRampId1);
    }

    private static IServiceProvider BuildProvider(
        GameTable<GuildStandardPartEntry> guildStandardPartTable,
        GameTable<DyeColorRampEntry> dyeColorRampTable)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (guildStandardPartTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.GuildStandardPart), guildStandardPartTable);
        if (dyeColorRampTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.DyeColorRamp), dyeColorRampTable);

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
