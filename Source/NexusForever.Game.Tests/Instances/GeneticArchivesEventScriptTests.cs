using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Raid.GeneticArchives;

namespace NexusForever.Game.Tests.Instances;

public class GeneticArchivesEventScriptTests
{
    [Fact]
    public void OnLoad_SetsEnterPhase()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.Enter, invocation.Arguments[0]);
    }

    [Theory]
    [InlineData(PublicEventPhase.GetToNextLevel, PublicEventObjective.DefeatTheFetidMiscreation)]
    [InlineData(PublicEventPhase.PhagebornConvergence, PublicEventObjective.DefeatThePhagebornConvergence)]
    [InlineData(PublicEventPhase.Ohmna, PublicEventObjective.DefeatDreadphageOhmna)]
    public void OnPublicEventPhase_SingleObjectivePhases_ActivateMappedObjective(PublicEventPhase phase, PublicEventObjective objective)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(objective, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_SecondFloor_ActivatesBothGuardianObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SecondFloor);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatPhagetechGuardianC148);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatPhagetechGuardianC432);
    }

    [Fact]
    public void OnPublicEventPhase_ArchiveDefenseSystem_ActivatesObjectiveAndChallenge()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ArchiveDefenseSystem);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.GetThroughTheArchiveDefenseSystem);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DeathFromAbove);
    }

    [Fact]
    public void OnPublicEventPhase_Minibosses_ActivatesAllFourObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Minibosses);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatTheMalfunctioningPiston);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatTheMalfunctioningBattery);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatTheMalfunctioningGear);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatTheMalfunctioningDynamo);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_BroadcastsMappedOhmnaMessage()
    {
        IPlayer player = CreatePlayer(out IGameSession session);
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        var script = CreateScript((CommunicatorMessage.TheDreadphageOhmna1, message));
        IPublicEvent publicEvent = CreatePublicEvent([player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_ActivatesOpeningBossObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatExperimentX89);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatKuralakTheDefiler);
    }

    [Theory]
    [InlineData(1100300052u, 49198u, -1147.055f, -111.3793f, -520.5323f, -2f, 27899u, "ExperimentX-89EntityScript")]
    [InlineData(1100300053u, 52969u, 169.4765f, -110.4199f, -489.5547f, 2.093871f, 30276u, "KuralakTheDefilerEntityScript")]
    [InlineData(1100300054u, 53031u, 133.965f, -111.45f, -505.34f, 0f, 27557u, "KuralakPillarEntityScript")]
    public void OnPublicEventPhase_Enter_SpawnsReviewedOpeningPlacements(
        uint entityId,
        uint creatureId,
        float x,
        float y,
        float z,
        float rx,
        uint displayInfo,
        string scriptName)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        CreatedNpc npc = Assert.Single(createdNpcs, n => GetEntityModel(n).Id == entityId);
        Vector3 position = new(x, y, z);
        AssertReviewedOpeningModel(npc, entityId, creatureId, position, rx, displayInfo, scriptName);
        AssertGridEntityAddedToMap(mapProxy, npc.Instance, position);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_DoesNotDuplicateReviewedOpeningPlacements()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);
        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        Assert.Equal(3, createdNpcs.Count);
        Assert.Equal(3, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count());
    }

    [Fact]
    public void OnPublicEventPhase_Ohmna_QueuesWipOpenDoorCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);
        var script = CreateScript(CreateCinematicFactory(cinematic));
        IPublicEvent publicEvent = CreatePublicEvent([player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Ohmna);

        RecordingDispatchProxy<ICinematicManager>.Invocation queue = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queue.Arguments[0]);
    }

    [Theory]
    [InlineData(PublicEventObjective.DefeatExperimentX89, PublicEventPhase.GetToNextLevel)]
    [InlineData(PublicEventObjective.DefeatKuralakTheDefiler, PublicEventPhase.GetToNextLevel)]
    [InlineData(PublicEventObjective.DefeatTheFetidMiscreation, PublicEventPhase.SecondFloor)]
    [InlineData(PublicEventObjective.DefeatPhageMaw, PublicEventPhase.ArchiveDefenseSystem)]
    [InlineData(PublicEventObjective.DefeatThePhagetechPrototypes, PublicEventPhase.ArchiveDefenseSystem)]
    [InlineData(PublicEventObjective.GetThroughTheArchiveDefenseSystem, PublicEventPhase.PhagebornConvergence)]
    [InlineData(PublicEventObjective.DefeatThePhagebornConvergence, PublicEventPhase.Minibosses)]
    [InlineData(PublicEventObjective.DefeatTheMalfunctioningGear, PublicEventPhase.Ohmna)]
    [InlineData(PublicEventObjective.DefeatTheMalfunctioningPiston, PublicEventPhase.Ohmna)]
    [InlineData(PublicEventObjective.DefeatTheMalfunctioningDynamo, PublicEventPhase.Ohmna)]
    [InlineData(PublicEventObjective.DefeatTheMalfunctioningBattery, PublicEventPhase.Ohmna)]
    public void OnPublicEventObjectiveStatus_Success_AdvancesBranchChain(PublicEventObjective objective, PublicEventPhase nextPhase)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == nextPhase);
    }

    [Theory]
    [InlineData(PublicEventObjective.DefeatPhagetechGuardianC148, PublicEventObjective.DefeatConstructsInTheCentrifuge)]
    [InlineData(PublicEventObjective.DefeatConstructsInTheCentrifuge, PublicEventObjective.DefeatPhageMaw)]
    [InlineData(PublicEventObjective.DefeatPhagetechGuardianC432, PublicEventObjective.DefeatTheParagonsOfSymbiosis)]
    [InlineData(PublicEventObjective.DefeatTheParagonsOfSymbiosis, PublicEventObjective.DefeatThePhagetechPrototypes)]
    public void OnPublicEventObjectiveStatus_Success_ActivatesBranchSubObjective(PublicEventObjective objective, PublicEventObjective nextObjective)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(nextObjective, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalBoss_FinishesEvent()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatDreadphageOhmna, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatExperimentX89, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Theory]
    [InlineData(PublicEventPhase.Enter, CommunicatorMessage.TheDreadphageOhmna1)]
    [InlineData(PublicEventPhase.Ohmna, CommunicatorMessage.TheDreadphageOhmna8)]
    public void OnCinematicFinish_BranchMessagePhases_SendMappedOhmnaMessage(PublicEventPhase phase, CommunicatorMessage messageId)
    {
        IPlayer player = CreatePlayer(out IGameSession session);
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        var script = CreateScript((messageId, message));
        IPublicEvent publicEvent = CreatePublicEvent([player], out RecordingDispatchProxy<IPublicEvent> eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Phase), (uint)phase);
        script.OnLoad(publicEvent);

        script.OnCinematicFinish(player, 0u);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    private static IPublicEvent CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent([], out eventProxy);
    }

    private static IPublicEvent CreatePublicEvent(IReadOnlyList<IPlayer> players, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(players, out eventProxy, out _, out _);
    }

    private static IPublicEvent CreatePublicEvent(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedNpc> createdNpcs)
    {
        return CreatePublicEvent([], out eventProxy, out mapProxy, out createdNpcs);
    }

    private static IPublicEvent CreatePublicEvent(
        IReadOnlyList<IPlayer> players,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedNpc> createdNpcs)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 1462u });

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<CreatedNpc> npcs = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            CreatedNpc npc = CreateNpc();
            npcs.Add(npc);
            return npc.Instance;
        });

        createdNpcs = npcs;
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

    private static GeneticArchivesEventScript CreateScript(params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript(RecordingDispatchProxy<ICinematicFactory>.Create(out _), messages);
    }

    private static GeneticArchivesEventScript CreateScript(
        ICinematicFactory cinematicFactory,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IReadOnlyDictionary<CommunicatorMessage, ICommunicatorMessage> lookup = messages.ToDictionary(m => m.Id, m => m.Message);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetCommunicatorMessage),
            args => lookup.TryGetValue((CommunicatorMessage)args[0], out ICommunicatorMessage message) ? message : null);
        return new GeneticArchivesEventScript(globalQuestManager, cinematicFactory);
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

    private static CreatedNpc CreateNpc()
    {
        INonPlayerEntity npc = RecordingDispatchProxy<INonPlayerEntity>.Create(out RecordingDispatchProxy<INonPlayerEntity> npcProxy);
        return new CreatedNpc(npc, npcProxy);
    }

    private static EntityModel GetEntityModel(CreatedNpc npc)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            npc.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        return Assert.IsType<EntityModel>(initialise.Arguments[0]);
    }

    private static void AssertReviewedOpeningModel(
        CreatedNpc npc,
        uint entityId,
        uint creatureId,
        Vector3 position,
        float rotationX,
        uint displayInfo,
        string scriptName)
    {
        EntityModel model = GetEntityModel(npc);
        Assert.Equal(entityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(creatureId, model.Creature);
        Assert.Equal((ushort)1462u, model.World);
        Assert.Equal((ushort)0u, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(rotationX, model.Rx);
        Assert.Equal(0f, model.Ry);
        Assert.Equal(0f, model.Rz);
        Assert.Equal(displayInfo, model.DisplayInfo);
        Assert.Equal((ushort)0u, model.OutfitInfo);
        Assert.Equal((ushort)1209u, model.Faction1);
        Assert.Equal((ushort)1209u, model.Faction2);
        Assert.Null(model.EntityEvent);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal(scriptName, entityScript.ScriptName));

        Assert.Collection(model.EntityStat.OrderBy(s => s.Stat),
            health =>
            {
                Assert.Equal((byte)Stat.Health, health.Stat);
                Assert.Equal(1f, health.Value);
            },
            level =>
            {
                Assert.Equal((byte)Stat.Level, level.Stat);
                Assert.Equal(50f, level.Value);
            });
    }

    private static void AssertGridEntityAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        INonPlayerEntity npc,
        Vector3 expectedPosition)
    {
        Assert.Contains(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)), invocation =>
        {
            if (!ReferenceEquals(npc, invocation.Arguments[0]))
                return false;

            IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(invocation.Arguments[1]);
            Assert.Equal(expectedPosition, position.Position);
            Assert.Equal(1462u, position.Info.Entry.Id);
            return true;
        });
    }

    private sealed record CreatedNpc(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);
}
