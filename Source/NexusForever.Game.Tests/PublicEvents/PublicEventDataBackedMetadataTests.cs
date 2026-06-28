using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.PublicEvent;
using NexusForever.Script;
using RuntimePublicEvent = NexusForever.Game.PublicEvent.PublicEvent;
using RuntimePublicEventObjective = NexusForever.Game.PublicEvent.PublicEventObjective;
using RuntimePublicEventStats = NexusForever.Game.PublicEvent.PublicEventStats;
using RuntimePublicEventTeam = NexusForever.Game.PublicEvent.PublicEventTeam;
using SharedPublicEventObjectiveStatus = NexusForever.Network.World.Message.Model.Shared.PublicEventObjectiveStatus;
using PublicEventTeamId = NexusForever.Game.Static.PublicEvent.PublicEventTeam;

namespace NexusForever.Game.Tests.PublicEvents;

public class PublicEventDataBackedMetadataTests
{
    [Fact]
    public void Template_MapsLocationsChildEventsAndDepotVirtualItemsFromTypedTables()
    {
        var publicEventEntry = new PublicEventEntry
        {
            Id = 9001u,
            WorldLocation2Id = 1234u,
            PublicEventTypeEnum = PublicEventType.AdventureLevianBay
        };
        var objectiveEntry = new PublicEventObjectiveEntry
        {
            Id = 501u,
            PublicEventId = publicEventEntry.Id,
            PublicEventTeamId = PublicEventTeamId.PublicTeam,
            PublicEventObjectiveFlags = PublicEventObjectiveFlag.InitialObjective,
            PublicEventObjectiveTypeEnum = PublicEventObjectiveType.InteractDepot,
            ObjectId = 7u,
            Count = 10u,
            WorldLocation2Id = 5678u
        };

        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.PublicEvent), CreateGameTable(
            publicEventEntry,
            new PublicEventEntry { Id = 9003u, PublicEventIdParent = publicEventEntry.Id },
            new PublicEventEntry { Id = 9002u, PublicEventIdParent = publicEventEntry.Id },
            new PublicEventEntry { Id = 9004u, PublicEventIdParent = 1u }));
        tableProxy.SetProperty(nameof(IGameTableManager.PublicEventObjective), CreateGameTable(objectiveEntry));
        tableProxy.SetProperty(nameof(IGameTableManager.PublicEventTeam), CreateGameTable(new PublicEventTeamEntry
        {
            Id = PublicEventTeamId.PublicTeam
        }));
        tableProxy.SetProperty(nameof(IGameTableManager.PublicEventCustomStat), CreateGameTable(
            new PublicEventCustomStatEntry
            {
                Id = 10u,
                PublicEventTypeEnum = PublicEventType.AdventureLevianBay,
                StatIndex = 2u
            },
            new PublicEventCustomStatEntry
            {
                Id = 11u,
                PublicEventTypeEnum = PublicEventType.Dungeon,
                StatIndex = 3u
            },
            new PublicEventCustomStatEntry
            {
                Id = 12u,
                PublicEventId = publicEventEntry.Id,
                StatIndex = 4u
            },
            new PublicEventCustomStatEntry
            {
                Id = 13u,
                PublicEventTypeEnum = PublicEventType.AdventureLevianBay,
                StatIndex = 4u
            }));
        tableProxy.SetProperty(nameof(IGameTableManager.PublicEventDepot), CreateGameTable(new PublicEventDepotEntry
        {
            Id = 7u,
            Creature2Id = 19986u,
            Item2Id = 12834u
        }));
        tableProxy.SetProperty(nameof(IGameTableManager.PublicEventVirtualItemDepot), CreateGameTable(new PublicEventVirtualItemDepotEntry
        {
            Id = 1u,
            Creature2Id = 19986u,
            VirtualItemId00 = 100u,
            VirtualItemId01 = 101u
        }));

        var template = new PublicEventTemplate(gameTableManager);
        template.Initialise(publicEventEntry);

        Assert.Equal([1234u], template.Locations);
        Assert.Equal([9002u, 9003u], template.ChildEventIds);
        Assert.Equal([10u, 12u], template.CustomStats.Select(s => s.Id));

        IReadOnlyList<SharedPublicEventObjectiveStatus.VirtualItem> virtualItems = template.GetObjectiveVirtualItems(objectiveEntry);
        Assert.Collection(virtualItems,
            item => AssertVirtualItem(item, 100u, 10u),
            item => AssertVirtualItem(item, 101u, 10u));

        IPublicEventTeam team = RecordingDispatchProxy<IPublicEventTeam>.Create(out _);
        var objective = new RuntimePublicEventObjective();
        objective.Initialise(team, objectiveEntry, virtualItems);

        SharedPublicEventObjectiveStatus status = objective.Build().ObjectiveStatus;
        Assert.Equal(PublicEventObjectiveDataType.VirtualItemDepot, status.DataType);
        Assert.Collection(status.VirtualItems,
            item => AssertVirtualItem(item, 100u, 10u),
            item => AssertVirtualItem(item, 101u, 10u));
        Assert.Equal([5678u], objective.Build().Locations);
    }

    [Fact]
    public void JoinEvent_SendsEventAndObjectiveLocationsWithDepotChoices()
    {
        var objectiveEntry = new PublicEventObjectiveEntry
        {
            Id = 501u,
            Count = 3u,
            PublicEventTeamId = PublicEventTeamId.PublicTeam,
            PublicEventObjectiveFlags = PublicEventObjectiveFlag.InitialObjective,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main,
            WorldLocation2Id = 5678u
        };
        var template = new PublicEventTestSupport.TestPublicEventTemplate(
            objectiveEntry,
            [1234u],
            [
                new SharedPublicEventObjectiveStatus.VirtualItem
                {
                    ItemId = 100u,
                    Count = 3u
                }
            ]);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IPlayer player = TestPlayerBuilder.Create()
            .WithCharacterId(101ul)
            .WithSession(session)
            .Build();
        RuntimePublicEvent publicEvent = CreateJoinablePublicEvent(template);

        publicEvent.JoinEvent(player, PublicEventTeamId.PublicTeam);

        ServerPublicEventStart start = Assert.IsType<ServerPublicEventStart>(
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))).Arguments[0]);
        Assert.Equal([1234u], start.Locations);

        var objective = Assert.Single(start.Objectives);
        Assert.Equal([5678u], objective.Locations);
        Assert.Equal(PublicEventObjectiveDataType.VirtualItemDepot, objective.ObjectiveStatus.DataType);
        SharedPublicEventObjectiveStatus.VirtualItem virtualItem = Assert.Single(objective.ObjectiveStatus.VirtualItems);
        AssertVirtualItem(virtualItem, 100u, 3u);
    }

    private static RuntimePublicEvent CreateJoinablePublicEvent(IPublicEventTemplate template)
    {
        var publicEvent = new RuntimePublicEvent(
            NullLogger<RuntimePublicEvent>.Instance,
            RecordingDispatchProxy<IScriptManager>.Create(out _),
            new PublicEventTestSupport.DelegateFactory<IPublicEventTeam>(() => new RuntimePublicEventTeam(
                NullLogger<RuntimePublicEventTeam>.Instance,
                new PublicEventTestSupport.DelegateFactory<IPublicEventObjective>(() => new RuntimePublicEventObjective()),
                new PublicEventTestSupport.DelegateFactory<IPublicEventTeamMember>(() =>
                    RecordingDispatchProxy<IPublicEventTeamMember>.Create(out _)),
                new PublicEventTestSupport.ThrowingFactory<IPublicEventVote>(),
                new RuntimePublicEventStats())),
            RecordingDispatchProxy<IPublicEventEntityFactory>.Create(out _));

        IPublicEventManager manager = RecordingDispatchProxy<IPublicEventManager>.Create(out _);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        publicEvent.Initialise(manager, template, map);
        return publicEvent;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));

        typeof(GameTable<T>)
            .GetProperty(nameof(GameTable<T>.Entries), BindingFlags.Instance | BindingFlags.Public)
            ?.SetValue(table, entries);

        ulong maxId = entries
            .Select(entry => Convert.ToUInt64(typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry)))
            .DefaultIfEmpty(0ul)
            .Max() + 1ul;

        var lookup = Enumerable.Repeat(-1, (int)maxId).ToArray();
        FieldInfo idField = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0];
        for (int i = 0; i < entries.Length; i++)
        {
            ulong id = Convert.ToUInt64(idField.GetValue(entries[i]));
            lookup[id] = i;
        }

        typeof(GameTable<T>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, lookup);

        typeof(GameTable<T>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new GameTableHeader { MaxId = maxId });

        return table;
    }

    private static void AssertVirtualItem(SharedPublicEventObjectiveStatus.VirtualItem item, uint expectedItemId, uint expectedCount)
    {
        Assert.Equal(expectedItemId, item.ItemId);
        Assert.Equal(expectedCount, item.Count);
    }
}
