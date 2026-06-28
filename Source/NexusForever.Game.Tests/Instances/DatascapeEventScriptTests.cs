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
using NexusForever.Script.Instance.Raid.Datascape;

namespace NexusForever.Game.Tests.Instances;

public class DatascapeEventScriptTests
{
    private const uint OpeningPhase = 0u;
    private const uint FirstFrostBoulderPhase = 3u;
    private const uint SecondFrostBoulderPhase = 4u;
    private const uint FrostbringerWarlockPhase = 5u;

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
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(out RecordingDispatchProxy<IPublicEvent> eventProxy, out _, out _);
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

    [Fact]
    public void OnPublicEventPhase_Enter_SpawnsReviewedOpeningPlacements()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatTheSystemDaemons);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatOptimizedMemoryProbeED1);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatOptimizedMemoryProbeP2Z);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatOptimizedMemoryProbeTX67);
        Assert.Collection(createdNpcs,
            npc => AssertDatascapeSpawnModel(npc, 1100300059u, 61819u, 1301, OpeningPhase, new Vector3(618.551f, -215.61023f, 75f), Vector3.Zero, 30425u, "OptimizedMemoryProbeED1EntityScript", 15900000f),
            npc => AssertDatascapeSpawnModel(npc, 1100300060u, 61818u, 1301, OpeningPhase, new Vector3(865f, -215.6028f, -173.5f), new Vector3(1.55485f, 0f, 0f), 30425u, "OptimizedMemoryProbeTX-67EntityScript", 15900000f),
            npc => AssertDatascapeSpawnModel(npc, 1100300061u, 31667u, 1301, OpeningPhase, new Vector3(618f, -216.15657f, -421f), new Vector3(3.10206f, 0f, 0f), 30425u, "OptimizedMemoryProbeP2ZEntityScript", 15900000f),
            npc => AssertDatascapeSpawnModel(npc, 1100300062u, 30495u, 1349, OpeningPhase, new Vector3(132.5f, -226.5f, -67f), Vector3.Zero, 33172u, "NullSystemDaemonEntityScript", 14400000f),
            npc => AssertDatascapeSpawnModel(npc, 1100300063u, 30496u, 1349, OpeningPhase, new Vector3(132.5f, -226.5f, -263f), new Vector3(3.08953f, 0f, 0f), 33172u, "BinarySystemDaemonEntityScript", 14400000f));
        AssertGridEntitiesAddedToMap(mapProxy, createdNpcs);
    }

    [Fact]
    public void OnPublicEventPhase_OpeningPhases_DoNotDuplicateReviewedSpawns()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);
        script.OnPublicEventPhase((uint)PublicEventPhase.HallsOfTheInfiniteMind);

        Assert.Equal(5, createdNpcs.Count);
        Assert.Equal(5, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count());
    }

    [Fact]
    public void OnPublicEventPhase_FirstFrostBoulder_SpawnsReviewedPlacement()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FirstFrostBoulder);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatTheFirstFrostBoulderAvalanche);
        CreatedNpc npc = Assert.Single(createdNpcs);
        AssertDatascapeSpawnModel(
            npc,
            1100300064u,
            31677u,
            4475,
            FirstFrostBoulderPhase,
            new Vector3(3356.48f, -765.57f, -3246.29f),
            new Vector3(0.17427f, 0f, 0f),
            27434u,
            "FrostBoulderAvalancheFirstEntityScript",
            14500000f);
        AssertGridEntitiesAddedToMap(mapProxy, createdNpcs);
    }

    [Fact]
    public void OnPublicEventPhase_FirstFrostBoulder_DoesNotDuplicateReviewedPlacement()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FirstFrostBoulder);
        script.OnPublicEventPhase((uint)PublicEventPhase.FirstFrostBoulder);

        Assert.Single(createdNpcs);
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Fact]
    public void OnPublicEventPhase_SecondFrostBoulder_SpawnsReviewedPlacement()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SecondFrostBoulder);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatTheSecondFrostBoulderAvalanche);
        CreatedNpc npc = Assert.Single(createdNpcs);
        AssertDatascapeSpawnModel(
            npc,
            1100300065u,
            56200u,
            4476,
            SecondFrostBoulderPhase,
            new Vector3(3635.073f, -745.20f, -3373.29f),
            new Vector3(1.11529f, 0f, 0f),
            27434u,
            "FrostBoulderAvalancheSecondEntityScript",
            14500000f);
        AssertGridEntitiesAddedToMap(mapProxy, createdNpcs);
    }

    [Fact]
    public void OnPublicEventPhase_FrostbringerWarlock_SpawnsReviewedPlacement()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FrostbringerWarlock);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatTheFrostbringerWarlock);
        CreatedNpc npc = Assert.Single(createdNpcs);
        AssertDatascapeSpawnModel(
            npc,
            1100300066u,
            31674u,
            4476,
            FrostbringerWarlockPhase,
            new Vector3(3328.50f, -696.86f, -3639.44f),
            new Vector3(-1.42842f, 0f, 0f),
            23490u,
            "FrostbringerWarlockEntityScript",
            15900000f);
        AssertGridEntitiesAddedToMap(mapProxy, createdNpcs);
    }

    [Theory]
    [InlineData(PublicEventPhase.SecondFrostBoulder)]
    [InlineData(PublicEventPhase.FrostbringerWarlock)]
    public void OnPublicEventPhase_ReviewedFrostWingPlacements_DoNotDuplicate(PublicEventPhase phase)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);
        script.OnPublicEventPhase((uint)phase);

        Assert.Single(createdNpcs);
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Theory]
    [MemberData(nameof(DatascapePhaseSpawnCases))]
    public void OnPublicEventPhase_ReviewedLaterPlacement_SpawnsAndActivatesObjective(
        PublicEventPhase phase,
        PublicEventObjective objective,
        ExpectedDatascapeSpawn expectedSpawn)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        AssertObjectiveActivated(eventProxy, objective);
        CreatedNpc npc = Assert.Single(createdNpcs);
        AssertDatascapeSpawnModel(npc, expectedSpawn);
        AssertGridEntitiesAddedToMap(mapProxy, createdNpcs);
    }

    [Theory]
    [MemberData(nameof(DatascapePhaseSpawnCases))]
    public void OnPublicEventPhase_ReviewedLaterPlacement_DoesNotDuplicate(
        PublicEventPhase phase,
        PublicEventObjective ignoredObjective,
        ExpectedDatascapeSpawn ignoredSpawn)
    {
        _ = ignoredObjective;
        _ = ignoredSpawn;

        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);
        script.OnPublicEventPhase((uint)phase);

        Assert.Single(createdNpcs);
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Theory]
    [MemberData(nameof(DatascapeObjectiveStatusSpawnCases))]
    public void OnPublicEventObjectiveStatus_ReviewedSubObjectivePlacement_SpawnsAndActivatesObjective(
        PublicEventObjective sourceObjective,
        PublicEventObjective activatedObjective,
        ExpectedDatascapeSpawn expectedSpawn)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(sourceObjective, PublicEventStatus.Succeeded));

        AssertObjectiveActivated(eventProxy, activatedObjective);
        CreatedNpc npc = Assert.Single(createdNpcs);
        AssertDatascapeSpawnModel(npc, expectedSpawn);
        AssertGridEntitiesAddedToMap(mapProxy, createdNpcs);
    }

    [Theory]
    [MemberData(nameof(DatascapeObjectiveStatusSpawnCases))]
    public void OnPublicEventObjectiveStatus_ReviewedSubObjectivePlacement_DoesNotDuplicate(
        PublicEventObjective sourceObjective,
        PublicEventObjective ignoredObjective,
        ExpectedDatascapeSpawn ignoredSpawn)
    {
        _ = ignoredObjective;
        _ = ignoredSpawn;

        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(sourceObjective, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(sourceObjective, PublicEventStatus.Succeeded));

        Assert.Single(createdNpcs);
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
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
        return CreatePublicEvent(players, out eventProxy, out _, out _);
    }

    private static IPublicEvent CreatePublicEventWithReviewedSpawns(
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
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 1333u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);

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

    private static void AssertObjectiveActivated(RecordingDispatchProxy<IPublicEvent> eventProxy, PublicEventObjective objective)
    {
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
    }

    private static CreatedNpc CreateNpc()
    {
        INonPlayerEntity npc = RecordingDispatchProxy<INonPlayerEntity>.Create(out RecordingDispatchProxy<INonPlayerEntity> npcProxy);
        return new CreatedNpc(npc, npcProxy);
    }

    private static void AssertDatascapeSpawnModel(
        CreatedNpc npc,
        uint entityId,
        uint creatureId,
        ushort areaId,
        uint phase,
        Vector3 position,
        Vector3 rotation,
        uint displayInfo,
        string scriptName,
        float health)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            npc.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(entityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(creatureId, model.Creature);
        Assert.Equal((ushort)1333u, model.World);
        Assert.Equal(areaId, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(rotation.X, model.Rx);
        Assert.Equal(rotation.Y, model.Ry);
        Assert.Equal(rotation.Z, model.Rz);
        Assert.Equal(displayInfo, model.DisplayInfo);
        Assert.Equal((ushort)1171u, model.Faction1);
        Assert.Equal((ushort)1171u, model.Faction2);
        Assert.Equal(157u, model.EntityEvent.EventId);
        Assert.Equal(phase, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal(scriptName, entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Health && stat.Value == health);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 50f);
    }

    private static void AssertDatascapeSpawnModel(CreatedNpc npc, ExpectedDatascapeSpawn spawn)
    {
        AssertDatascapeSpawnModel(
            npc,
            spawn.EntityId,
            spawn.CreatureId,
            spawn.AreaId,
            spawn.Phase,
            spawn.Position,
            spawn.Rotation,
            spawn.DisplayInfo,
            spawn.ScriptName,
            spawn.Health);
    }

    private static void AssertGridEntitiesAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        IReadOnlyList<CreatedNpc> createdNpcs)
    {
        List<RecordingDispatchProxy<IMapInstance>.Invocation> enqueueAdds = mapProxy
            .GetInvocations(nameof(IMap.EnqueueAdd))
            .ToList();
        Assert.Equal(createdNpcs.Count, enqueueAdds.Count);

        for (int i = 0; i < createdNpcs.Count; i++)
        {
            Assert.Same(createdNpcs[i].Instance, enqueueAdds[i].Arguments[0]);

            IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdds[i].Arguments[1]);
            RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
                createdNpcs[i].Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
            EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
            Assert.Equal(new Vector3(model.X, model.Y, model.Z), position.Position);
            Assert.Equal(1333u, position.Info.Entry.Id);
        }
    }

    private sealed record CreatedNpc(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);

    public static IEnumerable<object[]> DatascapePhaseSpawnCases()
    {
        yield return [PublicEventPhase.Gloomclaw, PublicEventObjective.DefeatGloomclaw, new ExpectedDatascapeSpawn(1100300068u, 30498u, 1609, (uint)PublicEventPhase.Gloomclaw, new Vector3(4310f, -567.817f, -16812f), new Vector3(-3.08841f, 0f, 0f), 32992u, "GloomclawEntityScript", 21000000f)];
        yield return [PublicEventPhase.WarmongerAgratha, PublicEventObjective.DefeatWarmongerAgratha, new ExpectedDatascapeSpawn(1100300071u, 48295u, 1594, (uint)PublicEventPhase.WarmongerAgratha, new Vector3(-4018.35f, -905.709f, 2575.45f), new Vector3(-1.7226f, 0f, 0f), 36939u, "WarmongerAgrathaEntityScript", 12500000f)];
        yield return [PublicEventPhase.WarmongerChuna, PublicEventObjective.DefeatWarmongerChuna, new ExpectedDatascapeSpawn(1100300072u, 48916u, 4478, (uint)PublicEventPhase.WarmongerChuna, new Vector3(-4350.04f, -913.187f, 2404.41f), new Vector3(-1.44454f, 0f, 0f), 36939u, "WarmongerChunaEntityScript", 12500000f)];
        yield return [PublicEventPhase.WarmongerTalarii, PublicEventObjective.DefeatWarmongerTalarii, new ExpectedDatascapeSpawn(1100300073u, 48917u, 4479, (uint)PublicEventPhase.WarmongerTalarii, new Vector3(-4359.49f, -903.383f, 2590.74f), new Vector3(0.19367f, 0f, 0f), 36939u, "WarmongerTalariiEntityScript", 12500000f)];
        yield return [PublicEventPhase.GrandWarmongerTargresh, PublicEventObjective.DefeatGrandWarmongerTargresh, new ExpectedDatascapeSpawn(1100300074u, 48177u, 1479, (uint)PublicEventPhase.GrandWarmongerTargresh, new Vector3(-4403.72f, -821.913f, 2679.14f), new Vector3(-1.63275f, 0f, 0f), 36938u, "GrandWarmongerTargreshEntityScript", 14500000f)];
        yield return [PublicEventPhase.Avatus, PublicEventObjective.DefeatAvatus, new ExpectedDatascapeSpawn(1100300075u, 30505u, 1301, (uint)PublicEventPhase.Avatus, new Vector3(618f, -198.7f, -174f), new Vector3(1.61258f, 0f, 0f), 28937u, "DatascapeAvatusEntityScript", 72000000f)];
    }

    public static IEnumerable<object[]> DatascapeObjectiveStatusSpawnCases()
    {
        yield return [PublicEventObjective.DefeatOptimizedMemoryProbeED1, PublicEventObjective.DefeatTheBioEnhancedBroodmother, new ExpectedDatascapeSpawn(1100300067u, 31885u, 1590, OpeningPhase, new Vector3(2985.78f, -794.348f, 3396.41f), new Vector3(-0.693315f, 0f, 0f), 27107u, "BioEnhancedBroodmotherEntityScript", 18000000f)];
        yield return [PublicEventObjective.GeneratorCharge2, PublicEventObjective.DefeatTheHyperAcceleratedSkeledroid, new ExpectedDatascapeSpawn(1100300069u, 48065u, 2371, (uint)PublicEventPhase.LogicWingRoom1, new Vector3(-22076.3f, 598.146f, -15934.1f), new Vector3(1f, 0f, 0f), 28678u, "HyperAcceleratedSkeledroidEntityScript", 21300000f)];
        yield return [PublicEventObjective.GeneratorCharge8, PublicEventObjective.DefeatTheAugmentedHeraldOfAvatus, new ExpectedDatascapeSpawn(1100300070u, 48374u, 2373, (uint)PublicEventPhase.LogicWingRoom3, new Vector3(-22050.2f, 619.71f, -14309.2f), new Vector3(2.56983f, 0f, 0f), 28901u, "AugmentedHeraldOfAvatusEntityScript", 9100000f)];
    }

    public sealed record ExpectedDatascapeSpawn(
        uint EntityId,
        uint CreatureId,
        ushort AreaId,
        uint Phase,
        Vector3 Position,
        Vector3 Rotation,
        uint DisplayInfo,
        string ScriptName,
        float Health);
}
