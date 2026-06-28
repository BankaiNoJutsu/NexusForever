using System.Numerics;
using NexusForever.Database.World.Model;
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
using NexusForever.Script.Instance.EventInstances.ShadesEve;

namespace NexusForever.Game.Tests.Instances;

public class ShadesEveEventScriptTests
{
    [Fact]
    public void OnLoad_SetsInitialFindTheFountainPhase()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.FindTheFountain, invocation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_FindTheFountain_UsesCurrentPlayerCount()
    {
        var script = CreateScript();
        CreatedSimple fountain = CreateSimple();
        IPublicEvent publicEvent = CreatePublicEvent(3u, [], out RecordingDispatchProxy<IPublicEvent> eventProxy, out _, fountain.Instance);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FindTheFountain);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.FindTheFountain, activation.Arguments[0]);
        Assert.Equal(3u, activation.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventPhase_FindTheFountain_SpawnsReviewedFountainGatheringCircle()
    {
        var script = CreateScript();
        CreatedSimple fountain = CreateSimple();
        IPublicEvent publicEvent = CreatePublicEvent(3u, [], out _, out RecordingDispatchProxy<IMapInstance> mapProxy, fountain.Instance);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FindTheFountain);

        AssertFountainGatheringCircleModel(fountain);
        AssertGridEntityAddedToMap(mapProxy, fountain.Instance, new Vector3(333.41528f, -871.36365f, -242.43497f));
    }

    [Fact]
    public void OnPublicEventPhase_FindTheFountain_DoesNotDuplicateFountainGatheringCircle()
    {
        var script = CreateScript();
        CreatedSimple fountain = CreateSimple();
        IPublicEvent publicEvent = CreatePublicEvent(3u, [], out RecordingDispatchProxy<IPublicEvent> eventProxy, out RecordingDispatchProxy<IMapInstance> mapProxy, fountain.Instance);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FindTheFountain);
        script.OnPublicEventPhase((uint)PublicEventPhase.FindTheFountain);

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.CreateEntity)));
        Assert.Single(fountain.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Fact]
    public void OnPublicEventPhase_TalkWithTheLocals_ActivatesMappedObjective()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.TalkWithTheLocals);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.TalkWithTheLocals, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_SpeakWithEttyWindsen_BroadcastsMappedAngelMessage()
    {
        IPlayer player = CreatePlayer(out IGameSession session);
        CreatedNpc etty = CreateNpc();
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        var script = CreateScript((CommunicatorMessage.TheAngel5, message));
        IPublicEvent publicEvent = CreatePublicEvent(1u, [player], out _, out _, etty.Instance);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SpeakWithEttyWindsen);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_SpeakWithEttyWindsen_SpawnsReviewedEttyWindsen()
    {
        CreatedNpc etty = CreateNpc();
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, [], out _, out RecordingDispatchProxy<IMapInstance> mapProxy, etty.Instance);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SpeakWithEttyWindsen);

        AssertEttyWindsenModel(etty);
        AssertGridEntityAddedToMap(mapProxy, etty.Instance, new Vector3(340.9081f, -869.39844f, -256.90118f));
    }

    [Fact]
    public void OnPublicEventPhase_SpeakWithEttyWindsen_DoesNotDuplicateEttyWindsen()
    {
        CreatedNpc etty = CreateNpc();
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, [], out RecordingDispatchProxy<IPublicEvent> eventProxy, out RecordingDispatchProxy<IMapInstance> mapProxy, etty.Instance);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SpeakWithEttyWindsen);
        script.OnPublicEventPhase((uint)PublicEventPhase.SpeakWithEttyWindsen);

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.CreateEntity)));
        Assert.Single(etty.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Success_AdvancesBranchPhaseChain()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SpeakWithEttyWindsen, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.SpeakWithTheMayor);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_TalkWithLocals_StartsMappedVote()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.TalkWithTheLocals, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation vote = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.StartVote)));
        Assert.Equal(PublicEventTeam.PublicTeam, vote.Arguments[0]);
        Assert.Equal(64u, vote.Arguments[1]);
        Assert.Equal(1u, vote.Arguments[2]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FindTheFountain, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.StartVote)));
    }

    [Fact]
    public void OnCinematicFinish_FindTheFountain_SendsAngelMessageToPlayer()
    {
        IPlayer player = CreatePlayer(out IGameSession session);
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        var script = CreateScript((CommunicatorMessage.TheAngel1, message));
        IPublicEvent publicEvent = CreatePublicEvent(1u, [player], out RecordingDispatchProxy<IPublicEvent> eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Phase), (uint)PublicEventPhase.FindTheFountain);
        script.OnLoad(publicEvent);

        script.OnCinematicFinish(player, 0u);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    private static IPublicEvent CreatePublicEvent(uint playerCount, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(playerCount, [], out eventProxy, out _);
    }

    private static IPublicEvent CreatePublicEvent(uint playerCount, IReadOnlyList<IPlayer> players, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(playerCount, players, out eventProxy, out _);
    }

    private static IPublicEvent CreatePublicEvent(
        uint playerCount,
        IReadOnlyList<IPlayer> players,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        params IGridEntity[] createdEntities)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 3044u });
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);
        if (createdEntities.Length != 0)
        {
            Queue<IGridEntity> queuedEntities = new(createdEntities);
            eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () => queuedEntities.Dequeue());
        }

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

    private static ShadesEveMainEventScript CreateScript(params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IReadOnlyDictionary<CommunicatorMessage, ICommunicatorMessage> lookup = messages.ToDictionary(m => m.Id, m => m.Message);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetCommunicatorMessage),
            args => lookup.TryGetValue((CommunicatorMessage)args[0], out ICommunicatorMessage message) ? message : null);
        return new ShadesEveMainEventScript(globalQuestManager);
    }

    private static IPlayer CreatePlayer(out IGameSession session)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        session = RecordingDispatchProxy<IGameSession>.Create(out _);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
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

    private static void AssertFountainGatheringCircleModel(CreatedSimple simple)
    {
        RecordingDispatchProxy<ISimpleEntity>.Invocation initialise = Assert.Single(
            simple.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(1100300058u, model.Id);
        Assert.Equal(EntityType.Simple, model.Type);
        Assert.Equal(64820u, model.Creature);
        Assert.Equal((ushort)3044u, model.World);
        Assert.Equal((ushort)0u, model.Area);
        Assert.Equal(333.41528f, model.X);
        Assert.Equal(-871.36365f, model.Y);
        Assert.Equal(-242.43497f, model.Z);
        Assert.Equal(-3.1415925f, model.Rx);
        Assert.Equal(0f, model.Ry);
        Assert.Equal(0f, model.Rz);
        Assert.Equal(30327u, model.DisplayInfo);
        Assert.Equal((ushort)0u, model.OutfitInfo);
        Assert.Equal((ushort)219u, model.Faction1);
        Assert.Equal((ushort)219u, model.Faction2);
        Assert.Equal(597u, model.EntityEvent.EventId);
        Assert.Equal(0u, model.EntityEvent.Phase);
        Assert.Empty(model.EntityScript);
        Assert.Empty(model.EntityStat);
    }

    private static void AssertEttyWindsenModel(CreatedNpc npc)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            npc.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(1100300057u, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(62747u, model.Creature);
        Assert.Equal((ushort)3044u, model.World);
        Assert.Equal((ushort)0u, model.Area);
        Assert.Equal(340.9081f, model.X);
        Assert.Equal(-869.39844f, model.Y);
        Assert.Equal(-256.90118f, model.Z);
        Assert.Equal(2.9192612f, model.Rx);
        Assert.Equal(0f, model.Ry);
        Assert.Equal(0f, model.Rz);
        Assert.Equal(23053u, model.DisplayInfo);
        Assert.Equal((ushort)7913u, model.OutfitInfo);
        Assert.Equal((ushort)219u, model.Faction1);
        Assert.Equal((ushort)219u, model.Faction2);
        Assert.Equal(597u, model.EntityEvent.EventId);
        Assert.Equal(1u, model.EntityEvent.Phase);
        Assert.Empty(model.EntityScript);
        Assert.Equal(2, model.EntityStat.Count);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Health && stat.Value == 1f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 50f);
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
        Assert.Equal(3044u, position.Info.Entry.Id);
    }

    private sealed record CreatedNpc(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);

    private sealed record CreatedSimple(
        ISimpleEntity Instance,
        RecordingDispatchProxy<ISimpleEntity> Proxy);
}
