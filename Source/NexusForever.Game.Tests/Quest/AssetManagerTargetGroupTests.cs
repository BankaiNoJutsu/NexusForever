using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

public class AssetManagerTargetGroupTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CacheCreatureTargetGroups_WithMissingTargetGroupTableUsesEmptyCache(bool includeEmptyTable)
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            targetGroupTable: includeEmptyTable ? CreateGameTable<TargetGroupEntry>() : null);

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheCreatureTargetGroups");

        Assert.Null(assetManager.GetTargetGroupsForCreatureId(73498u));
    }

    [Fact]
    public void CacheCreatureTargetGroups_WithTableBackedGroupsIndexesCreatureGroupsOnly()
    {
        TargetGroupEntry creatureGroup = CreateTargetGroup(
            id: 11u,
            type: TargetGroupType.CreatureIdGroup,
            73498u,
            73605u);
        TargetGroupEntry ignoredNestedGroup = CreateTargetGroup(
            id: 12u,
            type: TargetGroupType.OtherTargetGroupCreatures,
            11u);

        GameTableManager gameTableManager = CreateGameTableManager(
            targetGroupTable: CreateGameTable(creatureGroup, ignoredNestedGroup));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheCreatureTargetGroups");

        Assert.Equal([11u], assetManager.GetTargetGroupsForCreatureId(73498u));
        Assert.Equal([11u], assetManager.GetTargetGroupsForCreatureId(73605u));
        Assert.Null(assetManager.GetTargetGroupsForCreatureId(1u));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CacheQuestObjectiveTargetGroups_WithUnavailableTablesUsesEmptyTargets(
        bool includeQuestObjectiveTable,
        bool includeTargetGroupTable)
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: includeQuestObjectiveTable
                ? CreateGameTable(CreateQuestObjective(1u, QuestObjectiveType.KillTargetGroup, targetGroupId: 44u))
                : null,
            targetGroupTable: includeTargetGroupTable ? CreateGameTable<TargetGroupEntry>() : null);

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheQuestObjectiveTargetGroups");

        Assert.Empty(assetManager.GetQuestObjectiveTargetIds(1u));
    }

    [Fact]
    public void CacheQuestObjectiveTargetGroups_WithTableBackedNestedGroupsExpandsCreatureTargets()
    {
        QuestObjectiveEntry questObjective = CreateQuestObjective(
            id: 501u,
            type: QuestObjectiveType.KillTargetGroup,
            targetGroupId: 90u);
        TargetGroupEntry parentGroup = CreateTargetGroup(
            id: 90u,
            type: TargetGroupType.OtherTargetGroupCreatures,
            91u,
            92u);
        TargetGroupEntry firstCreatureGroup = CreateTargetGroup(
            id: 91u,
            type: TargetGroupType.CreatureIdGroup,
            73498u,
            73605u);
        TargetGroupEntry secondCreatureGroup = CreateTargetGroup(
            id: 92u,
            type: TargetGroupType.CreatureIdGroup,
            73605u,
            73681u);

        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: CreateGameTable(questObjective),
            targetGroupTable: CreateGameTable(parentGroup, firstCreatureGroup, secondCreatureGroup));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheQuestObjectiveTargetGroups");

        Assert.Equal([73498u, 73605u, 73681u], assetManager.GetQuestObjectiveTargetIds(501u));
    }

    private static GameTableManager CreateGameTableManager(
        GameTable<QuestObjectiveEntry> questObjectiveTable = null,
        GameTable<TargetGroupEntry> targetGroupTable = null)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (questObjectiveTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.QuestObjective), questObjectiveTable);
        if (targetGroupTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.TargetGroup), targetGroupTable);

        return gameTableManager;
    }

    private static AssetManager CreateAssetManager(GameTableManager gameTableManager)
    {
        return new AssetManager(null, gameTableManager);
    }

    private static QuestObjectiveEntry CreateQuestObjective(uint id, QuestObjectiveType type, uint targetGroupId)
    {
        return new QuestObjectiveEntry
        {
            Id   = id,
            Type = (uint)type,
            Data = targetGroupId
        };
    }

    private static TargetGroupEntry CreateTargetGroup(uint id, TargetGroupType type, params uint[] dataEntries)
    {
        uint[] entries = new uint[7];
        for (int i = 0; i < dataEntries.Length && i < entries.Length; i++)
            entries[i] = dataEntries[i];

        return new TargetGroupEntry
        {
            Id          = id,
            Type        = (uint)type,
            DataEntries = entries
        };
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
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry);
    }

    private static void InvokeCache(AssetManager assetManager, string methodName)
    {
        typeof(AssetManager)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
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
