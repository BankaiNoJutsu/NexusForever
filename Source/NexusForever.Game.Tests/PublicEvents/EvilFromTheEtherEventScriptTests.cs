using System.Numerics;
using NexusForever.Game.Abstract.Cinematic;
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
using NexusForever.Script.Instance.Expedition.EvilFromTheEther;

namespace NexusForever.Game.Tests.PublicEvents;

public class EvilFromTheEtherEventScriptTests
{
    [Fact]
    public void OnLoad_SetsInitialTalkToCaptainWeirPhase()
    {
        _ = CreateScript(out var publicEventProxy, out _, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.TalkToCaptainWeir, invocation.Arguments[0]);
    }

    [Fact]
    public void GoToAirlock_ActivatesGroupObjectiveAndCreatesWipGuessedGatherTrigger()
    {
        EvilFromTheEtherEventScript script = CreateScript(
            out var publicEventProxy,
            out var mapProxy,
            out CreatedWorldLocationTrigger trigger);

        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), 4u);

        script.OnPublicEventPhase((uint)PublicEventPhase.GoToAirlock);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GoToAirlock, activation.Arguments[0]);
        Assert.Equal(4u, activation.Arguments[1]);
        AssertWorldLocationTrigger(trigger, 50278u, 8242u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(-396.43555f, -840.7188f, 119.74138f));
    }

    [Fact]
    public void OpenMedbay_ActivatesObjectivesTeleportsPlayersAndCleansGatherEntities()
    {
        RecordingDispatchProxy<IGlobalQuestManager> questManagerProxy;
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out var mapProxy, out questManagerProxy);
        IPlayer player = CreatePlayer(42ul, out var playerProxy);
        IWorldEntity gatherRing = CreateWorldEntity(PublicEventCreature.GatherRing, 501u, out var gatherRingProxy);
        IWorldLocationVolumeGridTriggerEntity gatherTrigger = CreateWorldLocationTrigger(50278u, 502u, out var gatherTriggerProxy);
        ICommunicatorMessage communicatorMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out var communicatorMessageProxy);

        script.OnAddToMap(gatherRing);
        script.OnAddToMap(gatherTrigger);

        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), new[] { player });
        mapProxy.SetMethodHandler(nameof(IMapInstance.GetEntity), args =>
        {
            uint guid = (uint)args[0];
            return guid switch
            {
                501u => gatherRing,
                502u => gatherTrigger,
                _    => null
            };
        });
        questManagerProxy.SetMethodReturn(nameof(IGlobalQuestManager.GetCommunicatorMessage), communicatorMessage);

        script.OnPublicEventPhase((uint)PublicEventPhase.OpenMedbay);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = publicEventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.OpenMedbay);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DownloadCrewLogs);

        RecordingDispatchProxy<IPlayer>.Invocation teleport = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportToLocal)));
        Assert.Equal(new Vector3(53.180374f, -852.86273f, -91.41684f), teleport.Arguments[0]);

        Assert.Single(gatherRingProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        Assert.Single(gatherTriggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));

        RecordingDispatchProxy<IGlobalQuestManager>.Invocation messageLookup = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
        Assert.Equal(CommunicatorMessage.CaptainWeir2, messageLookup.Arguments[0]);
        Assert.Single(communicatorMessageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
    }

    [Fact]
    public void GoToPrimaryPowerPlant_SeedsPlayersAlreadyInsideDoorRange()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out var mapProxy);
        IDoorEntity door = CreateDoor(7059788ul, 321u, [11ul, 12ul]);

        script.OnAddToMap(door);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), 3u);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetEntity), door);

        script.OnPublicEventPhase((uint)PublicEventPhase.GoToPrimaryPowerPlant);

        RecordingDispatchProxy<IPublicEvent>.Invocation activateInvocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GoToPrimaryPowerPlant, activateInvocation.Arguments[0]);
        Assert.Equal(3u, activateInvocation.Arguments[1]);

        RecordingDispatchProxy<IPublicEvent>.Invocation updateInvocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.GoToPrimaryPowerPlant, updateInvocation.Arguments[0]);
        Assert.Equal(2, updateInvocation.Arguments[1]);
    }

    [Fact]
    public void GoToPrimaryPowerPlant2_ReSeedsPlayersAfterObjectiveReset()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out var mapProxy);
        IDoorEntity door = CreateDoor(7024518ul, 654u, [21ul]);

        script.OnAddToMap(door);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetEntity), door);

        script.OnPublicEventPhase((uint)PublicEventPhase.GoToPrimaryPowerPlant2);

        RecordingDispatchProxy<IPublicEvent>.Invocation resetInvocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ResetObjective)));
        Assert.Equal(PublicEventObjective.GoToPrimaryPowerPlant, resetInvocation.Arguments[0]);

        RecordingDispatchProxy<IPublicEvent>.Invocation activateInvocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GoToPrimaryPowerPlant, activateInvocation.Arguments[0]);
        Assert.Equal(0u, activateInvocation.Arguments[1]);

        RecordingDispatchProxy<IPublicEvent>.Invocation updateInvocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.GoToPrimaryPowerPlant, updateInvocation.Arguments[0]);
        Assert.Equal(1, updateInvocation.Arguments[1]);
    }

    [Fact]
    public void ObjectiveStatus_SuccessAdvancesBranchPhaseChainAndFinishesFinalStep()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out _);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.TalkToCaptainWeir, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.TalkToCaptainWeir2, PublicEventStatus.Succeeded));

        Assert.Contains(publicEventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.GoToAirlock);

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void ObjectiveStatus_IncompleteDoesNotAdvance()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out _);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.TalkToCaptainWeir, PublicEventStatus.Active));

        Assert.Empty(publicEventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(publicEventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Theory]
    [InlineData(PublicEventPhase.TalkToCaptainWeir, CommunicatorMessage.CaptainWeir1)]
    [InlineData(PublicEventPhase.ScavengeSpareParts, CommunicatorMessage.CaptainWeir4)]
    [InlineData(PublicEventPhase.DefeatEthericOrganisms, CommunicatorMessage.CaptainWeir12)]
    public void CinematicFinish_SendsWipGuessedPhaseMessage(PublicEventPhase phase, CommunicatorMessage expectedMessage)
    {
        RecordingDispatchProxy<IGlobalQuestManager> questManagerProxy;
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out _, out questManagerProxy);
        IPlayer player = CreatePlayer(99ul, out _);
        ICommunicatorMessage communicatorMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out var communicatorMessageProxy);

        publicEventProxy.SetProperty(nameof(IPublicEvent.Phase), (uint)phase);
        questManagerProxy.SetMethodReturn(nameof(IGlobalQuestManager.GetCommunicatorMessage), communicatorMessage);

        script.OnCinematicFinish(player, 1234u);

        RecordingDispatchProxy<IGlobalQuestManager>.Invocation messageLookup = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
        Assert.Equal(expectedMessage, messageLookup.Arguments[0]);
        Assert.Single(communicatorMessageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
    }

    private static EvilFromTheEtherEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> publicEventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        bool preserveLoadInvocations = false)
    {
        return CreateScript(out publicEventProxy, out mapProxy, out _, preserveLoadInvocations);
    }

    private static EvilFromTheEtherEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> publicEventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out RecordingDispatchProxy<IGlobalQuestManager> questManagerProxy,
        bool preserveLoadInvocations = false)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out _);
        IGlobalQuestManager questManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out questManagerProxy);
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out publicEventProxy);
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);

        publicEventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3404u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        var script = new EvilFromTheEtherEventScript(cinematicFactory, questManager);
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            publicEventProxy.Invocations.Clear();
        return script;
    }

    private static EvilFromTheEtherEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> publicEventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedWorldLocationTrigger createdTrigger)
    {
        EvilFromTheEtherEventScript script = CreateScript(out publicEventProxy, out mapProxy);
        CreatedWorldLocationTrigger trigger = CreateWorldLocationTrigger();
        publicEventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () => trigger.Instance);
        createdTrigger = trigger;
        return script;
    }

    private static IDoorEntity CreateDoor(ulong activePropId, uint guid, ulong[] playerCharacterIds)
    {
        IDoorEntity door = RecordingDispatchProxy<IDoorEntity>.Create(out var doorProxy);
        doorProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        doorProxy.SetProperty(nameof(IWorldEntity.CreatureId), (uint)PublicEventCreature.Door);
        doorProxy.SetProperty(nameof(IWorldEntity.ActivePropId), activePropId);
        doorProxy.SetMethodReturn(nameof(IGridEntity.GetInRange), playerCharacterIds.Select(CreatePlayer).ToArray());
        return door;
    }

    private static IWorldEntity CreateWorldEntity(
        PublicEventCreature creature,
        uint guid,
        out RecordingDispatchProxy<IWorldEntity> entityProxy)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureId), (uint)creature);
        return entity;
    }

    private static IWorldLocationVolumeGridTriggerEntity CreateWorldLocationTrigger(
        uint worldLocationId,
        uint guid,
        out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy)
    {
        IWorldLocationVolumeGridTriggerEntity trigger = RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Create(out triggerProxy);
        triggerProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        triggerProxy.SetProperty(nameof(IWorldLocationVolumeGridTriggerEntity.Entry), new WorldLocation2Entry
        {
            Id = worldLocationId
        });
        return trigger;
    }

    private static CreatedWorldLocationTrigger CreateWorldLocationTrigger()
    {
        IWorldLocationVolumeGridTriggerEntity trigger = RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Create(
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        return new CreatedWorldLocationTrigger(trigger, triggerProxy);
    }

    private static IPlayer CreatePlayer(ulong characterId)
    {
        return CreatePlayer(characterId, out _);
    }

    private static IPlayer CreatePlayer(ulong characterId, out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out _);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static IPublicEventObjective CreateObjective(PublicEventObjective objective, PublicEventStatus status)
    {
        IPublicEventObjective eventObjective = RecordingDispatchProxy<IPublicEventObjective>.Create(out var objectiveProxy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry
        {
            Id = (uint)objective
        });
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Status), status);
        return eventObjective;
    }

    private static void AssertWorldLocationTrigger(CreatedWorldLocationTrigger trigger, uint worldLocationId, uint objectId)
    {
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Invocation initialise = Assert.Single(
            trigger.Proxy.GetInvocations(nameof(IWorldLocationVolumeGridTriggerEntity.Initialise)));
        Assert.Equal(worldLocationId, initialise.Arguments[0]);
        Assert.Equal(objectId, initialise.Arguments[1]);
    }

    private static void AssertTriggerAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        IGridEntity trigger,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
        Assert.Same(trigger, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(3404u, position.Info.Entry.Id);
    }

    private sealed record CreatedWorldLocationTrigger(
        IWorldLocationVolumeGridTriggerEntity Instance,
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> Proxy);
}
