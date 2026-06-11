using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

public class GlobalQuestManagerPartialTableTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Initialise_WithMissingPrimaryTablesUsesEmptyCaches(bool includeEmptyTables)
    {
        var manager = CreateManager(gameTableManager =>
        {
            if (!includeEmptyTables)
                return;

            SetTable(gameTableManager, nameof(GameTableManager.Quest2), CreateGameTable<Quest2Entry>());
            SetTable(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable<Creature2Entry>());
            SetTable(gameTableManager, nameof(GameTableManager.CommunicatorMessages), CreateGameTable<CommunicatorMessagesEntry>());
        });

        manager.Initialise();

        Assert.Null(manager.GetQuestInfo(42));
        Assert.Empty(manager.GetQuestGivers(42));
        Assert.Empty(manager.GetQuestReceivers(42));
        Assert.Null(manager.GetCommunicatorMessage(200u));
        Assert.Empty(manager.GetQuestCommunicatorMessages(42));
        Assert.Empty(manager.GetQuestCommunicatorQuestStateTriggers(42, QuestState.Accepted));
    }

    [Fact]
    public void Initialise_WithTableBackedRowsCachesQuestRelationsAndCommunicators()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Quest2), CreateGameTable(new Quest2Entry
            {
                Id = 42u
            }));
            SetTable(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable(
                new Creature2Entry
                {
                    Id             = 100u,
                    QuestIdGiven   = QuestIds(42u),
                    QuestIdReceive = QuestIds(43u)
                },
                new Creature2Entry
                {
                    Id             = 101u,
                    QuestIdGiven   = QuestIds(42u, 44u),
                    QuestIdReceive = QuestIds()
                }));
            SetTable(gameTableManager, nameof(GameTableManager.CommunicatorMessages), CreateGameTable(
                new CommunicatorMessagesEntry
                {
                    Id               = 200u,
                    QuestIdDelivered = 42u,
                    Quests           = CommunicatorValues(),
                    States           = CommunicatorValues()
                },
                new CommunicatorMessagesEntry
                {
                    Id     = 201u,
                    Quests = CommunicatorValues(42u),
                    States = CommunicatorValues((uint)QuestState.Accepted)
                },
                new CommunicatorMessagesEntry
                {
                    Id               = 202u,
                    QuestIdDelivered = 44u,
                    Quests           = CommunicatorValues(44u),
                    States           = CommunicatorValues((uint)QuestState.Accepted)
                }));
        });

        manager.Initialise();

        Assert.NotNull(manager.GetQuestInfo(42));
        Assert.Equal([100u, 101u], manager.GetQuestGivers(42));
        Assert.Equal([100u], manager.GetQuestReceivers(43));
        Assert.Equal(200u, manager.GetCommunicatorMessage(200u).Id);
        Assert.Equal([200u], manager.GetQuestCommunicatorMessages(42).Select(c => c.Id));
        Assert.Equal([201u], manager.GetQuestCommunicatorQuestStateTriggers(42, QuestState.Accepted).Select(c => c.Id));
        Assert.Empty(manager.GetQuestCommunicatorQuestStateTriggers(44, QuestState.Accepted));
    }

    private static uint[] QuestIds(params uint[] questIds)
    {
        var values = new uint[25];
        Array.Copy(questIds, values, questIds.Length);
        return values;
    }

    private static uint[] CommunicatorValues(params uint[] values)
    {
        var result = new uint[3];
        Array.Copy(values, result, values.Length);
        return result;
    }

    private static GlobalQuestManager CreateManager(Action<GameTableManager> configure)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        configure(gameTableManager);

        return new GlobalQuestManager(gameTableManager: gameTableManager);
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0ul : entries.Max(GetEntryId) + 1ul
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(T[] entries) where T : class, new()
    {
        if (entries.Length == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1ul)).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[(int)GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static ulong GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0];
        return Convert.ToUInt64(idField.GetValue(entry));
    }

    private static void SetTable<T>(GameTableManager gameTableManager, string propertyName, GameTable<T> table) where T : class, new()
    {
        SetAutoProperty(gameTableManager, propertyName, table);
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
}
