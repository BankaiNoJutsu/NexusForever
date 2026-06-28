using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

public class CommunicatorMessageTests
{
    private const ushort QuestId = 10510;

    [Fact]
    public void Meets_WhenClientState11AndQuestIsMissing_AllowsStarterCommunicator()
    {
        CommunicatorMessage message = CreateStarterMessageWithState11();
        IPlayer player = CreatePlayer(questState: null);

        Assert.True(message.Meets(player));
    }

    [Theory]
    [InlineData(QuestState.Accepted)]
    [InlineData(QuestState.Achieved)]
    [InlineData(QuestState.Completed)]
    public void Meets_WhenClientState11AndQuestAlreadyKnown_RejectsStarterCommunicator(QuestState questState)
    {
        CommunicatorMessage message = CreateStarterMessageWithState11();
        IPlayer player = CreatePlayer(questState);

        Assert.False(message.Meets(player));
    }

    [Fact]
    public void Meets_WhenWorldZoneMatchesAncestor_AllowsRootZoneCommunicator()
    {
        CommunicatorMessage message = CreateStarterMessageWithState11(
            worldZoneId: 35u,
            gameTableManager: CreateGameTableManager(
                new WorldZoneEntry { Id = 35u },
                new WorldZoneEntry { Id = 590u, ParentZoneId = 35u },
                new WorldZoneEntry { Id = 597u, ParentZoneId = 590u }));
        IPlayer player = CreatePlayer(
            questState: null,
            zone: new WorldZoneEntry { Id = 597u, ParentZoneId = 590u });

        Assert.True(message.Meets(player));
    }

    [Fact]
    public void Meets_WhenWorldZoneDoesNotMatchCurrentOrAncestor_RejectsCommunicator()
    {
        CommunicatorMessage message = CreateStarterMessageWithState11(
            worldZoneId: 35u,
            gameTableManager: CreateGameTableManager(
                new WorldZoneEntry { Id = 40u },
                new WorldZoneEntry { Id = 590u, ParentZoneId = 40u },
                new WorldZoneEntry { Id = 597u, ParentZoneId = 590u }));
        IPlayer player = CreatePlayer(
            questState: null,
            zone: new WorldZoneEntry { Id = 597u, ParentZoneId = 590u });

        Assert.False(message.Meets(player));
    }

    private static CommunicatorMessage CreateStarterMessageWithState11(
        uint worldZoneId = 0u,
        IGameTableManager gameTableManager = null)
    {
        return new CommunicatorMessage(new CommunicatorMessagesEntry
        {
            Id               = 7969u,
            WorldZoneId      = worldZoneId,
            QuestIdDelivered = QuestId,
            Quests           = [QuestId, 0u, 0u],
            States           = [11u, 0u, 0u]
        }, gameTableManager: gameTableManager);
    }

    private static IPlayer CreatePlayer(
        QuestState? questState,
        WorldZoneEntry zone = null)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry
        {
            Id = 1u
        });

        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
        {
            Assert.Equal(QuestId, (ushort)args[0]);
            return questState;
        });

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.Zone), zone);
        playerProxy.SetProperty(nameof(IPlayer.Level), 3u);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        return player;
    }

    private static IGameTableManager CreateGameTableManager(params WorldZoneEntry[] worldZones)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(
            out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.WorldZone), CreateGameTable(worldZones));
        return gameTableManager;
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
        FieldInfo field = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public);
        return (uint)field.GetValue(entry);
    }
}
