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
using NexusForever.Script.Instance.Dungeon.ColdbloodCitadel;

namespace NexusForever.Game.Tests.Instances;

public class ColdbloodCitadelEventScriptTests
{
    [Fact]
    public void OnLoad_SetsInitialEnterPhase()
    {
        ColdbloodCitadelEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.Enter, invocation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_CreatesBranchGatherTrigger()
    {
        ColdbloodCitadelEventScript script = CreateScript(out _, out RecordingDispatchProxy<IMapInstance> mapProxy, out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 53206u, 8656u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(604.33f, -475.452f, -322.957f));
    }

    [Fact]
    public void OnPublicEventPhase_HailStoneGatecrasher_ActivatesMainAndOptionalBranchObjectives()
    {
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        ColdbloodCitadelEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _,
            true,
            (CommunicatorMessage.TowerEngineerRenhakul1, message));
        IWorldEntity gatherRing = CreateWorldEntity(PublicEventCreature.GatherRing, 701u, out RecordingDispatchProxy<IWorldEntity> gatherRingProxy);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(75624u, 702u, out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] switch
        {
            701u => gatherRing,
            702u => trigger,
            _    => null
        });

        script.OnAddToMap(gatherRing);
        script.OnAddToMap(trigger);
        script.OnPublicEventPhase((uint)PublicEventPhase.HailStoneGatecrasher);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatHailStoneGatecrasher);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.SavePellFightingTheOsun);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.StealSampleOfLiquidSoulfrost);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.GatherSoulfrostShards);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.RallyTheWinterfuryPell);
        Assert.Single(gatherRingProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
    }

    [Fact]
    public void OnPublicEventPhase_IceBloodCoven_ActivatesBranchTrapOptionalObjective()
    {
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        ColdbloodCitadelEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _,
            true,
            (CommunicatorMessage.TowerEngineerRenhakul11, message));

        script.OnPublicEventPhase((uint)PublicEventPhase.IceBloodCoven);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatTheIcebloodCoven);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.RescueThePellArchitect);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.KillKrovakSummonersAndTheirFrostguards);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.ConcurrentCovenCollapse);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.RescueWinterfuryPrisoners);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DestroySoulrotCanisters);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DisableSoulfrostTraps);
        Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
    }

    [Fact]
    public void OnPublicEventPhase_RisenHarizog_ActivatesFinalBranchObjectives()
    {
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        ColdbloodCitadelEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _,
            false,
            (CommunicatorMessage.TowerEngineerRenhakul4, message));

        script.OnPublicEventPhase((uint)PublicEventPhase.RisenHarizog);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatTheRisenHarizog);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.InfusionInterdiction);
        Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
    }

    [Theory]
    [InlineData(PublicEventObjective.FindThePellAttackingColdbloodCitadel, PublicEventPhase.HailStoneGatecrasher)]
    [InlineData(PublicEventObjective.DefeatHailStoneGatecrasher, PublicEventPhase.IceBloodCoven)]
    [InlineData(PublicEventObjective.DefeatTheIcebloodCoven, PublicEventPhase.RisenHarizog)]
    public void OnPublicEventObjectiveStatus_Success_AdvancesBranchPhaseChain(PublicEventObjective objective, PublicEventPhase nextPhase)
    {
        ColdbloodCitadelEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == nextPhase);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalBoss_FinishesEvent()
    {
        ColdbloodCitadelEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatTheRisenHarizog, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        ColdbloodCitadelEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FindThePellAttackingColdbloodCitadel, PublicEventStatus.Active));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    private static ColdbloodCitadelEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript(out eventProxy, out _, out _, false, preserveLoadInvocations, messages);
    }

    private static ColdbloodCitadelEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers,
        bool alwaysActivateOptionalObjectives = false,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript(out eventProxy, out mapProxy, out createdTriggers, alwaysActivateOptionalObjectives, false, messages);
    }

    private static ColdbloodCitadelEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers,
        bool alwaysActivateOptionalObjectives,
        bool preserveLoadInvocations,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager(messages);
        ColdbloodCitadelEventScript script = alwaysActivateOptionalObjectives
            ? new AlwaysOptionalColdbloodCitadelEventScript(globalQuestManager)
            : new ColdbloodCitadelEventScript(globalQuestManager);
        IPublicEvent publicEvent = CreatePublicEvent(out eventProxy, out mapProxy, out createdTriggers);
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static IPublicEvent CreatePublicEvent(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers)
    {
        IPlayer player = CreatePlayer(out _);
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2058u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), new[] { player });

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

    private static IGlobalQuestManager CreateGlobalQuestManager(params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IReadOnlyDictionary<CommunicatorMessage, ICommunicatorMessage> lookup = messages.ToDictionary(m => m.Id, m => m.Message);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetCommunicatorMessage),
            args => lookup.TryGetValue((CommunicatorMessage)args[0], out ICommunicatorMessage message) ? message : null);
        return globalQuestManager;
    }

    private static CreatedTrigger CreateTrigger()
    {
        IWorldLocationVolumeGridTriggerEntity trigger = RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Create(
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        return new CreatedTrigger(trigger, triggerProxy);
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

    private static IPlayer CreatePlayer(out IGameSession session)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        session = RecordingDispatchProxy<IGameSession>.Create(out _);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
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

    private static void AssertObjectiveActivated(RecordingDispatchProxy<IPublicEvent> eventProxy, PublicEventObjective objective)
    {
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
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
        IGridEntity trigger,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
        Assert.Same(trigger, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(2058u, position.Info.Entry.Id);
    }

    private sealed class AlwaysOptionalColdbloodCitadelEventScript : ColdbloodCitadelEventScript
    {
        public AlwaysOptionalColdbloodCitadelEventScript(IGlobalQuestManager globalQuestManager)
            : base(globalQuestManager)
        {
        }

        protected override bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return true;
        }
    }

    private sealed record CreatedTrigger(
        IWorldLocationVolumeGridTriggerEntity Instance,
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> Proxy);
}
