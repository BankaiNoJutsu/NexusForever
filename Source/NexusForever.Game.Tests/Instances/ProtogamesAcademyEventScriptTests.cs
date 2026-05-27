using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Dungeon.ProtogamesAcademy;

namespace NexusForever.Game.Tests.Instances;

public class ProtogamesAcademyEventScriptTests
{
    [Fact]
    public void OnLoad_SetsInitialInitiatePhase()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.InitiateProtogamesAcademy, invocation.Arguments[0]);
    }

    [Theory]
    [InlineData(PublicEventPhase.GatherInvulnotron, PublicEventObjective.GatherInvulnotron)]
    [InlineData(PublicEventPhase.GatherGromka, PublicEventObjective.GatherGromka)]
    [InlineData(PublicEventPhase.GatherIrukiBoldbeard, PublicEventObjective.GatherIrukiBoldbeard)]
    [InlineData(PublicEventPhase.Gather, PublicEventObjective.Gather)]
    [InlineData(PublicEventPhase.Gather2, PublicEventObjective.Gather2)]
    [InlineData(PublicEventPhase.GoToLastEvent, PublicEventObjective.GoToTheLastEvent)]
    public void OnPublicEventPhase_GroupObjectives_UseCurrentPlayerCount(PublicEventPhase phase, PublicEventObjective objective)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(3u, out RecordingDispatchProxy<IPublicEvent> eventProxy, out _, out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(objective, activation.Arguments[0]);
        Assert.Equal(3u, activation.Arguments[1]);
    }

    [Theory]
    [InlineData(PublicEventPhase.GatherInvulnotron, 48842u, 7932u, -24400.1f, -974.668f, -28977.1f)]
    [InlineData(PublicEventPhase.GatherGromka, 48843u, 7933u, -24504.9f, -974.749f, -28857.5f)]
    [InlineData(PublicEventPhase.GatherIrukiBoldbeard, 48842u, 7932u, -24400.1f, -974.668f, -28977.1f)]
    [InlineData(PublicEventPhase.TeleporterToTheNextEvent, 50164u, 7936u, -24400.19f, -974.668f, -28977.15f)]
    [InlineData(PublicEventPhase.Gather, 48846u, 7934u, -19804.34f, -945.5437f, -29483.94f)]
    [InlineData(PublicEventPhase.Gather2, 48846u, 7934u, -19804.34f, -945.5437f, -29483.94f)]
    public void OnPublicEventPhase_BranchTriggerPhases_CreateWipGuessedTrigger(
        PublicEventPhase phase,
        uint worldLocationId,
        uint objectId,
        float x,
        float y,
        float z)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            3u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, worldLocationId, objectId);
        AssertTriggerAddedToMap(mapProxy, trigger, new Vector3(x, y, z));
    }

    [Theory]
    [InlineData(PublicEventPhase.GatherGromka, CommunicatorMessage.PhineasARotostar1)]
    [InlineData(PublicEventPhase.DefeatGromka, CommunicatorMessage.PhineasARotostar2)]
    [InlineData(PublicEventPhase.GatherIrukiBoldbeard, CommunicatorMessage.PhineasARotostar3)]
    [InlineData(PublicEventPhase.DefeatIrukiBoldbeard, CommunicatorMessage.PhineasARotostar4)]
    [InlineData(PublicEventPhase.TeleporterToTheNextEvent, CommunicatorMessage.PhineasARotostar5)]
    [InlineData(PublicEventPhase.Gather, CommunicatorMessage.PhineasARotostar6)]
    [InlineData(PublicEventPhase.DefeatIceboxMk2, CommunicatorMessage.PhineasARotostar7)]
    [InlineData(PublicEventPhase.Gather2, CommunicatorMessage.PhineasARotostar8)]
    [InlineData(PublicEventPhase.GoToLastEvent, CommunicatorMessage.PhineasARotostar9)]
    public void OnPublicEventPhase_BranchMessagePhases_BroadcastMappedPhineasMessage(PublicEventPhase phase, CommunicatorMessage messageId)
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

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatGromka, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.GatherIrukiBoldbeard);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalBoss_FinishesEvent()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatWrathbone, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.InitiateProtogamesAcademy, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
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
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3173u });
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

    private static ProtogamesAcademyEventScript CreateScript(params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IReadOnlyDictionary<CommunicatorMessage, ICommunicatorMessage> lookup = messages.ToDictionary(m => m.Id, m => m.Message);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetCommunicatorMessage),
            args => lookup.TryGetValue((CommunicatorMessage)args[0], out ICommunicatorMessage message) ? message : null);
        return new ProtogamesAcademyEventScript(globalQuestManager);
    }

    private static IPlayer CreatePlayer(out IGameSession session)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        session = RecordingDispatchProxy<IGameSession>.Create(out _);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
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
        Assert.Equal(3173u, position.Info.Entry.Id);
    }

    private sealed record CreatedTrigger(
        IWorldLocationVolumeGridTriggerEntity Instance,
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> Proxy);
}
