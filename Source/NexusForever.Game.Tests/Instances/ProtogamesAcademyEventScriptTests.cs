using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Dungeon.ProtogamesAcademy;

namespace NexusForever.Game.Tests.Instances;

public class ProtogamesAcademyEventScriptTests
{
    private const uint InvulnotronPhase = 3u;
    private const uint GromkaPhase = 5u;
    private const uint IrukiBoldbeardPhase = 7u;
    private const uint SeekNSlaughterPhase = 11u;
    private const uint IceboxMk2Phase = 13u;
    private const uint SuperInvulnotronPhase = 15u;
    private const uint WrathbonePhase = 18u;

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
    [InlineData(PublicEventPhase.DefeatInvulnotron, PublicEventObjective.DefeatInvulnotron, 1100300031u, 67475u, 4651, InvulnotronPhase, -24330.6f, -974.0855f, -28917.8f, 24783u, 1322, "InvulnotronEntityScript")]
    [InlineData(PublicEventPhase.DefeatGromka, PublicEventObjective.DefeatGromka, 1100300033u, 67594u, 4507, GromkaPhase, -24424.6f, -974.0912f, -28788.1f, 28878u, 1322, "GromkaEntityScript")]
    [InlineData(PublicEventPhase.DefeatIrukiBoldbeard, PublicEventObjective.DefeatIrukiBoldbeard, 1100300034u, 67663u, 4507, IrukiBoldbeardPhase, -24360.97f, -972.6974f, -28943.9f, 32741u, 1322, "IrukiBoldbeardEntityScript")]
    [InlineData(PublicEventPhase.DefeatSeekNSlaughter, PublicEventObjective.DefeatSeekNSlaughter, 1100300035u, 67668u, 4507, SeekNSlaughterPhase, -19777.14f, -946.8414f, -29457.42f, 21309u, 233, "SeekNSlaughterEntityScript")]
    [InlineData(PublicEventPhase.DefeatIceboxMk2, PublicEventObjective.DefeatIceboxMk2, 1100300036u, 67757u, 4507, IceboxMk2Phase, -19777.14f, -946.8414f, -29457.42f, 21323u, 233, "IceboxMk2EntityScript")]
    [InlineData(PublicEventPhase.DefeatSuperInvulnotron, PublicEventObjective.DefeatSuperInvulnotron, 1100300100u, 68096u, 4507, SuperInvulnotronPhase, -19779f, -946f, -29462f, 24783u, 1322, "SuperInvulnotronEntityScript")]
    [InlineData(PublicEventPhase.DefeatWrathbone, PublicEventObjective.DefeatWrathbone, 1100300101u, 67944u, 4508, WrathbonePhase, -15788f, -904f, -29615f, 26004u, 1322, "WrathboneEntityScript")]
    public void OnPublicEventPhase_ReviewedBossPhases_SpawnReviewedPlacement(
        PublicEventPhase phase,
        PublicEventObjective objective,
        uint entityId,
        uint creatureId,
        ushort areaId,
        uint eventPhase,
        float x,
        float y,
        float z,
        uint displayInfo,
        ushort factionId,
        string scriptName)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(
            3u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        AssertObjectiveActivated(eventProxy, objective);
        CreatedNpc npc = Assert.Single(createdNpcs);
        AssertProtogamesAcademySpawnModel(
            npc,
            entityId,
            creatureId,
            areaId,
            eventPhase,
            new Vector3(x, y, z),
            displayInfo,
            factionId,
            scriptName);
        AssertGridEntityAddedToMap(mapProxy, npc.Instance, new Vector3(x, y, z));
    }

