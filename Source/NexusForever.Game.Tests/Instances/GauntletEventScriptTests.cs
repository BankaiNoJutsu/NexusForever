using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Expedition.Gauntlet;
using NexusForever.Script.Instance.Expedition.Gauntlet.Script;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Game.Tests.Instances;

public class GauntletEventScriptTests
{
    [Fact]
    public void OnLoad_SetsInitialTalkToPilotPhase()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.TalkToPilotTaboro, invocation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_TalkToPilotTaboro_SpawnsReviewedPilotTaboroPlacement()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithPilotTaboro(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedNpc pilotTaboro);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.TalkToPilotTaboro);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.TalkToPilotTaboro, activation.Arguments[0]);
        AssertPilotTaboroModel(pilotTaboro, new Vector3(541.0117f, 0.2229719f, -513.6139f));
        AssertGridEntityAddedToMap(mapProxy, pilotTaboro.Instance, new Vector3(541.0117f, 0.2229719f, -513.6139f));
    }

    [Theory]
    [InlineData(PublicEventPhase.GatherAtTheSwarmPit, PublicEventObjective.GatherAtTheSwarmPit)]
    [InlineData(PublicEventPhase.EnterTheChamberOfChoices, PublicEventObjective.EnterTheChamberOfChoices)]
    [InlineData(PublicEventPhase.EnterTheMainEventArena, PublicEventObjective.EnterTheMainEventArena)]
    public void OnPublicEventPhase_GroupObjectives_UseCurrentPlayerCount(PublicEventPhase phase, PublicEventObjective objective)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(5u, out RecordingDispatchProxy<IPublicEvent> eventProxy, out _, out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(objective, activation.Arguments[0]);
        Assert.Equal(5u, activation.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventPhase_GetIntoAirlock_SpawnsReviewedGatherMarkerAndTrigger()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithAirlockGatherMarker(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedSimple gatherMarker,
            out List<CreatedTrigger> createdTriggers);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.GetIntoAirlock);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GetIntoAirlock, activation.Arguments[0]);
        Assert.Equal(5u, activation.Arguments[1]);

        Vector3 position = new(523.0805f, 0.1994047f, -507.9437f);
        AssertAirlockGatherMarkerModel(gatherMarker, position);
        AssertGridEntityAddedToMap(mapProxy, gatherMarker.Instance, position);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 38909u, 5735u);
        AssertTriggerAddedToMap(mapProxy, trigger, position);
    }

    [Fact]
    public void OnPublicEventPhase_GetIntoAirlock_DoesNotDuplicateReviewedGatherMarker()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithAirlockGatherMarker(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedSimple gatherMarker,
            out List<CreatedTrigger> createdTriggers);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.GetIntoAirlock);
        script.OnPublicEventPhase((uint)PublicEventPhase.GetIntoAirlock);

        Assert.Single(gatherMarker.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Equal(2, createdTriggers.Count);
        Assert.Single(
            mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)),
            i => ReferenceEquals(gatherMarker.Instance, i.Arguments[0]));
    }

    [Theory]
    [InlineData(PublicEventPhase.GatherAtTheSwarmPit, 39001u, 5753u, -1007.078f, 5.185604E-06f, 1063.784f)]
    public void OnPublicEventPhase_BranchGatherPhases_CreateWipGuessedTrigger(
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
            out List<CreatedTrigger> createdTriggers);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, worldLocationId, objectId);
        AssertTriggerAddedToMap(mapProxy, trigger, new Vector3(x, y, z));
    }

    [Fact]
    public void OnPublicEventPhase_SurviveTheFirstArena_ActivatesMappedScriptObjective()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SurviveTheFirstArena);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.SurviveTheFirstArena, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_ChamberOfChoices_ActivatesBothBranchEntrancesAndSideObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(2u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ChamberOfChoices);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i =>
            (PublicEventObjective)i.Arguments[0] == PublicEventObjective.EnterTheFactionFrictionArena
            && (uint)i.Arguments[1] == 2u);
        Assert.Contains(activations, i =>
            (PublicEventObjective)i.Arguments[0] == PublicEventObjective.EnterTheCharnelChamber
            && (uint)i.Arguments[1] == 2u);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.EndorseAProduct);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.SaveGauntletContestantsFromTheDarkspur);
    }

    [Theory]
    [InlineData(PublicEventObjective.EnterTheCharnelChamber, PublicEventObjective.KillTheChampionator)]
    [InlineData(PublicEventObjective.EnterTheFactionFrictionArena, PublicEventObjective.KillTheOpposingFactionsTeam)]
    public void OnPublicEventObjectiveStatus_BranchEntranceSuccess_ActivatesBranchKillObjective(PublicEventObjective entrance, PublicEventObjective killObjective)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(entrance, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(killObjective, activation.Arguments[0]);
    }

    [Theory]
    [InlineData(PublicEventObjective.KillTheChampionator)]
    [InlineData(PublicEventObjective.KillTheOpposingFactionsTeam)]
    public void OnPublicEventObjectiveStatus_BranchKillSuccess_AdvancesToMainEventArena(PublicEventObjective killObjective)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(killObjective, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.EnterTheMainEventArena);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SurviveTheFirstArena_AdvancesToChamberOfChoices()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SurviveTheFirstArena, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.EnterTheChamberOfChoices);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalNpc_FinishesEvent()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.TalkToJudgeKain, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.TalkToPilotTaboro, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Fact]
    public void OnPublicEventPhase_WhatHappened_QueuesWipCinematicAndCreditsScriptWithoutCountObjective()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);
        var script = CreateScript(cinematic);
        IPublicEvent publicEvent = CreatePublicEvent(1u, [player], out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.WhatHappened);

        AssertCinematicQueued(cinematicManagerProxy, cinematic);
        RecordingDispatchProxy<IPublicEvent>.Invocation update = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.FindOutWhatHappenedToYou, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventPhase_SurviveTheMainEvent_ActivatesReviewedGoonSquadObjective()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SurviveTheMainEvent);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.SurviveTheMainEvent);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatTheGoonSquad);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DefeatTheGoonSquad_AdvancesToSliceAndDice()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatTheGoonSquad, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.DefeatSliceAndDice);
    }

    [Fact]
    public void OnPublicEventPhase_DefeatSliceAndDice_ActivatesReviewedBossObjective()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatSliceAndDice);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.DefeatSliceAndDice, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DefeatSliceAndDice_AdvancesToShockKing()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatSliceAndDice, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.DefeatTheShockKing);
    }

    [Fact]
    public void OnPublicEventPhase_DefeatTheShockKing_ActivatesReviewedBossObjective()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatTheShockKing);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.DefeatTheShockKing, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DefeatTheShockKing_AdvancesToBrickBraggor()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatTheShockKing, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.DefeatBrickBraggor);
    }

    [Fact]
    public void OnPublicEventPhase_DefeatBrickBraggor_QueuesWipCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);
        var script = CreateScript(cinematic);
        IPublicEvent publicEvent = CreatePublicEvent(1u, [player], out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatBrickBraggor);

        AssertCinematicQueued(cinematicManagerProxy, cinematic);
        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.DefeatBrickBraggor, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DefeatBrickBraggor_AdvancesToNpcTalk()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatBrickBraggor, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.TalkToNpc);
    }

    [Theory]
    [MemberData(nameof(GauntletTalkObjectiveFilterCases))]
    public void GauntletTalkObjectiveScripts_AreBoundToMappedCreatures(
        Type scriptType,
        uint expectedCreatureId)
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { expectedCreatureId }, attribute.CreatureId);
    }

    [Theory]
    [MemberData(nameof(GauntletTalkObjectiveCreditCases))]
    public void GauntletTalkObjectiveScripts_OnActivateSuccess_UpdatesActiveTalkObjectiveByTargetGroupForPlayer(
        Type scriptType,
        uint expectedTargetGroupId)
    {
        var script = (GauntletTalkObjectiveEntityScriptBase)Activator.CreateInstance(scriptType);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity worldEntity);

        script.OnLoad(worldEntity);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Same(player, update.Arguments[0]);
        Assert.Equal(PublicEventObjectiveType.TalkTo, update.Arguments[1]);
        Assert.Equal(expectedTargetGroupId, update.Arguments[2]);
        Assert.Equal(1, update.Arguments[3]);
    }

    [Fact]
    public void ProductEndorsementScript_IsBoundToMappedProductCreatures()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(ProductEndorsementEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 49218u, 49429u, 49486u, 49536u, 49573u }, attribute.CreatureId);
    }

    [Fact]
    public void ProductEndorsement_OnActivateSuccess_UpdatesActiveTargetGroupObjectiveOnce()
    {
        var script = new ProductEndorsementEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity product);

        script.OnLoad(product);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(7038u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void ElectricRoomDoorLockScript_IsBoundToMappedDoorLocks()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(ElectricRoomDoorLockEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 48682u, 48683u, 48684u }, attribute.CreatureId);
    }

    [Fact]
    public void ElectricRoomDoorLock_OnActivateSuccess_UpdatesActiveTargetGroupObjectiveOnce()
    {
        var script = new ElectricRoomDoorLockEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity doorLock);

        script.OnLoad(doorLock);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(6831u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    public static IEnumerable<object[]> GauntletTalkObjectiveFilterCases()
    {
        yield return [typeof(PilotTaboroEntityScript), 48580u];
        yield return [typeof(JudgeKainEntityScript), 48945u];
        yield return [typeof(AgentLexEntityScript), 48950u];
    }

    public static IEnumerable<object[]> GauntletTalkObjectiveCreditCases()
    {
        yield return [typeof(PilotTaboroEntityScript), 6818u];
        yield return [typeof(JudgeKainEntityScript), 6898u];
        yield return [typeof(AgentLexEntityScript), 6899u];
    }

    private static IPublicEvent CreatePublicEvent(uint playerCount, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(playerCount, [], out eventProxy, out _, out _);
    }

    private static IPublicEvent CreatePublicEvent(
        uint playerCount,
        IReadOnlyList<IPlayer> players,
        out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(playerCount, players, out eventProxy, out _, out _);
    }

    private static IPublicEvent CreatePublicEvent(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers)
    {
        return CreatePublicEvent(playerCount, [], out eventProxy, out mapProxy, out createdTriggers);
    }

    private static IPublicEvent CreatePublicEvent(
        uint playerCount,
        IReadOnlyList<IPlayer> players,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2183u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<CreatedTrigger> triggers = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            CreatedTrigger trigger = CreateTrigger();
            triggers.Add(trigger);
            return trigger.Instance;
        });

        createdTriggers = triggers;
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithPilotTaboro(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedNpc pilotTaboro)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), 1u);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2183u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        pilotTaboro = CreateNpc();
        eventProxy.SetMethodReturn(nameof(IPublicEvent.CreateEntity), pilotTaboro.Instance);
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithAirlockGatherMarker(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedSimple gatherMarker,
        out List<CreatedTrigger> createdTriggers)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), 5u);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2183u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        CreatedSimple marker = CreateSimple();
        gatherMarker = marker;
        bool gatherMarkerCreated = false;
        List<CreatedTrigger> triggers = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            if (!gatherMarkerCreated)
            {
                gatherMarkerCreated = true;
                return marker.Instance;
            }

            CreatedTrigger trigger = CreateTrigger();
            triggers.Add(trigger);
            return trigger.Instance;
        });

        createdTriggers = triggers;
        return publicEvent;
    }

    private static GauntletEventScript CreateScript()
    {
        return CreateScript(RecordingDispatchProxy<ICinematicBase>.Create(out _));
    }

    private static GauntletEventScript CreateScript(ICinematicBase cinematic)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out RecordingDispatchProxy<ICinematicFactory> cinematicFactoryProxy);
        cinematicFactoryProxy.SetMethodReturn(nameof(ICinematicFactory.CreateCinematic), cinematic);
        return new GauntletEventScript(cinematicFactory);
    }

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy)
    {
        ICinematicManager cinematicManager = RecordingDispatchProxy<ICinematicManager>.Create(out cinematicManagerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CinematicManager), cinematicManager);
        return player;
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateWorldEntityWithPublicEventManager(out IWorldEntity worldEntity)
    {
        worldEntity = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> worldEntityProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        worldEntityProxy.SetProperty(nameof(IGridEntity.Map), map);

        return publicEventManagerProxy;
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

    private static CreatedTrigger CreateTrigger()
    {
        IWorldLocationVolumeGridTriggerEntity trigger = RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Create(
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        return new CreatedTrigger(trigger, triggerProxy);
    }

    private static CreatedNpc CreateNpc()
    {
        INonPlayerEntity npc = RecordingDispatchProxy<INonPlayerEntity>.Create(
            out RecordingDispatchProxy<INonPlayerEntity> npcProxy);
        return new CreatedNpc(npc, npcProxy);
    }

    private static CreatedSimple CreateSimple()
    {
        ISimpleEntity simple = RecordingDispatchProxy<ISimpleEntity>.Create(
            out RecordingDispatchProxy<ISimpleEntity> simpleProxy);
        return new CreatedSimple(simple, simpleProxy);
    }

    private static void AssertTriggerInitialised(CreatedTrigger trigger, uint worldLocationId, uint objectId)
    {
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Invocation initialise = Assert.Single(
            trigger.Proxy.GetInvocations(nameof(IWorldLocationVolumeGridTriggerEntity.Initialise)));
        Assert.Equal(worldLocationId, initialise.Arguments[0]);
        Assert.Equal(objectId, initialise.Arguments[1]);
    }

    private static void AssertPilotTaboroModel(CreatedNpc npc, Vector3 position)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            npc.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(1100300013u, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(48580u, model.Creature);
        Assert.Equal((ushort)2183u, model.World);
        Assert.Equal((ushort)2620u, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(21338u, model.DisplayInfo);
        Assert.Equal((ushort)219u, model.Faction1);
        Assert.Equal((ushort)219u, model.Faction2);
        Assert.Equal(446u, model.EntityEvent.EventId);
        Assert.Equal(0u, model.EntityEvent.Phase);
        Assert.Empty(model.EntityScript);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Health && stat.Value == 1f);
    }

    private static void AssertAirlockGatherMarkerModel(CreatedSimple marker, Vector3 position)
    {
        RecordingDispatchProxy<ISimpleEntity>.Invocation initialise = Assert.Single(
            marker.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(1100300014u, model.Id);
        Assert.Equal(EntityType.Simple, model.Type);
        Assert.Equal(58979u, model.Creature);
        Assert.Equal((ushort)2183u, model.World);
        Assert.Equal((ushort)2620u, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(Vector3.Zero.X, model.Rx);
        Assert.Equal(Vector3.Zero.Y, model.Ry);
        Assert.Equal(Vector3.Zero.Z, model.Rz);
        Assert.Equal(30327u, model.DisplayInfo);
        Assert.Equal((ushort)0u, model.OutfitInfo);
        Assert.Equal((ushort)219u, model.Faction1);
        Assert.Equal((ushort)219u, model.Faction2);
        Assert.Null(model.EntityEvent);
        Assert.Empty(model.EntityScript);
        Assert.Empty(model.EntityStat);
    }

    private static void AssertTriggerAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        CreatedTrigger trigger,
        Vector3 expectedPosition)
    {
        AssertGridEntityAddedToMap(mapProxy, trigger.Instance, expectedPosition);
    }

    private static void AssertGridEntityAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        IGridEntity entity,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(
            mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)),
            i => ReferenceEquals(entity, i.Arguments[0]));
        Assert.Same(entity, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(2183u, position.Info.Entry.Id);
    }

    private static void AssertCinematicQueued(RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy, ICinematicBase cinematic)
    {
        RecordingDispatchProxy<ICinematicManager>.Invocation queue = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queue.Arguments[0]);
    }

    private sealed record CreatedTrigger(
        IWorldLocationVolumeGridTriggerEntity Instance,
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> Proxy);

    private sealed record CreatedNpc(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);

    private sealed record CreatedSimple(
        ISimpleEntity Instance,
        RecordingDispatchProxy<ISimpleEntity> Proxy);
}
