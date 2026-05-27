using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden;
using NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.Script;

namespace NexusForever.Game.Tests.Instances;

public class SanctuaryOfTheSwordmaidenTriggerScriptTests
{
    [Theory]
    [InlineData(typeof(TheTempleOfTheLifeSpeakerGridTriggerScript), 3421u)]
    [InlineData(typeof(MoldwoodCorruptionGridTriggerScript), 3420u)]
    [InlineData(typeof(LifeweaverTerraceGridTriggerEntityScript), 3419u)]
    public void ScriptObjectiveTriggers_PlayerEnter_UpdatesMappedScriptObjective(Type scriptType, uint objectId)
    {
        dynamic script = Activator.CreateInstance(scriptType);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.Script, update.Arguments[0]);
        Assert.Equal(objectId, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void Trigger_NonPlayerEnter_DoesNotUpdateObjective()
    {
        var script = new TheTempleOfTheLifeSpeakerGridTriggerScript();
        IGridEntity entity = RecordingDispatchProxy<IGridEntity>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(entity);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void FlameMinibossTrigger_PlayerEnter_BroadcastsSeleneMessage()
    {
        IPlayer enteringPlayer = RecordingDispatchProxy<IPlayer>.Create(out _);
        IPlayer mapPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> mapPlayerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out _);
        mapPlayerProxy.SetProperty(nameof(IPlayer.Session), session);

        var globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        ICommunicatorMessage communicatorMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> communicatorMessageProxy);
        globalQuestManagerProxy.SetMethodReturn(nameof(IGlobalQuestManager.GetCommunicatorMessage), communicatorMessage);

        var script = new FlameMinibossMessageTriggerScript(globalQuestManager);
        IGridTriggerEntity trigger = CreateTriggerWithMapInstance([mapPlayer]);

        script.OnLoad(trigger);
        script.OnEnterRange(enteringPlayer);

        RecordingDispatchProxy<IGlobalQuestManager>.Invocation lookup = Assert.Single(globalQuestManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
        Assert.Equal(CommunicatorMessage.SpiritMotherSelene8, lookup.Arguments[0]);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(communicatorMessageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    [Fact]
    public void FlameMinibossTrigger_NonPlayerEnter_DoesNotBroadcast()
    {
        IGridEntity entity = RecordingDispatchProxy<IGridEntity>.Create(out _);
        var globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        var script = new FlameMinibossMessageTriggerScript(globalQuestManager);
        IGridTriggerEntity trigger = CreateTriggerWithMapInstance([]);

        script.OnLoad(trigger);
        script.OnEnterRange(entity);

        Assert.Empty(globalQuestManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateTrigger(out IGridTriggerEntity trigger)
    {
        trigger = RecordingDispatchProxy<IGridTriggerEntity>.Create(out RecordingDispatchProxy<IGridTriggerEntity> triggerProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        triggerProxy.SetProperty(nameof(IGridEntity.Map), map);

        return publicEventManagerProxy;
    }

    private static IGridTriggerEntity CreateTriggerWithMapInstance(IReadOnlyList<IPlayer> players)
    {
        IGridTriggerEntity trigger = RecordingDispatchProxy<IGridTriggerEntity>.Create(out RecordingDispatchProxy<IGridTriggerEntity> triggerProxy);
        IMapInstance map = RecordingDispatchProxy<IMapInstance>.Create(out RecordingDispatchProxy<IMapInstance> mapProxy);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);
        triggerProxy.SetProperty(nameof(IGridEntity.Map), map);
        return trigger;
    }
}
