using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Raid.InitializationCoreY83;
using NexusForever.Script.Instance.Raid.InitializationCoreY83.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Game.Tests.Instances;

public class InitializationCoreY83TriggerScriptTests
{
    [Fact]
    public void QuarantineDoorPanel_OnActivateSuccess_UpdatesMappedTargetGroupOnce()
    {
        var script = new QuarantineDoorPanelEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy =
            CreateWorldEntityWithPublicEventManager(out IWorldEntity panel);

        script.OnLoad(panel);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(12539u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void QuarantineDoor_OnActivateSuccess_UpdatesMappedTargetGroupOnce()
    {
        var script = new QuarantineDoorEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy =
            CreateWorldEntityWithPublicEventManager(out IWorldEntity door);

        script.OnLoad(door);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(12462u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void QuarantineDoorPanelEntityScript_UsesMappedCreatureFilter()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(QuarantineDoorPanelEntityScript));

        IScriptFilterSearch matchingSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(68824u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(66047u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(matchingSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void QuarantineDoorEntityScript_UsesMappedCreatureFilter()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(QuarantineDoorEntityScript));

        IScriptFilterSearch matchingSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(66047u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(68859u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(matchingSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void CentralAccessCorridorTrigger_PlayerEnter_BroadcastsNurtonMessage()
    {
        IPlayer enteringPlayer = RecordingDispatchProxy<IPlayer>.Create(out _);
        IPlayer mapPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> mapPlayerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out _);
        mapPlayerProxy.SetProperty(nameof(IPlayer.Session), session);

        var globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        ICommunicatorMessage communicatorMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> communicatorMessageProxy);
        globalQuestManagerProxy.SetMethodReturn(nameof(IGlobalQuestManager.GetCommunicatorMessage), communicatorMessage);

        var script = new CentralAccessCorridorTriggerScript(globalQuestManager);
        IGridTriggerEntity trigger = CreateTriggerWithMapInstance([mapPlayer]);

        script.OnLoad(trigger);
        script.OnEnterRange(enteringPlayer);

        RecordingDispatchProxy<IGlobalQuestManager>.Invocation lookup = Assert.Single(globalQuestManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
        Assert.Equal(CommunicatorMessage.Nurton1, lookup.Arguments[0]);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(communicatorMessageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    [Fact]
    public void CentralAccessCorridorTrigger_NonPlayerEnter_DoesNotBroadcast()
    {
        IGridEntity entity = RecordingDispatchProxy<IGridEntity>.Create(out _);
        var globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        var script = new CentralAccessCorridorTriggerScript(globalQuestManager);
        IGridTriggerEntity trigger = CreateTriggerWithMapInstance([]);

        script.OnLoad(trigger);
        script.OnEnterRange(entity);

        Assert.Empty(globalQuestManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
    }

    private static IGridTriggerEntity CreateTriggerWithMapInstance(IReadOnlyList<IPlayer> players)
    {
        IGridTriggerEntity trigger = RecordingDispatchProxy<IGridTriggerEntity>.Create(out RecordingDispatchProxy<IGridTriggerEntity> triggerProxy);
        IMapInstance map = RecordingDispatchProxy<IMapInstance>.Create(out RecordingDispatchProxy<IMapInstance> mapProxy);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);
        triggerProxy.SetProperty(nameof(IGridEntity.Map), map);
        return trigger;
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateWorldEntityWithPublicEventManager(out IWorldEntity entity)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        entity = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> entityProxy);
        entityProxy.SetProperty(nameof(IWorldEntity.Map), map);

        return publicEventManagerProxy;
    }
}
