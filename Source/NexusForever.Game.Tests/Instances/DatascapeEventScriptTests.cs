using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Raid.Datascape;

namespace NexusForever.Game.Tests.Instances;

public class DatascapeEventScriptTests
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
    [InlineData(PublicEventPhase.Enter)]
    [InlineData(PublicEventPhase.HallsOfTheInfiniteMind)]
    public void OnPublicEventPhase_Opening_ActivatesInitialWingObjectives(PublicEventPhase phase)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatTheSystemDaemons);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatOptimizedMemoryProbeED1);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatOptimizedMemoryProbeP2Z);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatOptimizedMemoryProbeTX67);
    }

    [Theory]
    [InlineData(PublicEventPhase.FirstFrostBoulder, PublicEventObjective.DefeatTheFirstFrostBoulderAvalanche)]
    [InlineData(PublicEventPhase.SecondFrostBoulder, PublicEventObjective.DefeatTheSecondFrostBoulderAvalanche)]
    [InlineData(PublicEventPhase.FrostbringerWarlock, PublicEventObjective.DefeatTheFrostbringerWarlock)]
    [InlineData(PublicEventPhase.MaelstromAuthority, PublicEventObjective.DefeatTheMaelstromAuthority)]
    [InlineData(PublicEventPhase.AlphaElementalGuardians, PublicEventObjective.DefeatTheElementalGuardians1)]
    [InlineData(PublicEventPhase.AlphaPersonalityDatacore, PublicEventObjective.RetrieveTheFirstPersonalityDatacore)]
    [InlineData(PublicEventPhase.EarthRoomCanimid, PublicEventObjective.DefeatTheFullyOptimizedCanimid)]
    [InlineData(PublicEventPhase.EarthRoomRock, PublicEventObjective.DefeatTheLogicGuidedRockslide)]
    [InlineData(PublicEventPhase.Gloomclaw, PublicEventObjective.DefeatGloomclaw)]
    [InlineData(PublicEventPhase.LogicWingLogicElemental, PublicEventObjective.DefeatTheAbstractAugmentationAlgorithm)]
    [InlineData(PublicEventPhase.DeltaElementalGuardians, PublicEventObjective.DefeatTheElementalGuardians3)]
    [InlineData(PublicEventPhase.DeltaPersonalityDatacore, PublicEventObjective.RetrieveTheSecondPersonalityDatacore)]
    [InlineData(PublicEventPhase.WarmongerAgratha, PublicEventObjective.DefeatWarmongerAgratha)]
    [InlineData(PublicEventPhase.WarmongerChuna, PublicEventObjective.DefeatWarmongerChuna)]
    [InlineData(PublicEventPhase.WarmongerTalarii, PublicEventObjective.DefeatWarmongerTalarii)]
    [InlineData(PublicEventPhase.GrandWarmongerTargresh, PublicEventObjective.DefeatGrandWarmongerTargresh)]
    [InlineData(PublicEventPhase.BetaElementalGuardians, PublicEventObjective.DefeatTheElementalGuardians2)]
    [InlineData(PublicEventPhase.BetaPersonalityDatacore, PublicEventObjective.RetrieveTheThirdPersonalityDatacore)]
    [InlineData(PublicEventPhase.MemoryCores, PublicEventObjective.PlaceTheDatacoresInTheOculus)]
    [InlineData(PublicEventPhase.Avatus, PublicEventObjective.DefeatAvatus)]
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
    public void OnPublicEventPhase_LogicWingRoom1_ActivatesGeneratorObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.LogicWingRoom1);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.PowerUpAllOfTheEldanPowerGenerators);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.GeneratorCharge1);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.GeneratorCharge2);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.GeneratorCharge3);
    }

    [Fact]
    public void OnPublicEventPhase_LogicWingRoom2_ActivatesGeneratorObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.LogicWingRoom2);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.PowerUpTheEldanPowerGenerators);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.GeneratorCharge4);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.GeneratorCharge5);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.GeneratorCharge6);
    }

    [Fact]
    public void OnPublicEventPhase_LogicWingRoom3_ActivatesGeneratorObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.LogicWingRoom3);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.PowerUpAllOfTheEldanPowerGenerators3);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.GeneratorCharge7);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.GeneratorCharge8);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.GeneratorCharge9);
    }

    [Fact]
    public void OnPublicEventPhase_VolatilityLattice_ActivatesEscapeAndTimer()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.VolatilityLattice);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.EscapeAvatusAttention);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.TimedOut);
    }

    [Theory]
    [InlineData(PublicEventPhase.TheOculus, CommunicatorMessage.Caretaker111)]
    [InlineData(PublicEventPhase.FirstFrostBoulder, CommunicatorMessage.Caretaker112)]
    [InlineData(PublicEventPhase.AlphaElementalGuardians, CommunicatorMessage.Caretaker113)]
    [InlineData(PublicEventPhase.BetaElementalGuardians, CommunicatorMessage.Caretaker114)]
    public void OnPublicEventPhase_BranchMessagePhases_BroadcastMappedCaretakerMessage(PublicEventPhase phase, CommunicatorMessage messageId)
    {
        IPlayer player = CreatePlayer(out IGameSession session);
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        var script = CreateScript((messageId, message));
        IPublicEvent publicEvent = CreatePublicEvent([player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Avatus_QueuesWipSpawnCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);
        var script = CreateScript(CreateCinematicFactory(cinematic));
        IPublicEvent publicEvent = CreatePublicEvent([player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Avatus);

        RecordingDispatchProxy<ICinematicManager>.Invocation queue = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queue.Arguments[0]);
    }

    [Theory]
    [InlineData(PublicEventObjective.DefeatTheSystemDaemons, PublicEventPhase.TheOculus)]
    [InlineData(PublicEventObjective.EscapeTheLimboInfomatrix, PublicEventPhase.FirstFrostBoulder)]
    [InlineData(PublicEventObjective.DefeatTheFirstFrostBoulderAvalanche, PublicEventPhase.SecondFrostBoulder)]
    [InlineData(PublicEventObjective.DefeatTheSecondFrostBoulderAvalanche, PublicEventPhase.FrostbringerWarlock)]
    [InlineData(PublicEventObjective.DefeatTheFrostbringerWarlock, PublicEventPhase.MaelstromAuthority)]
    [InlineData(PublicEventObjective.DefeatTheMaelstromAuthority, PublicEventPhase.AlphaPersonalityDatacore)]
    [InlineData(PublicEventObjective.DefeatTheBioEnhancedBroodmother, PublicEventPhase.EarthRoomCanimid)]
    [InlineData(PublicEventObjective.DefeatTheFullyOptimizedCanimid, PublicEventPhase.EarthRoomRock)]
    [InlineData(PublicEventObjective.DefeatTheLogicGuidedRockslide, PublicEventPhase.Gloomclaw)]
    [InlineData(PublicEventObjective.DefeatGloomclaw, PublicEventPhase.LogicWingRoom1)]
    [InlineData(PublicEventObjective.PowerUpAllOfTheEldanPowerGenerators, PublicEventPhase.LogicWingRoom2)]
    [InlineData(PublicEventObjective.PowerUpTheEldanPowerGenerators, PublicEventPhase.LogicWingRoom3)]
    [InlineData(PublicEventObjective.PowerUpAllOfTheEldanPowerGenerators3, PublicEventPhase.LogicWingLogicElemental)]
    [InlineData(PublicEventObjective.DefeatTheAbstractAugmentationAlgorithm, PublicEventPhase.DeltaElementalGuardians)]
    [InlineData(PublicEventObjective.DefeatTheElementalGuardians3, PublicEventPhase.DeltaPersonalityDatacore)]
    [InlineData(PublicEventObjective.DefeatOptimizedMemoryProbeTX67, PublicEventPhase.VolatilityLattice)]
    [InlineData(PublicEventObjective.EscapeAvatusAttention, PublicEventPhase.WarmongerAgratha)]
    [InlineData(PublicEventObjective.DefeatWarmongerAgratha, PublicEventPhase.WarmongerChuna)]
    [InlineData(PublicEventObjective.DefeatWarmongerChuna, PublicEventPhase.WarmongerTalarii)]
    [InlineData(PublicEventObjective.DefeatWarmongerTalarii, PublicEventPhase.GrandWarmongerTargresh)]
    [InlineData(PublicEventObjective.DefeatGrandWarmongerTargresh, PublicEventPhase.BetaElementalGuardians)]
    [InlineData(PublicEventObjective.DefeatTheElementalGuardians2, PublicEventPhase.BetaPersonalityDatacore)]
    [InlineData(PublicEventObjective.PlaceTheDatacoresInTheOculus, PublicEventPhase.Avatus)]
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
    [InlineData(PublicEventObjective.DefeatOptimizedMemoryProbeP2Z, PublicEventObjective.EscapeTheLimboInfomatrix)]
    [InlineData(PublicEventObjective.DefeatOptimizedMemoryProbeED1, PublicEventObjective.DefeatTheBioEnhancedBroodmother)]
    [InlineData(PublicEventObjective.GeneratorCharge2, PublicEventObjective.DefeatTheHyperAcceleratedSkeledroid)]
    [InlineData(PublicEventObjective.DefeatTheHyperAcceleratedSkeledroid, PublicEventObjective.DefyPerspective)]
    [InlineData(PublicEventObjective.GeneratorCharge8, PublicEventObjective.DefeatTheAugmentedHeraldOfAvatus)]
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
    public void OnPublicEventObjectiveStatus_OneDatacore_DoesNotAdvanceToMemoryCores()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.RetrieveTheFirstPersonalityDatacore, PublicEventStatus.Succeeded));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_AllDatacores_AdvancesToMemoryCores()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.RetrieveTheFirstPersonalityDatacore, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.RetrieveTheSecondPersonalityDatacore, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.RetrieveTheThirdPersonalityDatacore, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.MemoryCores);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalBoss_FinishesEvent()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatAvatus, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatTheSystemDaemons, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    private static IPublicEvent CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent([], out eventProxy);
    }

    private static IPublicEvent CreatePublicEvent(IReadOnlyList<IPlayer> players, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out RecordingDispatchProxy<IMapInstance> mapProxy);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);
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

    private static DatascapeEventScript CreateScript(params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript(RecordingDispatchProxy<ICinematicFactory>.Create(out _), messages);
    }

    private static DatascapeEventScript CreateScript(
        ICinematicFactory cinematicFactory,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IReadOnlyDictionary<CommunicatorMessage, ICommunicatorMessage> lookup = messages.ToDictionary(m => m.Id, m => m.Message);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetCommunicatorMessage),
            args => lookup.TryGetValue((CommunicatorMessage)args[0], out ICommunicatorMessage message) ? message : null);
        return new DatascapeEventScript(globalQuestManager, cinematicFactory);
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
}
