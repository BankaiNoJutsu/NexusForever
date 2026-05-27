using System.Numerics;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Expedition.Gauntlet;

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

    [Theory]
    [InlineData(PublicEventPhase.GetIntoAirlock, PublicEventObjective.GetIntoAirlock)]
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

    [Theory]
    [InlineData(PublicEventPhase.GetIntoAirlock, 38909u, 5735u, 523.0805f, 0.1994047f, -507.9437f)]
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
    public void OnPublicEventPhase_WhatHappened_QueuesWipCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);
        var script = CreateScript(cinematic);
        IPublicEvent publicEvent = CreatePublicEvent(1u, [player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.WhatHappened);

        AssertCinematicQueued(cinematicManagerProxy, cinematic);
    }

    [Fact]
    public void OnPublicEventPhase_DefeatBrickBraggor_QueuesWipCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);
        var script = CreateScript(cinematic);
        IPublicEvent publicEvent = CreatePublicEvent(1u, [player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatBrickBraggor);

        AssertCinematicQueued(cinematicManagerProxy, cinematic);
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

    private static void AssertTriggerInitialised(CreatedTrigger trigger, uint worldLocationId, uint objectId)
    {
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Invocation initialise = Assert.Single(
            trigger.Proxy.GetInvocations(nameof(IWorldLocationVolumeGridTriggerEntity.Initialise)));
        Assert.Equal(worldLocationId, initialise.Arguments[0]);
        Assert.Equal(objectId, initialise.Arguments[1]);
    }

    private static void AssertTriggerAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        CreatedTrigger trigger,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
        Assert.Same(trigger.Instance, enqueueAdd.Arguments[0]);

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
}