    [Theory]
    [InlineData(PublicEventPhase.DefeatInvulnotron)]
    [InlineData(PublicEventPhase.DefeatGromka)]
    [InlineData(PublicEventPhase.DefeatIrukiBoldbeard)]
    [InlineData(PublicEventPhase.DefeatSeekNSlaughter)]
    [InlineData(PublicEventPhase.DefeatIceboxMk2)]
    [InlineData(PublicEventPhase.DefeatSuperInvulnotron)]
    [InlineData(PublicEventPhase.DefeatWrathbone)]
    public void OnPublicEventPhase_ReviewedBossPhases_DoNotDuplicatePlacement(PublicEventPhase phase)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(
            3u,
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
    [InlineData(PublicEventPhase.GatherInvulnotron, 48842u, 7932u, -24400.1f, -974.668f, -28977.1f)]
    [InlineData(PublicEventPhase.GatherGromka, 48843u, 7933u, -24504.9f, -974.749f, -28857.5f)]
    [InlineData(PublicEventPhase.GatherIrukiBoldbeard, 48842u, 7932u, -24400.1f, -974.668f, -28977.1f)]
    [InlineData(PublicEventPhase.TeleporterToTheNextEvent, 50164u, 7936u, -24400.19f, -974.668f, -28977.15f)]
    [InlineData(PublicEventPhase.Gather, 48846u, 7934u, -19804.34f, -945.5437f, -29483.94f)]
    [InlineData(PublicEventPhase.Gather2, 48846u, 7934u, -19804.34f, -945.5437f, -29483.94f)]
    [InlineData(PublicEventPhase.GoToLastEvent, 48846u, 7939u, -19804.3f, -945.544f, -29483.9f)]
    [InlineData(PublicEventPhase.MeetWithPhineasARotostar, 48850u, 7940u, -15760.7f, -904.839f, -29638.5f)]
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
        IPublicEvent publicEvent = phase is PublicEventPhase.DefeatIrukiBoldbeard or PublicEventPhase.DefeatIceboxMk2
            ? CreatePublicEventWithReviewedSpawns(1u, [player], out _, out _, out _)
            : CreatePublicEvent(1u, [player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_DefeatGromka_BroadcastsMappedPhineasMessage()
    {
        IPlayer player = CreatePlayer(out IGameSession session);
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        var script = CreateScript((CommunicatorMessage.PhineasARotostar2, message));
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(1u, [player], out _, out _, out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatGromka);

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

    private static IPublicEvent CreatePublicEventWithReviewedSpawns(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedNpc> createdNpcs)
    {
        return CreatePublicEventWithReviewedSpawns(playerCount, [], out eventProxy, out mapProxy, out createdNpcs);
    }

    private static IPublicEvent CreatePublicEventWithReviewedSpawns(
        uint playerCount,
        IReadOnlyList<IPlayer> players,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedNpc> createdNpcs)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3173u });
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

    private static CreatedNpc CreateNpc()
    {
        INonPlayerEntity npc = RecordingDispatchProxy<INonPlayerEntity>.Create(out RecordingDispatchProxy<INonPlayerEntity> npcProxy);
        return new CreatedNpc(npc, npcProxy);
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
        CreatedTrigger trigger,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
        Assert.Same(trigger.Instance, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(3173u, position.Info.Entry.Id);
    }

    private static void AssertProtogamesAcademySpawnModel(
        CreatedNpc npc,
        uint entityId,
        uint creatureId,
        ushort areaId,
        uint phase,
        Vector3 position,
        uint displayInfo,
        ushort factionId,
        string scriptName)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            npc.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(entityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(creatureId, model.Creature);
        Assert.Equal((ushort)3173u, model.World);
        Assert.Equal(areaId, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(Vector3.Zero.X, model.Rx);
        Assert.Equal(Vector3.Zero.Y, model.Ry);
        Assert.Equal(Vector3.Zero.Z, model.Rz);
        Assert.Equal(displayInfo, model.DisplayInfo);
        Assert.Equal(factionId, model.Faction1);
        Assert.Equal(factionId, model.Faction2);
        Assert.Equal(667u, model.EntityEvent.EventId);
        Assert.Equal(phase, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal(scriptName, entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Health && stat.Value == 1f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 10f);
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
        Assert.Equal(3173u, position.Info.Entry.Id);
    }

    private sealed record CreatedTrigger(
        IWorldLocationVolumeGridTriggerEntity Instance,
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> Proxy);

    private sealed record CreatedNpc(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);
}
