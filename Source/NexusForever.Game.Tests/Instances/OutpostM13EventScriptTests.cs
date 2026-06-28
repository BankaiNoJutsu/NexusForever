using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Expedition.OutpostM13;
using NexusForever.Script.Instance.Expedition.OutpostM13.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Game.Tests.Instances;

public class OutpostM13EventScriptTests
{
    [Fact]
    public void OnLoad_SetsInitialTalkToCaptainMiloPhase()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.TalkToCaptainMilo, invocation.Arguments[0]);
    }

    [Fact]
    public void OnLoad_ActivatesGoldMedalTimerObjective()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GoldMedalTimer, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_TalkToCaptainMilo_SpawnsReviewedCaptainMiloPlacement()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedCaptainMilo(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.TalkToCaptainMilo);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.TalkToCaptainMilo);
        Assert.Equal(PublicEventObjective.TalkToCaptainMilo, activation.Arguments[0]);

        CreatedNpc captainMilo = Assert.Single(createdNpcs);
        Vector3 position = new(9791.18f, -932.668f, 5264.279f);
        AssertOutpostM13NpcModel(captainMilo, 1100300017u, 41201u, 1540, 0u, position, 28578u, 0, 466);
        AssertGridEntityAddedToMap(mapProxy, captainMilo.Instance, position);
    }

    [Fact]
    public void OnPublicEventPhase_TalkToCaptainMilo_DoesNotDuplicateReviewedSpawn()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedCaptainMilo(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.TalkToCaptainMilo);
        script.OnPublicEventPhase((uint)PublicEventPhase.TalkToCaptainMilo);

        Assert.Single(createdNpcs);
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Fact]
    public void OnPublicEventPhase_KillTheNovaburnMarauders_ActivatesCargoHoldMarauderObjective()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(3u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.KillTheNovaburnMarauders);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.KillNovaburnMarauders);
        Assert.Equal(PublicEventObjective.KillNovaburnMarauders, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_KillRansackerRorgh_ActivatesNamedMarauderObjective()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(3u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.KillRansackerRorgh);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.KillRansackerRorgh);
        Assert.Equal(PublicEventObjective.KillRansackerRorgh, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_GoToTheAsteroidSurface_ActivatesExitPanelObjective()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(3u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.GoToTheAsteroidSurface);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.GoToTheAsteroidSurface);
        Assert.Equal(PublicEventObjective.GoToTheAsteroidSurface, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_KillHiveQueen_ActivatesHiveQueenObjective()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(3u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.KillHiveQueen);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.KillHiveQueen);
        Assert.Equal(PublicEventObjective.KillHiveQueen, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_DefeatHivePods_ActivatesMappedScriptObjective()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(3u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatHivePods);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatHivePods);
        Assert.Equal(PublicEventObjective.DefeatHivePods, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_FindForemanKrause_ActivatesMappedTalkObjective()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(3u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FindForemanKrause);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.FindForemanKrause);
        Assert.Equal(PublicEventObjective.FindForemanKrause, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_CollectDatachrons_ActivatesMappedVirtualCollectObjective()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(3u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.CollectDatachrons);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.CollectDatachrons);
        Assert.Equal(PublicEventObjective.CollectDatachrons, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_HeadToMilosShuttle_UsesCurrentPlayerCount()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(3u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.HeadToMilosShuttle);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.HeadToMilosShuttle);
        Assert.Equal(PublicEventObjective.HeadToMilosShuttle, activation.Arguments[0]);
        Assert.Equal(3u, activation.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_KillNovaburnMaraudersSuccess_AdvancesToKillRansackerRorgh()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.KillNovaburnMarauders, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.KillRansackerRorgh);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_KillRansackerRorghSuccess_AdvancesToAsteroidSurface()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.KillRansackerRorgh, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.GoToTheAsteroidSurface);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_KillHiveQueenSuccess_AdvancesToHeadToMilosShuttle()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.KillHiveQueen, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.HeadToMilosShuttle);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FindForemanKrauseSuccess_AdvancesToCollectDatachrons()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FindForemanKrause, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.CollectDatachrons);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_CollectDatachronsSuccess_AdvancesToSearchTheMine()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.CollectDatachrons, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.SearchTheMine);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SearchTheMineSuccess_AdvancesToDefeatHivePods()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SearchTheMine, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.DefeatHivePods);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DefeatHivePodsSuccess_AdvancesToKillHiveQueen()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatHivePods, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.KillHiveQueen);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_HeadToMilosShuttleSuccess_CreditsGoldTimerAndFinishesEvent()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.HeadToMilosShuttle, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation update = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.GoldMedalTimer, update.Arguments[0]);
        Assert.Equal(0, update.Arguments[1]);

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = new OutpostM13EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.KillHiveQueen, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Fact]
    public void CaptainMiloScript_IsBoundToCaptainMiloCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(CaptainMiloEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 41201u }, attribute.CreatureId);
    }

    [Fact]
    public void DeadM13MinerScript_IsBoundToMappedCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(DeadM13MinerEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 29512u }, attribute.CreatureId);
    }

    [Fact]
    public void ForemanKrauseScript_IsBoundToMappedCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(ForemanKrauseEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 29468u }, attribute.CreatureId);
    }

    [Fact]
    public void ExitPanelScript_IsBoundToMappedCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(ExitPanelEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 23470u }, attribute.CreatureId);
    }

    [Fact]
    public void DeadM13Miner_OnActivateSuccess_UpdatesVirtualCollectAndRemovesOnce()
    {
        var script = new DeadM13MinerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(
            out IWorldEntity deadMiner,
            out RecordingDispatchProxy<IWorldEntity> deadMinerProxy);

        script.OnLoad(deadMiner);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.VirtualCollect, update.Arguments[0]);
        Assert.Equal(113u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
        Assert.Single(deadMinerProxy.GetInvocations(nameof(IWorldEntity.RemoveFromMap)));
    }

    [Fact]
    public void CaptainMilo_OnActivateSuccess_UpdatesActiveTalkObjectiveByTargetGroupForPlayer()
    {
        var script = new CaptainMiloEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity captainMilo);

        script.OnLoad(captainMilo);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Same(player, update.Arguments[0]);
        Assert.Equal(PublicEventObjectiveType.TalkTo, update.Arguments[1]);
        Assert.Equal(5308u, update.Arguments[2]);
        Assert.Equal(1, update.Arguments[3]);
    }

    [Fact]
    public void ForemanKrause_OnActivateSuccess_UpdatesActiveTalkObjectiveByTargetGroupForPlayerOnce()
    {
        var script = new ForemanKrauseEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity foremanKrause);

        script.OnLoad(foremanKrause);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Same(player, update.Arguments[0]);
        Assert.Equal(PublicEventObjectiveType.TalkTo, update.Arguments[1]);
        Assert.Equal(5295u, update.Arguments[2]);
        Assert.Equal(1, update.Arguments[3]);
    }

    [Fact]
    public void ExitPanel_OnActivateSuccess_UpdatesMappedExitPanelTargetGroupOnce()
    {
        var script = new ExitPanelEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity exitPanel);

        script.OnLoad(exitPanel);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(5294u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    private static IPublicEvent CreatePublicEvent(uint playerCount, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out RecordingDispatchProxy<IMapInstance> mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 1319u });

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithReviewedCaptainMilo(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedNpc> createdNpcs)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), 1u);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 1319u });

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

    private static RecordingDispatchProxy<IPublicEventManager> CreateWorldEntityWithPublicEventManager(out IWorldEntity entity)
    {
        return CreateWorldEntityWithPublicEventManager(out entity, out _);
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateWorldEntityWithPublicEventManager(
        out IWorldEntity entity,
        out RecordingDispatchProxy<IWorldEntity> entityProxy)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        entity = RecordingDispatchProxy<IWorldEntity>.Create(out entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);
        return publicEventManagerProxy;
    }

    private static CreatedNpc CreateNpc()
    {
        INonPlayerEntity npc = RecordingDispatchProxy<INonPlayerEntity>.Create(
            out RecordingDispatchProxy<INonPlayerEntity> npcProxy);
        return new CreatedNpc(npc, npcProxy);
    }

    private static void AssertOutpostM13NpcModel(
        CreatedNpc npc,
        uint entityId,
        uint creatureId,
        ushort areaId,
        uint phase,
        Vector3 position,
        uint displayInfo,
        ushort outfitInfo,
        ushort factionId)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            npc.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(entityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(creatureId, model.Creature);
        Assert.Equal((ushort)1319u, model.World);
        Assert.Equal(areaId, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(Vector3.Zero.X, model.Rx);
        Assert.Equal(Vector3.Zero.Y, model.Ry);
        Assert.Equal(Vector3.Zero.Z, model.Rz);
        Assert.Equal(displayInfo, model.DisplayInfo);
        Assert.Equal(outfitInfo, model.OutfitInfo);
        Assert.Equal(factionId, model.Faction1);
        Assert.Equal(factionId, model.Faction2);
        Assert.Equal(108u, model.EntityEvent.EventId);
        Assert.Equal(phase, model.EntityEvent.Phase);
        Assert.Empty(model.EntityScript);
        EntityStatModel health = Assert.Single(model.EntityStat);
        Assert.Equal((byte)Stat.Health, health.Stat);
        Assert.Equal(1f, health.Value);
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
        Assert.Equal(1319u, position.Info.Entry.Id);
    }

    private sealed record CreatedNpc(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);
}
