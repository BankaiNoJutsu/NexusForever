using System.Numerics;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Shared;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Expedition.FragmentZero;

namespace NexusForever.Game.Tests.Instances;

public class FragmentZeroEventScriptTests
{
    [Fact]
    public void OnLoad_StartsAtBranchEnabledSearchContinuation()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ContinueTheSearchForTheMissingCrew, invocation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_SearchContinuation_ActivatesBranchObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(5u, out RecordingDispatchProxy<IPublicEvent> eventProxy, out _, out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ContinueTheSearchForTheMissingCrew);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i =>
            (PublicEventObjective)i.Arguments[0] == PublicEventObjective.ContinueTheSearchForTheMissingCrew
            && (uint)i.Arguments[1] == 5u);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.LocateTheShipsBlackBox);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.EliminateSkeech);
    }

    [Fact]
    public void OnPublicEventPhase_SearchForMissingCrew_CreatesWipGuessedTurnstileTrigger()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            5u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<RecordingTrigger> createdTriggers);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SearchForMissingCrew);

        RecordingTrigger trigger = Assert.Single(createdTriggers);
        Assert.Equal((4397u, 15f, 4397u), Assert.Single(trigger.TurnstileInitialiseCalls));
        AssertTriggerAddedToMap(mapProxy, trigger, new Vector3(9865.48f, -765.58f, -5896.74f));
    }

    [Theory]
    [InlineData(PublicEventPhase.FollowTheFriendlySkeech, 49225u, 7902u, 9894.729f, -783.358f, -6032.538f)]
    [InlineData(PublicEventPhase.ContinueTheSearchForTheMissingCrew, 48711u, 7903u, 9685.685f, -767.6072f, -6098.117f)]
    public void OnPublicEventPhase_BranchWorldLocationPhases_CreateWipGuessedTrigger(
        PublicEventPhase phase,
        uint worldLocationId,
        uint objectId,
        float x,
        float y,
        float z)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            5u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<RecordingTrigger> createdTriggers);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        RecordingTrigger trigger = Assert.Single(createdTriggers);
        Assert.Equal((worldLocationId, objectId), Assert.Single(trigger.WorldLocationInitialiseCalls));
        AssertTriggerAddedToMap(mapProxy, trigger, new Vector3(x, y, z));
    }

    [Fact]
    public void OnPublicEventPhase_SearchContinuation_QueuesWipFirstWarningCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        ICinematicFactory cinematicFactory = CreateCinematicFactory(cinematic);
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);
        var script = CreateScript(cinematicFactory);
        IPublicEvent publicEvent = CreatePublicEvent(1u, [player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ContinueTheSearchForTheMissingCrew);

        RecordingDispatchProxy<ICinematicManager>.Invocation queue = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queue.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_DefeatPrototypes_ActivatesAllThreePrototypeObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatPrototypes);

        List<object> objectives = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .Select(i => i.Arguments[0])
            .ToList();
        Assert.Contains(PublicEventObjective.DefeatPrototypeAlphansideTheIncubationComplex, objectives);
        Assert.Contains(PublicEventObjective.DefeatPrototypeBeta, objectives);
        Assert.Contains(PublicEventObjective.DefeatPrototypeDelta, objectives);
    }

    [Theory]
    [InlineData(PublicEventPhase.FollowTheFriendlySkeech, CommunicatorMessage.SupervisorLola)]
    [InlineData(PublicEventPhase.SurviveTheSkeechAmbush, CommunicatorMessage.SupervisorLolax)]
    public void OnPublicEventPhase_BranchMessagePhases_BroadcastMappedSupervisorMessage(PublicEventPhase phase, CommunicatorMessage messageId)
    {
        IPlayer player = CreatePlayer(out IGameSession session);
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        var script = CreateScript((messageId, message));
        IPublicEvent publicEvent = CreatePublicEvent(1u, [player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Success_AdvancesBranchPhaseChain()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SearchInsideTheBiomaticsChamberForSyrus, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.DefeatProjectMatron);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalObjective_FinishesEvent()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SpeakWithHugo, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ContinueTheSearchForTheMissingCrew, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Fact]
    public void OnCinematicFinish_SearchContinuation_SendsCaptainHugoMessageToPlayer()
    {
        IPlayer player = CreatePlayer(out IGameSession session);
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        var script = CreateScript((CommunicatorMessage.CaptainHugo1, message));
        IPublicEvent publicEvent = CreatePublicEvent(1u, [player], out RecordingDispatchProxy<IPublicEvent> eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Phase), (uint)PublicEventPhase.ContinueTheSearchForTheMissingCrew);
        script.OnLoad(publicEvent);

        script.OnCinematicFinish(player, 0u);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    private static IPublicEvent CreatePublicEvent(uint playerCount, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(playerCount, [], out eventProxy, out _, out _);
    }

    private static IPublicEvent CreatePublicEvent(uint playerCount, IReadOnlyList<IPlayer> players, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(playerCount, players, out eventProxy, out _, out _);
    }

    private static IPublicEvent CreatePublicEvent(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<RecordingTrigger> createdTriggers)
    {
        return CreatePublicEvent(playerCount, [], out eventProxy, out mapProxy, out createdTriggers);
    }

    private static IPublicEvent CreatePublicEvent(
        uint playerCount,
        IReadOnlyList<IPlayer> players,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<RecordingTrigger> createdTriggers)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3180u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<RecordingTrigger> triggers = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            var trigger = new RecordingTrigger();
            triggers.Add(trigger);
            return trigger;
        });

        createdTriggers = triggers;
        return publicEvent;
    }

    private static IPublicEventObjective CreateObjective(PublicEventObjective objective, PublicEventStatus status)
    {
        IPublicEventObjective eventObjective = RecordingDispatchProxy<IPublicEventObjective>.Create(out RecordingDispatchProxy<IPublicEventObjective> objectiveProxy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry
        {
            Id = (uint)objective
        });
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Status), status);
        return eventObjective;
    }

    private static FragmentZeroEventScript CreateScript(params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript(RecordingDispatchProxy<ICinematicFactory>.Create(out _), messages);
    }

    private static FragmentZeroEventScript CreateScript(
        ICinematicFactory cinematicFactory,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IReadOnlyDictionary<CommunicatorMessage, ICommunicatorMessage> lookup = messages.ToDictionary(m => m.Id, m => m.Message);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetCommunicatorMessage),
            args => lookup.TryGetValue((CommunicatorMessage)args[0], out ICommunicatorMessage message) ? message : null);
        return new FragmentZeroEventScript(globalQuestManager, cinematicFactory);
    }

    private static IPlayer CreatePlayer(out IGameSession session)
    {
        return CreatePlayer(out session, out _);
    }

    private static IPlayer CreatePlayer(out IGameSession session, out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        session = RecordingDispatchProxy<IGameSession>.Create(out _);
        ICinematicManager cinematicManager = RecordingDispatchProxy<ICinematicManager>.Create(out cinematicManagerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.CinematicManager), cinematicManager);
        return player;
    }

    private static ICinematicFactory CreateCinematicFactory(ICinematicBase cinematic)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out RecordingDispatchProxy<ICinematicFactory> cinematicFactoryProxy);
        cinematicFactoryProxy.SetMethodReturn(nameof(ICinematicFactory.CreateCinematic), cinematic);
        return cinematicFactory;
    }

    private static void AssertTriggerAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        RecordingTrigger trigger,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
        Assert.Same(trigger, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(3180u, position.Info.Entry.Id);
    }

    private sealed class RecordingTrigger : IWorldLocationVolumeGridTriggerEntity, ITurnstileGridTriggerEntity
    {
        public List<(uint WorldLocationId, uint ObjectId)> WorldLocationInitialiseCalls { get; } = [];
        public List<(uint TriggerId, float Range, uint ObjectId)> TurnstileInitialiseCalls { get; } = [];

        public WorldLocation2Entry Entry { get; } = new();
        public uint Guid => 0u;
        public IBaseMap Map => null;
        public Vector3 Position => default;
        public IMapInfo PreviousMap => null;
        public bool InWorld => false;
        public float ActivationRange => 0f;
        public float? RangeCheck => null;

        public void Initialise(uint worldLocationId, uint objectId)
        {
            WorldLocationInitialiseCalls.Add((worldLocationId, objectId));
        }

        public void Initialise(uint id, float range, uint objectId)
        {
            TurnstileInitialiseCalls.Add((id, range, objectId));
        }

        public void Initialise(uint id, float range)
        {
        }

        public Task<T> SynchroniseAsync<T>(Func<T> func)
        {
            return Task.FromResult(func());
        }

        public void InvokeScriptCollection<T>(Action<T> action)
        {
        }

        public void RemoveFromMap()
        {
        }

        public void Relocate(Vector3 position)
        {
        }

        public void OnEnqueueAddToMap()
        {
        }

        public void OnAddToMap(IBaseMap map, uint guid, Vector3 vector)
        {
        }

        public void OnEnqueueRemoveFromMap()
        {
        }

        public void OnRemoveFromMap()
        {
        }

        public void OnRelocate(Vector3 vector)
        {
        }

        public bool CanSeeEntity(IGridEntity entity)
        {
            return false;
        }

        public void AddVisible(IGridEntity entity)
        {
        }

        public void RemoveVisible(IGridEntity entity)
        {
        }

        public T GetVisible<T>(uint guid) where T : IGridEntity
        {
            return default;
        }

        public IEnumerable<T> GetVisibleCreature<T>(uint creatureId) where T : IWorldEntity
        {
            return [];
        }

        public IPlayer GetVisiblePlayer(Identity identity)
        {
            return null;
        }

        public void SetInRangeCheck(float range)
        {
        }

        public void CheckEntityInRange(IGridEntity target)
        {
        }

        public IEnumerable<T> GetInRange<T>(uint guid) where T : IGridEntity
        {
            return [];
        }

        public void Update(double lastTick)
        {
        }

        public void Dispose()
        {
        }
    }
}
