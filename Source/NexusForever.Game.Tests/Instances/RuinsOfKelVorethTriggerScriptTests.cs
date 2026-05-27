using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth;
using NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Instances;

public class RuinsOfKelVorethTriggerScriptTests
{
    [Fact]
    public void DigsiteScarTrigger_PlayerEnter_BroadcastsAvraMessage()
    {
        IPlayer enteringPlayer = RecordingDispatchProxy<IPlayer>.Create(out _);
        IPlayer mapPlayer = CreatePlayer(Faction.Exile, out IGameSession session);
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager((CommunicatorMessage.AvraDarkos5, message));

        var script = new DigsiteScarMessageTriggerScript(globalQuestManager);
        IGridTriggerEntity trigger = CreateTriggerWithMapInstance([mapPlayer]);

        script.OnLoad(trigger);
        script.OnEnterRange(enteringPlayer);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    [Fact]
    public void DrokkTrigger_PlayerEnter_UsesBranchPairedFactionMessages()
    {
        IPlayer enteringPlayer = RecordingDispatchProxy<IPlayer>.Create(out _);
        IPlayer exilePlayer = CreatePlayer(Faction.Exile, out IGameSession exileSession);
        IPlayer dominionPlayer = CreatePlayer(Faction.Dominion, out IGameSession dominionSession);
        ICommunicatorMessage avraMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> avraProxy);
        ICommunicatorMessage toricMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> toricProxy);
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            (CommunicatorMessage.AvraDarkos6, avraMessage),
            (CommunicatorMessage.ToricAntevian5, toricMessage));

        var script = new DrokkMessageTriggerScript(globalQuestManager);
        IGridTriggerEntity trigger = CreateTriggerWithMapInstance([exilePlayer, dominionPlayer]);

        script.OnLoad(trigger);
        script.OnEnterRange(enteringPlayer);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation avraSend = Assert.Single(avraProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(exileSession, avraSend.Arguments[0]);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation toricSend = Assert.Single(toricProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(dominionSession, toricSend.Arguments[0]);
    }

    [Fact]
    public void TheExaniteForgesTrigger_PlayerEnter_BroadcastsAvraMessage()
    {
        IPlayer enteringPlayer = RecordingDispatchProxy<IPlayer>.Create(out _);
        IPlayer mapPlayer = CreatePlayer(Faction.Dominion, out IGameSession session);
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager((CommunicatorMessage.AvraDarkos8, message));

        var script = new TheExaniteForgesMessageTriggerScript(globalQuestManager);
        IGridTriggerEntity trigger = CreateTriggerWithMapInstance([mapPlayer]);

        script.OnLoad(trigger);
        script.OnEnterRange(enteringPlayer);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    [Fact]
    public void MessageTrigger_NonPlayerEnter_DoesNotBroadcast()
    {
        IGridEntity entity = RecordingDispatchProxy<IGridEntity>.Create(out _);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        var script = new DigsiteScarMessageTriggerScript(globalQuestManager);
        IGridTriggerEntity trigger = CreateTriggerWithMapInstance([]);

        script.OnLoad(trigger);
        script.OnEnterRange(entity);

        Assert.Empty(globalQuestManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
    }

    [Fact]
    public void BloodPitGladiator_OnDeath_UpdatesBloodPitTargetGroup()
    {
        var script = new BloodPitGladiatorEntityScript(
            RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _),
            RecordingDispatchProxy<IGameTableManager>.Create(out _));
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        creatureProxy.SetProperty(nameof(IGridEntity.Map), map);

        script.OnLoad(creature);
        script.OnDeath();

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.KillTargetGroup, update.Arguments[0]);
        Assert.Equal(3972u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void BloodPitCinematicTrigger_PlayerEnter_QueuesWipEnterCinematicForMapPlayers()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        IPlayer enteringPlayer = RecordingDispatchProxy<IPlayer>.Create(out _);
        IPlayer mapPlayer = CreateCinematicPlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);
        var script = new BloodPitCinematicTriggerScript(CreateCinematicFactory(cinematic));
        IGridTriggerEntity trigger = CreateTriggerWithMapInstance([mapPlayer]);

        script.OnLoad(trigger);
        script.OnEnterRange(enteringPlayer);

        RecordingDispatchProxy<ICinematicManager>.Invocation queue = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queue.Arguments[0]);
    }

    private static IPlayer CreatePlayer(Faction faction, out IGameSession session)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        session = RecordingDispatchProxy<IGameSession>.Create(out _);
        playerProxy.SetProperty(nameof(IWorldEntity.Faction1), faction);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static IPlayer CreateCinematicPlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy)
    {
        ICinematicManager cinematicManager = RecordingDispatchProxy<ICinematicManager>.Create(out cinematicManagerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CinematicManager), cinematicManager);
        return player;
    }

    private static ICinematicFactory CreateCinematicFactory(ICinematicBase cinematic)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out RecordingDispatchProxy<ICinematicFactory> cinematicFactoryProxy);
        cinematicFactoryProxy.SetMethodReturn(nameof(ICinematicFactory.CreateCinematic), cinematic);
        return cinematicFactory;
    }

    private static IGridTriggerEntity CreateTriggerWithMapInstance(IReadOnlyList<IPlayer> players)
    {
        IGridTriggerEntity trigger = RecordingDispatchProxy<IGridTriggerEntity>.Create(out RecordingDispatchProxy<IGridTriggerEntity> triggerProxy);
        IMapInstance map = RecordingDispatchProxy<IMapInstance>.Create(out RecordingDispatchProxy<IMapInstance> mapProxy);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);
        triggerProxy.SetProperty(nameof(IGridEntity.Map), map);
        return trigger;
    }

    private static IGlobalQuestManager CreateGlobalQuestManager(params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IReadOnlyDictionary<CommunicatorMessage, ICommunicatorMessage> lookup = messages.ToDictionary(m => m.Id, m => m.Message);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetCommunicatorMessage), args => lookup[(CommunicatorMessage)args[0]]);
        return globalQuestManager;
    }
}
