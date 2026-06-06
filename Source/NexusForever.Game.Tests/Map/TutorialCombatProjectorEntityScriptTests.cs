using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Tutorial;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Script.Main.Tutorial;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Tests.Map;

public class TutorialCombatProjectorEntityScriptTests
{
    [Fact]
    public void OnActivateSuccess_WhenRecoveryAdvancesCombatQuest_UsesLiveQuestDeltaBeforeTeleport()
    {
        List<string> order = [];
        bool recovered = false;

        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
        {
            ushort questId = (ushort)args[0];

            return (questId, recovered) switch
            {
                (10527, false) => QuestState.Accepted,
                (10518, false) => null,
                (10527, true)  => QuestState.Completed,
                (10518, true)  => QuestState.Accepted,
                _              => null
            };
        });
        questManagerProxy.SetMethodReturn(
            nameof(IQuestManager.GetActiveQuests),
            new[]
            {
                CreateQuest(10527, [
                    CreateObjective(21324),
                    CreateObjective(21323),
                ])
            });
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        sessionProxy.SetProperty(nameof(INetworkSession.Events), new EventQueue());

        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3460u });

        ICinematicManager cinematicManager = RecordingDispatchProxy<ICinematicManager>.Create(out _);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), Faction.Exile);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.CinematicManager), cinematicManager);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);
        playerProxy.SetMethodHandler(nameof(IPlayer.TryRecoverStarterTutorialQuestProgression), _ =>
        {
            order.Add("recover");
            recovered = true;
            return null;
        });
        playerProxy.SetMethodHandler(nameof(IPlayer.TeleportTo), args =>
        {
            Assert.Equal((ushort)3460, (ushort)args[0]);
            Assert.Equal(StarterTutorialDefinition.ExileCombatSimulationTeleportPosition.X, (float)args[1]);
            Assert.Equal(StarterTutorialDefinition.ExileCombatSimulationTeleportPosition.Y, (float)args[2]);
            Assert.Equal(StarterTutorialDefinition.ExileCombatSimulationTeleportPosition.Z, (float)args[3]);
            order.Add("teleport");
            return null;
        });

        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out RecordingDispatchProxy<ICinematicFactory> cinematicFactoryProxy);
        INoviceTutorialCombatProjector cinematic = RecordingDispatchProxy<INoviceTutorialCombatProjector>.Create(out _);
        cinematicFactoryProxy.SetMethodReturn(nameof(ICinematicFactory.CreateCinematic), cinematic);

        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable(new WorldLocation2Entry
        {
            Id = 51739u,
            WorldId = 3460u,
            Position0 = 10f,
            Position1 = 20f,
            Position2 = 30f
        }));

        ISimpleCollidableEntity owner = RecordingDispatchProxy<ISimpleCollidableEntity>.Create(out RecordingDispatchProxy<ISimpleCollidableEntity> ownerProxy);
        ownerProxy.SetProperty(nameof(IWorldEntity.Guid), 900u);
        ownerProxy.SetProperty(nameof(IWorldEntity.CreatureId), 73735u);
        ownerProxy.SetProperty(nameof(IWorldEntity.QuestChecklistIdx), (byte)0);

        var script = new TutorialCombatProjectorEntityScript(
            NullLogger<TutorialCombatProjectorEntityScript>.Instance,
            cinematicFactory,
            gameTableManager);
        script.OnLoad(owner);

        script.OnActivateSuccess(player);

        IEvent pendingEvent = GetQueuedEvent(session.Events);
        pendingEvent.Execute();

        Assert.Equal(["recover", "teleport"], order);
        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.SendInitialPackets)));
    }

    private static IEvent GetQueuedEvent(EventQueue queue)
    {
        FieldInfo field = typeof(EventQueue).GetField("events", BindingFlags.Instance | BindingFlags.NonPublic);
        var pendingEvents = Assert.IsAssignableFrom<IEnumerable>(field?.GetValue(queue));
        object pending = Assert.Single(pendingEvents.Cast<object>());

        PropertyInfo property = pending.GetType().GetProperty("Event", BindingFlags.Instance | BindingFlags.Public);
        return Assert.IsAssignableFrom<IEvent>(property?.GetValue(pending));
    }

    private static IQuest CreateQuest(ushort questId, IReadOnlyCollection<IQuestObjective> objectives)
    {
        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out RecordingDispatchProxy<IQuest> questProxy);
        questProxy.SetProperty(nameof(IQuest.Id), questId);
        questProxy.SetMethodHandler("GetEnumerator", _ => objectives.GetEnumerator());

        return quest;
    }

    private static IQuestObjective CreateObjective(uint objectiveId)
    {
        IQuestObjectiveInfo objectiveInfo = RecordingDispatchProxy<IQuestObjectiveInfo>.Create(out RecordingDispatchProxy<IQuestObjectiveInfo> objectiveInfoProxy);
        objectiveInfoProxy.SetProperty(nameof(IQuestObjectiveInfo.Id), objectiveId);

        IQuestObjective objective = RecordingDispatchProxy<IQuestObjective>.Create(out RecordingDispatchProxy<IQuestObjective> objectiveProxy);
        objectiveProxy.SetProperty(nameof(IQuestObjective.ObjectiveInfo), objectiveInfo);
        objectiveProxy.SetMethodReturn(nameof(IQuestObjective.IsComplete), true);

        return objective;
    }

    private static GameTable<WorldLocation2Entry> CreateGameTable(WorldLocation2Entry entry)
    {
        var table = (GameTable<WorldLocation2Entry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<WorldLocation2Entry>));

        typeof(GameTable<WorldLocation2Entry>)
            .GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new[] { entry });
        typeof(GameTable<WorldLocation2Entry>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new GameTableHeader { MaxId = entry.Id + 1u });

        int[] lookup = Enumerable.Repeat(-1, (int)entry.Id + 1).ToArray();
        lookup[entry.Id] = 0;
        typeof(GameTable<WorldLocation2Entry>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, lookup);

        return table;
    }
}
